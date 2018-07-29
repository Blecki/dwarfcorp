// FarmTask.cs
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

namespace DwarfCorp
{
    public class PlantTask : Task
    {
        public Farm FarmToWork { get; set; }
        public string Plant { get; set; }
        public List<ResourceAmount> RequiredResources { get; set; } 

        public PlantTask()
        {
            Priority = PriorityType.Low;
            Category = TaskCategory.Plant;
            BoredomIncrease = 0.2f;
        }

        public PlantTask(Farm farmToWork)
        {
            FarmToWork = farmToWork;
            Name = "Plant " + FarmToWork.Voxel.Coordinate;
            Priority = PriorityType.Low;
            AutoRetry = true;
            Category = TaskCategory.Plant;
            BoredomIncrease = 0.2f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return true;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return IsFeasible(agent) == Feasibility.Infeasible;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.IsTaskAllowed(Task.TaskCategory.Plant))
                return Feasibility.Infeasible;

            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;

            if (FarmToWork == null)
                return Feasibility.Infeasible;

            if (FarmToWork.Finished)
                return Feasibility.Infeasible;

            if (!agent.Faction.HasResources(RequiredResources))
                return Feasibility.Infeasible;

            return Feasibility.Feasible;
        }

        public override bool IsComplete(Faction faction)
        {
            if (FarmToWork == null) return true;
            if (FarmToWork.Finished) return true;
            if (FarmToWork.Voxel.IsEmpty) return true;
            if (!FarmToWork.Voxel.Type.IsSoil) return true;
            return false;
        }

        private IEnumerable<Act.Status> Cleanup(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                if (creature.Faction == creature.World.PlayerFaction)
                {
                    creature.World.MakeAnnouncement(String.Format("{0} cancelled farming task because it is unreachable", creature.Stats.FullName));
                    creature.World.Master.TaskManager.CancelTask(this);
                }
                yield return Act.Status.Fail;
                yield break;
            }
            yield return Act.Status.Success;
        }

        public override Act CreateScript(Creature agent)
        {
            return (new PlantAct(agent.AI) { Resources = RequiredResources, FarmToWork = FarmToWork, Name = "Work " + FarmToWork.Voxel.Coordinate } 
            | new Wrap(() => Cleanup(agent.AI))) & new Wrap(() => Cleanup(agent.AI));
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (FarmToWork == null) return float.MaxValue;
            else
            {
                return (FarmToWork.Voxel.WorldPosition - agent.AI.Position).LengthSquared();
            }
        }

        public override void OnEnqueued(Faction Faction)
        {
            Faction.Designations.AddVoxelDesignation(FarmToWork.Voxel, DesignationType.Plant, FarmToWork, this);
        }

        public override void OnDequeued(Faction Faction)
        {
            Faction.Designations.RemoveVoxelDesignation(FarmToWork.Voxel, DesignationType.Plant);
        }
    }
}
