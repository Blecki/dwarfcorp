using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    internal class GuardVoxel : Goal
    {
        private VoxelRef voxelToGuard = null;

        public GuardVoxel(GOAP agent, VoxelRef vox)
        {
            Name = "Guard Voxel: " + vox.WorldPosition;
            Priority = 0.1f;
            voxelToGuard = vox;
            Reset(agent);
        }

        public override void Reset(GOAP agent)
        {
            if(agent != null)
            {
                agent.Voxels.Add(voxelToGuard);
            }
            State[GOAPStrings.TargetType] = GOAP.TargetType.Voxel;
            State[GOAPStrings.TargetVoxel] = voxelToGuard;
            State[GOAPStrings.AtTarget] = true;
            State[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            base.Reset(agent);
        }


        public override void ContextReweight(CreatureAIComponent creature)
        {
            if(voxelToGuard == null)
            {
                Priority = 0.0f;
            }
            else
            {
                Priority = 0.1f / ((creature.Physics.GlobalTransform.Translation - voxelToGuard.WorldPosition).LengthSquared() + 0.01f);
                Cost = (voxelToGuard.WorldPosition - creature.Physics.GlobalTransform.Translation).LengthSquared();
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            Voxel vox = voxelToGuard.GetVoxel(creature.Master.Chunks, false);
            if(voxelToGuard == null || vox.Health <= 0)
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
            return new GuardVoxelAct(creature, voxelToGuard);
        }
    }

}