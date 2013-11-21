using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class AttackTargetVoxel : Action
    {
        public AttackTargetVoxel()
        {
            Name = "AttackTargetVoxel";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.Voxel;
            PreCondition[GOAPStrings.AtTarget] = true;
            PreCondition[GOAPStrings.TargetDead] = false;
            PreCondition[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            Effects = new WorldState();
            Effects[GOAPStrings.TargetDead] = true;

            Cost = 1.0f;
        }

        public override PerformStatus PerformContextAction(CreatureAIComponent creature, Microsoft.Xna.Framework.GameTime time)
        {
            CreatureAIComponent.PlannerSuccess code = creature.Dig(time);

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
            Voxel vox = creature.TargetVoxel.GetVoxel(creature.Master.Chunks, false);

            if(vox == null || creature.TargetVoxel == null || vox.Health < 0)
            {
                return ValidationStatus.Invalid;
            }
            else
            {
                if(creature.Master.IsDigDesignation(vox))
                {
                    creature.Master.GetDigDesignation(vox).numCreaturesAssigned++;
                }

                return ValidationStatus.Ok;
            }
        }
    }

}