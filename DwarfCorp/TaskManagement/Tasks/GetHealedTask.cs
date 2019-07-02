using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GetHealedTask : Task
    {
        public GetHealedTask()
        {
            Name = "Heal thyself";
            Priority = TaskPriority.Urgent;
            ReassignOnDeath = false;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GetHealedAct(agent.AI);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            GameComponent closestItem = agent.Faction.FindNearestItemWithTags("Bed", agent.AI.Position, true, agent.AI);

            return (closestItem != null && agent.AI.Stats.Health.IsDissatisfied()) || agent.AI.Stats.Health.IsCritical() ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 0.0f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

    }
}
