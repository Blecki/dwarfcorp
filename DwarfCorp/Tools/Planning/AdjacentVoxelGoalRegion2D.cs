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
    /// This is a GoalRegion which tells the dwarf to be 4-ways adjacent to the voxel in X and Z.
    /// </summary>
    /// <seealso cref="GoalRegion" />
    public class AdjacentVoxelGoalRegion2D : GoalRegion
    {
        public VoxelHandle Voxel { get; set; }

        public override bool IsReversible()
        {
            return true;
        }

        public override bool IsPossible()
        {
            return Voxel.IsValid && !VoxelHelpers.VoxelIsCompletelySurrounded(Voxel);
        }

        public override float Heuristic(VoxelHandle voxel)
        {
            return (voxel.WorldPosition - Voxel.WorldPosition).LengthSquared();
        }

        public AdjacentVoxelGoalRegion2D(VoxelHandle Voxel)
        {
            this.Voxel = Voxel;
        }

        public override bool IsInGoalRegion(VoxelHandle query)
        {
            if (!query.IsValid)
                return false;

            int diffX = query.Coordinate.X - Voxel.Coordinate.X;
            int diffY = query.Coordinate.Y - Voxel.Coordinate.Y;
            int diffZ = query.Coordinate.Z - Voxel.Coordinate.Z;

            return (diffY >= 0 && diffY <= 1) && ((diffX == 0 && Math.Abs(diffZ) <= 1) || (diffZ == 0 && Math.Abs(diffX) <= 1));
        }

        public override VoxelHandle GetVoxel()
        {
            return Voxel;
        }
    }
}
