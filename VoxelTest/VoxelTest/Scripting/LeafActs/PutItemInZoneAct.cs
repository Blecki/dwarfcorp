using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    public class PutItemInZoneAct : CreatureAct
    {
        public Zone Pile { get; set; }

        public PutItemInZoneAct(CreatureAIComponent agent, Zone stock) :
            base(agent)
        {
            Name = "Put Item";
            Pile = stock;
        }

        public override IEnumerable<Status> Run()
        {
            LocatableComponent grabbed = Creature.Hands.GetFirstGrab();

            if(grabbed == null)
            {
                yield return Status.Fail;
            }
            else if(Pile.AddItem(grabbed, Agent.TargetVoxel))
            {
                Creature.Hands.UnGrab(grabbed);

                Matrix m = Matrix.Identity;
                m.Translation = Agent.TargetVoxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f);
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