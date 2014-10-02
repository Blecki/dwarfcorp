using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Stop : Action
    {

        public Stop()
        {
            Name = "Stop";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Moving;

            Effects = new WorldState();
            Effects[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            Cost = 1.0f;
        }

        public override PerformStatus PerformContextAction(CreatureAIComponent creature, Microsoft.Xna.Framework.GameTime time)
        {
            CreatureAIComponent.PlannerSuccess code = creature.Stop(time);

            if (code == CreatureAIComponent.PlannerSuccess.Success)
            {
                return PerformStatus.Success;
            }
            else if (code == CreatureAIComponent.PlannerSuccess.Failure)
            {
                return PerformStatus.Invalid;
            }

            return PerformStatus.InProgress;
        }

        public override Action.ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            return ValidationStatus.Ok;
        }


    }
}
