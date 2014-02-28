using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class PutItemInStockpileAct : CreatureAct
    {
        public Stockpile Pile { get; set; }

        public PutItemInStockpileAct(CreatureAIComponent agent, Stockpile stock) :
            base(agent)
        {
            Name = "Put Item";
            Pile = stock;
        }

        public override IEnumerable<Status> Run()
        {
            if (Pile == null && Agent.TargetStockpile != null)
            {
                Pile = Agent.TargetStockpile;
            }
            else if (Pile != null)
            {
                Agent.TargetStockpile = Pile;
            }
            else
            {
                yield return Status.Fail;
            }
          

            LocatableComponent grabbed = Creature.Hands.GetFirstGrab();

            if (grabbed == null)
            {
                yield return Status.Fail;
            }
            else if (Pile.PutResource(grabbed, Agent.TargetVoxel.GetVoxel(Creature.Master.Chunks, false)))
            {
                Creature.Hands.UnGrab(grabbed);

                Matrix m = Matrix.Identity;
                m.Translation = Agent.TargetVoxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f);
                grabbed.LocalTransform = m;
                grabbed.HasMoved = true;
                grabbed.DrawBoundingBox = false;
                grabbed.IsStocked = true;
            

                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }
    }
}
