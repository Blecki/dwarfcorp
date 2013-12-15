using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PutItemInStockpileAct : CreatureAct
    {
        public Stockpile Pile { get { return GetPile();  } set{ SetPile(value);} }

        public Stockpile GetPile()
        {
            return Agent.Blackboard.GetData<Stockpile>(StockpileName);
        }

        public void SetPile(Stockpile pile)
        {
            Agent.Blackboard.SetData<Stockpile>(StockpileName, pile);   
        }

        public string StockpileName { get; set; }

        public PutItemInStockpileAct(CreatureAIComponent agent, string stockpileName) :
            base(agent)
        {
            Name = "Put Item";
            StockpileName = stockpileName;
        }

        public override IEnumerable<Status> Run()
        {
            if(Pile == null && Agent.TargetStockpile != null)
            {
                Pile = Agent.TargetStockpile;
            }
            else if(Pile != null)
            {
                Agent.TargetStockpile = Pile;
            }
            else
            {
                yield return Status.Fail;
                yield break;
            }


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