using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class GoToTargetVoxel : Action
    {
        public GoToTargetVoxel()
        {
            Name = "GoToTargetVoxel";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.Voxel;
            PreCondition[GOAPStrings.AtTarget] = false;
            PreCondition[GOAPStrings.TargetDead] = false;
            PreCondition[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;


            Effects = new WorldState();
            Effects[GOAPStrings.AtTarget] = true;
            Effects[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Moving;
            Cost = 1.0f;
        }

        public override ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            Voxel vox = creature.TargetVoxel.GetVoxel(creature.Master.Chunks, false);
            if (vox == null || vox.Health <= 0)
            {
                return ValidationStatus.Invalid;
            }
            else
            {
                return ValidationStatus.Ok;
            }
        }

        public override PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            CreatureAIComponent.PlannerSuccess successCode = CreatureAIComponent.PlannerSuccess.Wait;
            if (creature.CurrentPath == null)
            {
                successCode = creature.PlanPath(time);

                if (successCode == CreatureAIComponent.PlannerSuccess.Failure)
                {
                    return PerformStatus.Invalid;
                }
            }
            else
            {
                successCode = creature.Pathfind(time);
                if (successCode == CreatureAIComponent.PlannerSuccess.Success)
                {
                    return PerformStatus.Success;
                }
                else if (successCode == CreatureAIComponent.PlannerSuccess.Failure)
                {
                    return PerformStatus.Invalid;
                }
               
            }

            return PerformStatus.InProgress;
        }
    }
}
