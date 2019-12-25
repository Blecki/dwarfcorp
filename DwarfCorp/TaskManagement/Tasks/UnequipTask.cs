using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should pick up an item and put it in a stockpile.
    /// </summary>
    internal class UnequipTask : Task
    {
        public Resource Resource;

        public UnequipTask()
        {
            Priority = TaskPriority.Medium;
            Category = TaskCategory.Other;
            BoredomIncrease = 0;
            EnergyDecrease = 0;
        }

        public UnequipTask(Resource Resource) : this()
        {
            this.Resource = Resource;
            Name = "Unequip: " + Resource.DisplayName;
        }

        public override MaybeNull<Act> CreateScript(Creature Agent)
        {
            return new UnequipToolAct(Agent.AI, Resource);
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