// PlanService.cs
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
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// This is a GoalRegion which tells the dwarf to be in a specific voxel.
    /// </summary>
    /// <seealso cref="GoalRegion" />
    public class VoxelGoalRegion : GoalRegion
    {
        public TemporaryVoxelHandle Voxel { get; set; }

        public override bool IsPossible()
        {
            return Voxel.IsValid && !VoxelHelpers.VoxelIsCompletelySurrounded(Voxel);
        }

        public override float Heuristic(TemporaryVoxelHandle voxel)
        {
            return (voxel.Coordinate.ToVector3() - Voxel.Coordinate.ToVector3()).LengthSquared();
        }

        public VoxelGoalRegion(TemporaryVoxelHandle voxel)
        {
            Voxel = voxel;
        }

        public override bool IsInGoalRegion(TemporaryVoxelHandle voxel)
        {
            return Voxel == voxel;
        }

        public override TemporaryVoxelHandle GetVoxel()
        {
            return Voxel;
        }
    }
}
