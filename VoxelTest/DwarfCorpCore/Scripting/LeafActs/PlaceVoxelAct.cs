using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

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

            foreach (Status status in Creature.HitAndWait(1.0f, true))
            {
                if (status == Status.Running)
                {
                    yield return status;
                }
            }

            Body grabbed = Creature.Inventory.RemoveAndCreate(Resource).FirstOrDefault();

            if(grabbed == null)
            {
                yield return Status.Fail;
            }
            else
            {
                if(Creature.Faction.WallBuilder.IsDesignation(Voxel))
                {
                    TossMotion motion = new TossMotion(1.0f, 2.0f, grabbed.LocalTransform, Voxel.Position + new Vector3(0.5f, 0.5f, 0.5f));
                    motion.OnComplete += grabbed.Die;
                    grabbed.AnimationQueue.Add(motion);

                    WallBuilder put = Creature.Faction.WallBuilder.GetDesignation(Voxel);
                    put.Put(PlayState.ChunkManager);


                    Creature.Faction.WallBuilder.Designations.Remove(put);
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