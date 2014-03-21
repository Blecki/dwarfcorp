using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature puts the item currently in its hands into a zone.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PutItemInZoneAct : CreatureAct
    {
        public Zone Pile { get; set; }
        public string VoxelID { get; set; }

        public PutItemInZoneAct(CreatureAIComponent agent, Zone stock, string voxel) :
            base(agent)
        {
            Name = "Put Item";
            Pile = stock;
            VoxelID = voxel;
        }

        public override IEnumerable<Status> Run()
        {
            LocatableComponent grabbed = Creature.Hands.GetFirstGrab();
            VoxelRef voxel = Agent.Blackboard.GetData<VoxelRef>(VoxelID);
            if(grabbed == null || voxel == null)
            {
                yield return Status.Fail;
            }
            else if (Pile.AddItem(grabbed, voxel))
            {
                Creature.Hands.UnGrab(grabbed);

                Matrix m = Matrix.Identity;
                m.Translation = voxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f);
                grabbed.LocalTransform = m;
                grabbed.HasMoved = true;
                grabbed.DrawBoundingBox = false;


                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }
    }

}