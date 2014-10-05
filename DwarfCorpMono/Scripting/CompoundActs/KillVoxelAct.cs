using System;
using System.Collections.Generic;
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
            Tree = new Sequence(new GoToVoxelAct(voxel, creature),
                                new DigAct(Agent));
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }
}
