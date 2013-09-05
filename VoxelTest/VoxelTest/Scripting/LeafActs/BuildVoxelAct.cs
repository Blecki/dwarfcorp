using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class BuildVoxelAct : CreatureAct
    {
        public VoxelRef Voxel { get; set; }

        public BuildVoxelAct(VoxelRef voxel, CreatureAIComponent agent) :
            base(agent)
        {
            Agent = agent;
            Voxel = voxel;
            Name = "Build Voxel " + voxel.ToString();
        }

        public override IEnumerable<Status> Run()
        {
            LocatableComponent grabbed = Creature.Hands.GetFirstGrab();

            if (grabbed == null)
            {
                yield return Status.Fail;
            }
            else
            {

                if (Creature.Master.PutDesignator.IsDesignation(Voxel))
                {
                    grabbed.IsStocked = true;
                    Creature.Hands.UnGrab(grabbed);
                    grabbed.Die();

                    PutDesignation put = Creature.Master.PutDesignator.GetDesignation(Voxel);
                    put.Put(Creature.Master.Chunks);


                    Creature.Master.PutDesignator.Designations.Remove(put);
                    yield return Status.Success;
                }
                else
                {
                    yield return Status.Fail;
                }

            }
        }
    }
}
