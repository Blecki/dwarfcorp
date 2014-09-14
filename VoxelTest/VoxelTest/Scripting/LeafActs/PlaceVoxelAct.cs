using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;

namespace DwarfCorp
{
    /// <summary>
    /// A creature uses the item currently in its hands to construct a voxel.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PlaceVoxelAct : CreatureAct
    {
        public Voxel Voxel { get; set; }
        public ResourceAmount Resource { get; set; }
        public PlaceVoxelAct(Voxel voxel, CreatureAI agent, ResourceAmount resource) :
            base(agent)
        {
            Agent = agent;
            Voxel = voxel;
            Name = "Build Voxel " + voxel.ToString();
            Resource = resource;
        }

        public override IEnumerable<Status> Run()
        {
            Body grabbed = Creature.Inventory.RemoveAndCreate(Resource).FirstOrDefault();

            if(grabbed == null)
            {
                yield return Status.Fail;
            }
            else
            {
                if(Creature.Faction.PutDesignator.IsDesignation(Voxel))
                {
                    grabbed.Die();

                    WallBuilder put = Creature.Faction.PutDesignator.GetDesignation(Voxel);
                    put.Put(PlayState.ChunkManager);


                    Creature.Faction.PutDesignator.Designations.Remove(put);
                    Creature.Stats.NumBlocksPlaced++;
                    yield return Status.Success;
                }
                else
                {
                    Creature.Inventory.Resources.AddItem(grabbed);
                    grabbed.Die();
                    
                    yield return Status.Fail;
                }
            }
        }
    }

}