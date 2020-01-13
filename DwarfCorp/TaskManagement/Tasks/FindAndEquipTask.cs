using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class FindAndEquipTask : Task
    {
        public String ResourceApparentType;

        public FindAndEquipTask()
        {
            Priority = TaskPriority.Medium;
            Category = TaskCategory.Other;
            BoredomIncrease = 0;
            EnergyDecrease = 0;
        }

        public FindAndEquipTask(String ResourceApparentType) : this()
        {
            this.ResourceApparentType = ResourceApparentType;
            Name = "Equip: " + ResourceApparentType;
        }

        public override MaybeNull<Act> CreateScript(Creature Agent)
        {
            return new Sequence(
                            new GetResourcesOfApparentType(Agent.AI, new List<ResourceApparentTypeAmount> { new ResourceApparentTypeAmount(ResourceApparentType, 1) }) { BlackboardEntry = "tool-stashed" },
                            new EquipAct(Agent.AI) { BlackboardEntry = "tool-stashed" }
                        );
        }

        public override bool ShouldDelete(Creature agent)
        {
            return IsFeasible(agent) == Feasibility.Infeasible;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return !agent.AI.Movement.IsSessile
                   && !agent.AI.Stats.IsAsleep ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1;
        }
        
        public override bool IsComplete(WorldManager World)
        {
            return false;
        }

        public override void OnEnqueued(WorldManager World)
        {
        }

        public override void OnDequeued(WorldManager World)
        {
        }

        public override Vector3? GetCameraZoomLocation()
        {
            return null;
        }
    }

}