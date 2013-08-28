using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    class KillVoxel : Goal
    {
        VoxelRef voxelToKill = null;
        public KillVoxel(GOAP agent, VoxelRef vox)
        {
            Name = "Kill Voxel: " + vox.WorldPosition;
            Priority = 0.1f;
            voxelToKill = vox;
            Reset(agent);
        }

        public override void Reset(GOAP agent)
        {
            if (agent != null) { agent.Voxels.Add(voxelToKill); }
            State[GOAPStrings.TargetType] = GOAP.TargetType.Voxel;
            State[GOAPStrings.TargetDead] = true;
            State[GOAPStrings.TargetVoxel] = voxelToKill;
            State[GOAPStrings.AtTarget] = true;
            State[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;
            base.Reset(agent);
        }


        public override void ContextReweight(CreatureAIComponent creature)
        {
            Voxel vox = voxelToKill.GetVoxel(creature.Master.Chunks, false);
            if (vox == null)
            {
                Priority = 0.0f;
                Cost = 999f;
            }
            else
            {
                if (vox.Chunk.IsCompletelySurrounded(voxelToKill))
                {
                    Priority = 0.0f;
                    Cost = 999f;
                }
                else
                {
                    Priority = 0.1f / ((creature.Physics.GlobalTransform.Translation - voxelToKill.WorldPosition).LengthSquared() + 0.01f) + (float)PlayState.random.NextDouble() * 0.1f;
                    Cost = ((creature.Physics.GlobalTransform.Translation - voxelToKill.WorldPosition).LengthSquared());
                }
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            Voxel vox = voxelToKill.GetVoxel(creature.Master.Chunks, false);
            if (vox == null || vox.Health <= 0)
            {
                return false;
            }
            else
            {

                if (creature.Master.IsDigDesignation(vox))
                {
                    creature.Master.GetDigDesignation(vox).numCreaturesAssigned++;
                }
                else
                {
                    return false;
                }
                 

                return true;
            }
        }


    }

}
