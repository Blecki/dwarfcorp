using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class Wander : Action
    {
        public Wander()
        {
            Name = "Wander";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            Effects = new WorldState();
            Effects[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Moving;

            Cost = 1.0f;
        }

        public override PerformStatus PerformContextAction(CreatureAIComponent creature, Microsoft.Xna.Framework.GameTime time)
        {
            CreatureAIComponent.PlannerSuccess code = creature.Wander(time, 0.5f);

            if(code == CreatureAIComponent.PlannerSuccess.Success)
            {
                return PerformStatus.Success;
            }
            else if(code == CreatureAIComponent.PlannerSuccess.Failure)
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