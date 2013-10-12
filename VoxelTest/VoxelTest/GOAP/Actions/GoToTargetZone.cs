using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class GoToTargetZone : Action
    {
        public GoToTargetZone()
        {
            Name = "GoToTargetZone";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.Zone;
            PreCondition[GOAPStrings.AtTarget] = false;
            PreCondition[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            Effects = new WorldState();
            Effects[GOAPStrings.AtTarget] = true;
            Effects[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Moving;
            Cost = 1.0f;
        }

        public override void Apply(WorldState state)
        {
            state[GOAPStrings.CurrentZone] = state[GOAPStrings.TargetZone];
            base.Apply(state);
        }

        public override void UnApply(WorldState state)
        {
            state[GOAPStrings.CurrentZone] = null;
            base.UnApply(state);
        }

        public override void UndoEffects(WorldState state)
        {
            state[GOAPStrings.CurrentZone] = null;
            base.UndoEffects(state);
        }

        public override Action.ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            if (creature.TargetVoxel == null)
            {
                Zone z = (Zone)creature.Goap.Belief[GOAPStrings.TargetZone];

                if (z is Stockpile)
                {
                    Stockpile s = (Stockpile)z;


                    VoxelRef v = s.GetNearestFreeVoxel(creature.Physics.GlobalTransform.Translation);

                    if (v != null)
                    {
                        creature.TargetVoxel = v;
                    }

                    if (creature.TargetVoxel != null)
                    {
                        s.SetReserved(v, true);
                    }
                    else
                    {
                        return ValidationStatus.Invalid;
                    }
                }
                else if (z is Room)
                {
                    Room r = (Room)z;

                    int index = r.GetClosestDesignationTo(creature.Physics.GlobalTransform.Translation);

                    if (index != -1)
                    {
                        creature.TargetVoxel = r.Designations[index];
                    }
                    else
                    {
                        Console.Error.WriteLine("Failed to set target voxel.");
                        return ValidationStatus.Invalid;
                    }
                }
                else
                {
                    return ValidationStatus.Invalid;
                }

            }
            else
            {
                return ValidationStatus.Invalid;
            }
            
            return ValidationStatus.Ok;
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
            }

            return PerformStatus.InProgress;
        }
    }
}
