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
            Name = "Satisfy Tiredness";
            Priority = PriorityType.High;
        }

        public override Task Clone()
        {
            return new SatisfyTirednessTask();
        }

        public override Act CreateScript(Creature agent)
        {
            return new FindBedAndSleepAct(agent.AI);
        }

        public override bool IsFeasible(Creature agent)
        {
            return agent.Sensors.Enemies.Count == 0;
        }

        public override float ComputeCost(Creature agent)
        {
            return agent.Status.Hunger.IsUnhappy() ? 0.0f : 1e13f;
        }

    }
}
