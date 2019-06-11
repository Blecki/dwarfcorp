using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class SatisfyHungerTask : Task
    {
        public bool MustPay = false;

        public SatisfyHungerTask()
        {
            ReassignOnDeath = false;
            Name = "Satisfy Hunger";
            Priority = PriorityType.Medium;
            BoredomIncrease = GameSettings.Default.Boredom_Eat;
        }

        public override Act CreateScript(Creature agent)
        {
            return new FindAndEatFoodAct(agent.AI, MustPay);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return agent.Stats.Hunger.IsDissatisfied() ? 0.0f : 1e13f;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.Stats.Hunger.IsCritical())
                return Feasibility.Feasible; // Hunger is more important right now than hostiles!
            
            return agent.Sensor.Enemies.Count == 0 ? Feasibility.Feasible : Feasibility.Infeasible ;
        }
    }
}
