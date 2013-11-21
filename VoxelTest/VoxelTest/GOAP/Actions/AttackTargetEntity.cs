using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class AttackTargetEntity : Action
    {
        public AttackTargetEntity()
        {
            Name = "AttackTargetEntity";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.Entity;
            PreCondition[GOAPStrings.AtTarget] = true;
            PreCondition[GOAPStrings.TargetDead] = false;
            PreCondition[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            Effects = new WorldState();
            Effects[GOAPStrings.TargetDead] = true;

            Cost = 1.0f;
        }

        public override Action.ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            if(creature.TargetComponent == null || creature.TargetComponent.IsDead)
            {
                return ValidationStatus.Invalid;
            }
            else
            {
                return ValidationStatus.Ok;
            }
        }

        public override Action.PerformStatus PerformContextAction(CreatureAIComponent creature, Microsoft.Xna.Framework.GameTime time)
        {
            CreatureAIComponent.PlannerSuccess code = creature.MeleeAttack(time);

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
    }

}