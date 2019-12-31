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
            BoredomIncrease = GameSettings.Current.Boredom_Sleep;
            EnergyDecrease = GameSettings.Current.Energy_Restful;
        }

        public override MaybeNull<Act> CreateScript(Creature agent)
        {
            return new FindBedAndSleepAct(agent.AI);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.Stats.Energy.IsCritical()) return Feasibility.Feasible;
            return agent.Sensor.Enemies.Count == 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return agent.Stats.Hunger.IsDissatisfied() ? 0.0f : 1e13f;
        }

    }
}
