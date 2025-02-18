#!/usr/bin/env python3

# Copyright (c) 2022 Space Wizards Federation
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.

class ConversionMode:
    def __init__(self, tw, th, states):
        self.tw = tw
        self.th = th
        self.states = states

conversion_modes = {
    # TG
    "tg": ConversionMode(
        7, 7,
        [
            # Each output state gives a source quadrant for BR, TL, TR, BL.
            # The idea is that each of the 4 directions is a different rotation of the same state.
            # These states are associated by a bitfield indicating occupance relative to the indicated corner:
            # 1: Tile anti-clockwise of indicated diagonal occupied.
            # 2: Tile in indicated diagonal occupied.
            # 4: Tile clockwise of indicated diagonal occupied.

            # BR, TL, TR, BL
            [  0,  0,  0,  0], # 0 : Standing / Outer corners
            [ 12, 12,  3,  3], # 1 : Straight line ; top half horizontal bottom half vertical
            [  0,  0,  0,  0], # 2 : Standing / Outer corners diagonal
            [ 12, 12,  3,  3], # 3 : Seems to match 1
            [  3,  3, 12, 12], # 4 : Straight line ; top half vertical bottom half horizontal
            [ 15, 15, 15, 15], # 5 : Inner corners
            [  3,  3, 12, 12], # 6 : Seems to match 4
            [ 46, 46, 46, 46], # 7 : Full
        ]
    ),
    # Citadel Station
    "citadel": ConversionMode(
        7, 3,
        [
            # BR, TL, TR, BL
            [  3,  0,  1,  2],
            [ 11,  8,  5,  6],
            [  3,  0,  1,  2],
            [ 11,  8,  5,  6],
            [  7,  4,  9, 10],
            [ 15, 12, 13, 14],
            [  7,  4,  9, 10],
            [ 19, 16, 17, 18],
        ]
    ),
    # VXA
    "vxa": ConversionMode(
        2, 3,
        [
            # 01 # 1: Tile anti-clockwise of indicated diagonal occupied.
            # 23 # 2: Tile in indicated diagonal occupied.
            # 45 # 4: Tile clockwise of indicated diagonal occupied.
            # BR, TL, TR, BL
            [  5,  2,  3,  4], # 0 X (ST)
            [  4,  3,  5,  2], # 1
            [  5,  2,  3,  4], # 2 X (ST)
            [  4,  3,  5,  2], # 3
            [  3,  4,  2,  5], # 4
            [  1,  1,  1,  1], # 5 X (IC)
            [  3,  4,  2,  5], # 6
            [  2,  5,  4,  3], # 7 X (F)
        ]
    ),
    # VXA+ - custom extensions to the VXA AT field format to make it map 1:1 with RT subtiles
    "vxap": ConversionMode(
        2, 4,
        [
            # BR, TL, TR, BL
            [  5,  2,  3,  4], # 0 X (ST)
            [  4,  3,  5,  2], # 1
            [  0,  0,  0,  0], # 2 - diagdup of 0
            [  6,  6,  7,  7], # 3 - diagdup of 1
            [  3,  4,  2,  5], # 4
            [  1,  1,  1,  1], # 5 X (IC)
            [  7,  7,  6,  6], # 6 - diagdup of 4
            [  2,  5,  4,  3], # 7 X (F)
        ]
    ),
    # rt_states - debugging!
    "rt_states": ConversionMode(
        8, 1,
        [
            [  0,  0,  0,  0],
            [  1,  1,  1,  1],
            [  2,  2,  2,  2],
            [  3,  3,  3,  3],
            [  4,  4,  4,  4],
            [  5,  5,  5,  5],
            [  6,  6,  6,  6],
            [  7,  7,  7,  7],
        ]
    ),
}

all_conv = "tg/citadel/vxa/vxap/rt_states"

