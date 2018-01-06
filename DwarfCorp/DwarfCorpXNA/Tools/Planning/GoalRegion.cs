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
    /// A goal region is an abstract way of specifing when a dwarf has reached a goal.
    /// </summary>
    public abstract class GoalRegion
    {
        /// <summary>
        /// Determines whetherthe specified voxel is within the goal region.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <returns>
        ///   <c>true</c> if [is in goal region] [the specified voxel]; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsInGoalRegion(VoxelHandle voxel);
        /// <summary>
        /// Gets a voxel associated with this goal region.
        /// </summary>
        /// <returns>The voxel associated with this goal region.</returns>
        public abstract VoxelHandle GetVoxel();
        /// <summary>
        /// Returns an admissible heuristic for A* planning from the given voxel to this region.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <returns>An admissible heuristic value.</returns>
        public abstract float Heuristic(VoxelHandle voxel);
        /// <summary>
        /// Determines whether the goal is a.priori possible.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is possible; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsPossible();

        public abstract bool IsReversible();
    }
}
