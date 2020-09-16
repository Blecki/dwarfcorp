// DestinationVoxel.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Diagnostics;

namespace DwarfCorp
{

    /// <summary>
    /// Specifies the location of a vertex on a voxel.
    /// </summary>
    public enum VoxelVertex
    {
        Back = 0,
        Front = 1,
        Bottom = 0,
        Top = 2,
        Left = 0,
        Right = 4,

        BackBottomLeft = Back | Bottom | Left,      // 000
        FrontBottomLeft = Front | Bottom | Left,    // 001
        BackTopLeft = Back | Top | Left,            // 010
        FrontTopLeft = Front | Top | Left,          // 011
        BackBottomRight = Back | Bottom | Right,    // 100
        FrontBottomRight = Front | Bottom | Right,  // 101
        BackTopRight = Back | Top | Right,          // 110
        FrontTopRight = Front | Top | Right,        // 111

        Count = 8
    }

    /// <summary>
    /// Specifies how a voxel is to be sloped.
    /// </summary>
    [Flags]
    public enum RampType // Todo: Change to compass orientation.
    {
        None = 0x0,
        TopFrontLeft = 0x1,
        TopFrontRight = 0x2,
        TopBackLeft = 0x4,
        TopBackRight = 0x8,
        Front = TopFrontLeft | TopFrontRight,
        Back = TopBackLeft | TopBackRight,
        Left = TopBackLeft | TopFrontLeft,
        Right = TopBackRight | TopFrontRight,
        All = TopFrontLeft | TopFrontRight | TopBackLeft | TopBackRight
    }


    /// <summary> Determines a transition texture type. Each phrase
    /// (front, left, back, right) defines whether or not a tile of the same type is
    /// on the given face</summary>
    [Flags]
    public enum TransitionTexture
    {
        None = 0,
        Front = 1,
        Right = 2,
        FrontRight = 3,
        Back = 4,
        FrontBack = 5,
        BackRight = 6,
        FrontBackRight = 7,
        Left = 8,
        FrontLeft = 9,
        LeftRight = 10,
        LeftFrontRight = 11,
        LeftBack = 12,
        FrontBackLeft = 13,
        LeftBackRight = 14,
        All = 15
    }
}
