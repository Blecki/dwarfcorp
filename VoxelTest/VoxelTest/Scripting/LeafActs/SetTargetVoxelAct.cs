using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class SetTargetVoxelAct : CreatureAct
    {
        public VoxelRef Voxel { get; set; }

        public SetTargetVoxelAct(VoxelRef voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Voxel = voxel;
        }

        public override IEnumerable<Status> Run()
        {
            if (Voxel == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Agent.TargetVoxel = Voxel;
                yield return Status.Success;
            }
        }

    }
}
