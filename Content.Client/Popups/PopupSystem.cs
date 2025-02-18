using Content.Client.Stylesheets;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

        private readonly List<PopupLabel> _aliveLabels = new();

        public const float PopupLifetime = 3f;

        public override void Initialize()
        {
            SubscribeNetworkEvent<PopupCursorEvent>(OnPopupCursorEvent);
            SubscribeNetworkEvent<PopupCoordinatesEvent>(OnPopupCoordinatesEvent);
            SubscribeNetworkEvent<PopupEntityEvent>(OnPopupEntityEvent);
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        #region Actual Implementation

        public void PopupCursor(string message)
        {
            PopupMessage(message, _inputManager.MouseScreenPosition);
        }

        public void PopupCoordinates(string message, EntityCoordinates coordinates)
        {
            PopupMessage(message, _eyeManager.CoordinatesToScreen(coordinates));
        }

        public void PopupEntity(string message, EntityUid uid)
        {
            if (!EntityManager.EntityExists(uid))
                return;

            var transform = EntityManager.GetComponent<TransformComponent>(uid);
            PopupMessage(message, _eyeManager.CoordinatesToScreen(transform.Coordinates), uid);
        }

        public void PopupMessage(string message, ScreenCoordinates coordinates, EntityUid? entity = null)
        {
            var label = new PopupLabel(_eyeManager, EntityManager)
            {
                Entity = entity,
                Text = message,
                StyleClasses = { StyleNano.StyleClassPopupMessage },
            };

            _userInterfaceManager.PopupRoot.AddChild(label);
            label.Measure(Vector2.Infinity);

            var mapCoordinates = _eyeManager.ScreenToMap(coordinates.Position);
            label.InitialPos = mapCoordinates;
            LayoutContainer.SetPosition(label, label.InitialPos.Position);
            _aliveLabels.Add(label);
        }

        #endregion

        #region Abstract Method Implementations

        public override void PopupCursor(string message, Filter filter)
        {
            if (!filter.CheckPrediction)
                return;

            PopupCursor(message);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter)
        {
            if (!filter.CheckPrediction)
                return;

            PopupCoordinates(message, coordinates);
        }

        public override void PopupEntity(string message, EntityUid uid, Filter filter)
        {
            if (!filter.CheckPrediction)
                return;

            PopupEntity(message, uid);
        }

        #endregion

        #region Network Event Handlers

        private void OnPopupCursorEvent(PopupCursorEvent ev)
        {
            PopupCursor(ev.Message);
        }

        private void OnPopupCoordinatesEvent(PopupCoordinatesEvent ev)
        {
            PopupCoordinates(ev.Message, ev.Coordinates);
        }

        private void OnPopupEntityEvent(PopupEntityEvent ev)
        {
            PopupEntity(ev.Message, ev.Uid);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            foreach (var label in _aliveLabels)
            {
                label.Dispose();
            }

            _aliveLabels.Clear();
        }

        #endregion

        public override void FrameUpdate(float frameTime)
        {
            if (_aliveLabels.Count == 0) return;

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            var playerPos = player != null ? Transform(player.Value).MapPosition : MapCoordinates.Nullspace;

            // ReSharper disable once ConvertToLocalFunction
            var predicate = static (EntityUid uid, (EntityUid? compOwner, EntityUid? attachedEntity) data)
                => uid == data.compOwner || uid == data.attachedEntity;
            var occluded = player != null && _examineSystem.IsOccluded(player.Value);

            for (var i = _aliveLabels.Count - 1; i >= 0; i--)
            {
                var label = _aliveLabels[i];
                if (label.TotalTime > PopupLifetime ||
                    label.Entity != null && Deleted(label.Entity))
                {
                    label.Dispose();
                    _aliveLabels.RemoveAt(i);
                    continue;
                }

                if (label.Entity == player)
                {
                    label.Visible = true;
                    continue;
                }

                var otherPos = label.Entity != null ? Transform(label.Entity.Value).MapPosition : label.InitialPos;

                if (occluded && !ExamineSystemShared.InRangeUnOccluded(
                        playerPos,
                        otherPos, 0f,
                        (label.Entity, player), predicate))
                {
                    label.Visible = false;
                    continue;
                }

                label.Visible = true;
            }
        }

        private sealed class PopupLabel : Label
        {
            private readonly IEyeManager _eyeManager;
            private readonly IEntityManager _entityManager;

            public float TotalTime { get; private set; }
            /// <summary>
            /// The original Mapid and ScreenPosition of the label.
            /// </summary>
            /// <remarks>
            /// Yes that's right it's not technically MapCoordinates.
            /// </remarks>
            public MapCoordinates InitialPos { get; set; }
            public EntityUid? Entity { get; set; }

            public PopupLabel(IEyeManager eyeManager, IEntityManager entityManager)
            {
                _eyeManager = eyeManager;
                _entityManager = entityManager;
                ShadowOffsetXOverride = 1;
                ShadowOffsetYOverride = 1;
                FontColorShadowOverride = Color.Black;
            }

            protected override void FrameUpdate(FrameEventArgs eventArgs)
            {
                TotalTime += eventArgs.DeltaSeconds;

                Vector2 position;
                if (Entity == null)
                    position = _eyeManager.WorldToScreen(InitialPos.Position) / UIScale - DesiredSize / 2;
                else if (_entityManager.TryGetComponent(Entity.Value, out TransformComponent xform))
                    position = (_eyeManager.CoordinatesToScreen(xform.Coordinates).Position / UIScale) - DesiredSize / 2;
                else
                {
                    // Entity has probably been deleted.
                    Visible = false;
                    TotalTime += PopupLifetime;
                    return;
                }

                LayoutContainer.SetPosition(this, position - (0, 20 * (TotalTime * TotalTime + TotalTime)));

                if (TotalTime > 0.5f)
                {
                    Modulate = Color.White.WithAlpha(1f - 0.2f * (float)Math.Pow(TotalTime - 0.5f, 3f));
                }
            }
        }
    }
}
