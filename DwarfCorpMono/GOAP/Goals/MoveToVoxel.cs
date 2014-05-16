using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    class MoveToVoxel : Goal
    {
        VoxelRef m_voxel = null;
        public MoveToVoxel(GOAP agent, VoxelRef voxel)
        {
            Name = "Go to Voxel: " + voxel.WorldPosition;
            Priority = 0.1f;
            m_voxel = voxel;
            Reset(agent);
        }

        public override void Reset(GOAP agent)
        {

            State[GOAPStrings.TargetType] = GOAP.TargetType.Voxel;
            State[GOAPStrings.TargetVoxel] = m_voxel;
            State[GOAPStrings.AtTarget] = true;
            State[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            base.Reset(agent);
        }



        public override void ContextReweight(CreatureAIComponent creature)
        {
                Priority = 0.5f;
                Cost = 1.0f - Priority;
            
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            return true;
        }
    }
}
