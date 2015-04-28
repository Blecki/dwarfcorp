using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GuardVoxel : Goal
    {
        public VoxelRef VoxelToGuard = null;

        public GuardVoxel(GOAP agent, VoxelRef vox)
        {
            Name = "Guard Voxel: " + vox.WorldPosition;
            Priority = 0.1f;
            VoxelToGuard = vox;
            Reset(agent);
        }

        public override void Reset(GOAP agent)
        {
            if(agent != null)
            {
                agent.Voxels.Add(VoxelToGuard);
            }
            State[GOAPStrings.TargetType] = GOAP.TargetType.Voxel;
            State[GOAPStrings.TargetVoxel] = VoxelToGuard;
            State[GOAPStrings.AtTarget] = true;
            State[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            base.Reset(agent);
        }


        public override void ContextReweight(CreatureAIComponent creature)
        {
            if(VoxelToGuard == null)
            {
                Priority = 0.0f;
            }
            else
            {
                Priority = 0.1f / ((creature.Physics.GlobalTransform.Translation - VoxelToGuard.WorldPosition).LengthSquared() + 0.01f);
                Cost = (VoxelToGuard.WorldPosition - creature.Physics.GlobalTransform.Translation).LengthSquared();
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            Voxel vox = VoxelToGuard.GetVoxel(creature.Master.Chunks, false);
            if(VoxelToGuard == null || vox.Health <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            return new GuardVoxelAct(creature, VoxelToGuard);
        }
    }

}