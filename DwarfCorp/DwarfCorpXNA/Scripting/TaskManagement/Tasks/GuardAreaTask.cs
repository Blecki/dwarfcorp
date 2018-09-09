// GuardVoxelTask.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class GuardZoneTask : Task
    {
        /// <summary>
        /// Exists to pass voxel designation based task cancelling on to parent task.
        /// </summary>
        public class DesignationProxyTask : Task
        {
            public GuardZoneTask ParentTask;
            public VoxelHandle Voxel;

            public DesignationProxyTask(GuardZoneTask ParentTask, VoxelHandle Voxel)
            {
                this.ParentTask = ParentTask;
                this.Voxel = Voxel;
            }

            public override void OnDequeued(Faction Faction)
            {
                Faction.Designations.RemoveVoxelDesignation(Voxel, DesignationType.Guard);
                var key = VoxelHelpers.GetVoxelQuickCompare(Voxel);
                Faction.GuardedVoxels.Remove(key);

                if (Faction.GuardedVoxels.Count == 0)
                    Faction.World.Master.TaskManager.CancelTask(ParentTask);
            }
        }

        public GuardZoneTask()
        {
            Category = TaskCategory.Guard;
            Name = "Guard Area";
            Priority = PriorityType.Eventually;
            MaxAssignable = Int32.MaxValue;
            BoredomIncrease = GameSettings.Default.Boredom_BoringTask;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GuardAreaAct(agent.AI);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return agent.Faction.GuardedVoxels.Count() > 0;
        }

        public override void Render(DwarfTime time)
        {

        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;
            return agent.Faction.GuardedVoxels.Count > 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override void OnEnqueued(Faction Faction)
        {
            // Guard tool inserts the designation.
            // Todo: Setup voxel change hook here.
        }

        public override void OnDequeued(Faction Faction)
        {
            // Task is cancelled - get rid of all guard designations.
            foreach (var vox in Faction.GuardedVoxels)
                Faction.Designations.RemoveVoxelDesignation(vox.Value, DesignationType.Guard);
            Faction.GuardedVoxels.Clear();

            // Todo: Cleanup voxel change hook here.
        }
    }
}