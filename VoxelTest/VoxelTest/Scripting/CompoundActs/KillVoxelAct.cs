using System;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class KillVoxelAct : CompoundCreatureAct
    {
        public VoxelRef Voxel { get; set; }

        public KillVoxelAct(VoxelRef voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Kill Voxel ";
            Tree = new Sequence(
                                new GoToVoxelAct(voxel, creature),
                                new SetBlackboardData<VoxelRef>(creature, "DigVoxel", voxel),
                                new DigAct(Agent, "DigVoxel"),
                                new ClearBlackboardData(creature, "DigVoxel")
                                );
        }
    }

}