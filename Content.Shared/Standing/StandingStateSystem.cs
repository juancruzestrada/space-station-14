using Content.Shared.Audio;
using Content.Shared.Hands.Components;
using Content.Shared.Rotation;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Physics;
using Content.Shared.Physics;

namespace Content.Shared.Standing
{
    public sealed class StandingStateSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        
        // If StandingCollisionLayer value is ever changed to more than one layer, the logic needs to be edited.
        private const int StandingCollisionLayer = (int) CollisionGroup.MidImpassable;

        public bool IsDown(EntityUid uid, StandingStateComponent? standingState = null)
        {
            if (!Resolve(uid, ref standingState, false))
                return false;

            return !standingState.Standing;
        }

        public bool Down(EntityUid uid, bool playSound = true, bool dropHeldItems = true,
            StandingStateComponent? standingState = null,
            AppearanceComponent? appearance = null,
            SharedHandsComponent? hands = null)
        {
            // TODO: This should actually log missing comps...
            if (!Resolve(uid, ref standingState, false))
                return false;

            // Optional component.
            Resolve(uid, ref appearance, ref hands, false);

            if (!standingState.Standing)
                return true;

            // This is just to avoid most callers doing this manually saving boilerplate
            // 99% of the time you'll want to drop items but in some scenarios (e.g. buckling) you don't want to.
            // We do this BEFORE downing because something like buckle may be blocking downing but we want to drop hand items anyway
            // and ultimately this is just to avoid boilerplate in Down callers + keep their behavior consistent.
            if (dropHeldItems && hands != null)
            {
                RaiseLocalEvent(uid, new DropHandItemsEvent(), false);
            }

            var msg = new DownAttemptEvent();
            RaiseLocalEvent(uid, msg, false);

            if (msg.Cancelled)
                return false;

            standingState.Standing = false;
            Dirty(standingState);
            RaiseLocalEvent(uid, new DownedEvent(), false);

            if (!_gameTiming.IsFirstTimePredicted)
                return true;

            // Seemed like the best place to put it
            appearance?.SetData(RotationVisuals.RotationState, RotationState.Horizontal);

            // Change collision masks to allow going under certain entities like flaps and tables
            if (TryComp(uid, out FixturesComponent? fixtureComponent))
            {
                foreach (var (key, fixture) in fixtureComponent.Fixtures)
                {
                    if ((fixture.CollisionMask & StandingCollisionLayer) == 0)
                        continue;

                    standingState.ChangedFixtures.Add(key);
                    fixture.CollisionMask &= ~StandingCollisionLayer;
                }
            }

            // Currently shit is only downed by server but when it's predicted we can probably only play this on server / client
            // > no longer true with door crushing. There just needs to be a better way to handle audio prediction.
            if (playSound)
            {
                SoundSystem.Play(Filter.Pvs(uid), standingState.DownSoundCollection.GetSound(), uid, AudioHelpers.WithVariation(0.25f));
            }

            return true;
        }

        public bool Stand(EntityUid uid,
            StandingStateComponent? standingState = null,
            AppearanceComponent? appearance = null)
        {
            // TODO: This should actually log missing comps...
            if (!Resolve(uid, ref standingState, false))
                return false;

            // Optional component.
            Resolve(uid, ref appearance, false);

            if (standingState.Standing)
                return true;

            var msg = new StandAttemptEvent();
            RaiseLocalEvent(uid, msg, false);

            if (msg.Cancelled)
                return false;

            standingState.Standing = true;
            standingState.Dirty();
            RaiseLocalEvent(uid, new StoodEvent(), false);

            appearance?.SetData(RotationVisuals.RotationState, RotationState.Vertical);

            if (TryComp(uid, out FixturesComponent? fixtureComponent))
            {
                foreach (var key in standingState.ChangedFixtures)
                {
                    if (fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                        fixture.CollisionMask |= StandingCollisionLayer;
                }
            }
            standingState.ChangedFixtures.Clear();

            return true;
        }
    }

    public sealed class DropHandItemsEvent : EventArgs
    {
    }

    /// <summary>
    /// Subscribe if you can potentially block a down attempt.
    /// </summary>
    public sealed class DownAttemptEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Subscribe if you can potentially block a stand attempt.
    /// </summary>
    public sealed class StandAttemptEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Raised when an entity becomes standing
    /// </summary>
    public sealed class StoodEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Raised when an entity is not standing
    /// </summary>
    public sealed class DownedEvent : EntityEventArgs
    {
    }
}
