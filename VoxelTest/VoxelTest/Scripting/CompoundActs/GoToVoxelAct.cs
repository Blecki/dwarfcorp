using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GoToVoxelAct : CompoundCreatureAct
    {
        public VoxelRef Voxel { get; set; }

        public GoToVoxelAct(VoxelRef voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Go to Voxel";
            if (Voxel != null)
            {
                Tree = new Sequence(new SetTargetVoxelAct(voxel, Agent),
                                    new PlanAct(Agent),
                                    new FollowPathAct(Agent),
                                    new StopAct(Agent));
            }
            else
            {
                Tree = new Sequence(
                    new PlanAct(Agent),
                    new FollowPathAct(Agent),
                    new StopAct(Agent));
            }
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }
}
