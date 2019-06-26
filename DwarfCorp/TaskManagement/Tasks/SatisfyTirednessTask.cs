using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should find a bed and sleep (or else pass out).
    /// </summary>
    public class SatisfyTirednessTask : Task
    {
        public SatisfyTirednessTask()
        {
            ReassignOnDeath = false;
            Name = "Go to sleep";
            Priority = TaskPriority.High;
            BoredomIncrease = GameSettings.Default.Boredom_Sleep;
        }

        public override Act CreateScript(Creature agent)
        {
            return new FindBedAndSleepAct(agent.AI);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.Sensor.Enemies.Count == 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return agent.Stats.Hunger.IsDissatisfied() ? 0.0f : 1e13f;
        }

    }

    /// <summary>
    /// Tells a creature that it should find a bed and sleep (or else pass out).
    /// </summary>
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
