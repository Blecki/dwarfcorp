using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
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
                if(Creature.Faction.PutDesignator.IsDesignation(Voxel))
                {
                    Creature.Hands.UnGrab(grabbed);
                    grabbed.Die();
                    Agent.Blackboard.SetData<object>("HeldObject", null);

                    PutDesignation put = Creature.Faction.PutDesignator.GetDesignation(Voxel);
                    put.Put(PlayState.ChunkManager);


                    Creature.Faction.PutDesignator.Designations.Remove(put);
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