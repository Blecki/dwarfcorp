using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GoToVoxel : CompoundCreatureAct
    {
        public VoxelRef Voxel { get; set; }

        public GoToVoxel(VoxelRef voxel, CreatureAIComponent creature, float planRateLimit, int maxExpansions) :
            base(creature)
        {
            Voxel = voxel;
            Tree = new Sequence(new SetTargetVoxelAct(voxel, Agent),
                                new PlanAct(Agent, planRateLimit, maxExpansions),
                                new FollowPathAct(Agent),
                                new StopAct(Agent, Agent.Stats.StoppingForce));
        }
    }
}
