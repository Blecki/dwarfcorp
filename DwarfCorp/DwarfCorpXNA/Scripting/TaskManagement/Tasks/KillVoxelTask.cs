// KillVoxelTask.cs
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
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should destroy a voxel via digging.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class KillVoxelTask : Task
    {
        public VoxelHandle Voxel = VoxelHandle.InvalidHandle;
        public float VoxelHealth { get; set; }

        public KillVoxelTask()
        {
            MaxAssignable = 3;
            Priority = PriorityType.Medium;
            Category = TaskCategory.Dig;
        }

        public KillVoxelTask(VoxelHandle vox)
        {
            MaxAssignable = 3;
            Name = "Mine Block " + vox.Coordinate;
            Voxel = vox;
            Priority = PriorityType.Low;
            Category = TaskCategory.Dig;
            VoxelHealth = Voxel.Type.StartingHealth;
        }

        public override Act CreateScript(Creature creature)
        {
            return new KillVoxelAct(creature.AI, this);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return !Voxel.IsEmpty;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.IsTaskAllowed(Task.TaskCategory.Dig))
                return Feasibility.Infeasible;

            if (!Voxel.IsValid || Voxel.IsEmpty)
                return Feasibility.Infeasible;

            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;

            if (VoxelHelpers.VoxelIsCompletelySurrounded(Voxel) || VoxelHelpers.VoxelIsSurroundedByWater(Voxel))
                return Feasibility.Infeasible;

            return Feasibility.Feasible;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return !Voxel.IsValid || Voxel.IsEmpty;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (!Voxel.IsValid)
                return 1000;

            int surroundedValue = 0;
            float freeTopValue = 0;
            if (!alreadyCheckedFeasible)
            {
                if (VoxelHelpers.VoxelIsCompletelySurrounded(Voxel))
                    surroundedValue = 10000;

                var above = VoxelHelpers.GetVoxelAbove(Voxel);
                if (above.IsValid && !above.IsEmpty)
                {
                    freeTopValue = 100;
                }
            }

            return (agent.AI.Position - Voxel.WorldPosition).LengthSquared() + 10 * Math.Abs(VoxelConstants.ChunkSizeY - Voxel.Coordinate.Y) + surroundedValue + freeTopValue;
        }

        public override bool IsComplete(Faction faction)
        {
            if (!Voxel.IsValid) return false;
            return Voxel.IsEmpty;
        }

        public override void OnDequeued(Faction Faction)
        {
            Faction.Designations.RemoveVoxelDesignation(Voxel, DesignationType.Dig);
        }
        
    }

}