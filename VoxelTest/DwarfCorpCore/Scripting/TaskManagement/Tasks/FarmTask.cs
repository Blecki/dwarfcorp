using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.TaskManagement.Tasks
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class FarmTask : Task
    {
        public Farm FarmToWork { get; set; }

        public FarmTask()
        {
            Priority = PriorityType.Low;
        }

        public FarmTask(Farm farmToWork)
        {
            FarmToWork = farmToWork;
            Name = "Work " + FarmToWork.ID;
            Priority = PriorityType.Low;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

        public override bool IsFeasible(Creature agent)
        {
            return FarmToWork != null;
        }

        public override Act CreateScript(Creature agent)
        {
            return new FarmAct(agent.AI) {FarmToWork = FarmToWork, Name = "Work " + FarmToWork.ID};
        }

        public override float ComputeCost(Creature agent)
        {
            if (FarmToWork == null) return float.MaxValue;
            else
            {
                return (FarmToWork.GetBoundingBox().Center() - agent.AI.Position).LengthSquared();
            }
        }

        public override Task Clone()
        {
            return new FarmTask() {FarmToWork = FarmToWork};
        }
    }
}
