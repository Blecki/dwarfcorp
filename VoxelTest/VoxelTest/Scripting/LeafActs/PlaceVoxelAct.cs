using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class PlaceVoxelAct : CreatureAct
    {
        public VoxelRef Voxel { get; set; }

        public PlaceVoxelAct(VoxelRef voxel, CreatureAIComponent agent) :
            base(agent)
        {
            Agent = agent;
            Voxel = voxel;
            Name = "Build Voxel " + voxel.ToString();
        }

        public override IEnumerable<Status> Run()
        {
            LocatableComponent grabbed = Creature.Hands.GetFirstGrab();

            if(grabbed == null)
            {
                yield return Status.Fail;
            }
            else
            {
                if(Creature.Master.PutDesignator.IsDesignation(Voxel))
                {
                    Creature.Hands.UnGrab(grabbed);
                    grabbed.Die();
                    Agent.Blackboard.SetData<object>("HeldObject", null);

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