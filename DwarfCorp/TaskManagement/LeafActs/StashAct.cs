using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class StashAct : CreatureAct
    {
        public Zone Zone { get; set; }
        public string TargetName { get; set; }
        public string StashedItemOut { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public GameComponent Target { get { return GetTarget(); } set { SetTarget(value); } }

        public StashAct()
        {

        }

        public StashAct(CreatureAI agent, Zone zone, string targetName, string stashedItemOut) :
            base(agent)
        {
            Name = "Stash " + targetName;
            Zone = zone;
            TargetName = targetName;
            StashedItemOut = stashedItemOut;
        }

        public GameComponent GetTarget()
        {
            return Agent.Blackboard.GetData<GameComponent>(TargetName);
        }

        public void SetTarget(GameComponent targt)
        {
            Agent.Blackboard.SetData(TargetName, targt);
        }

        public override IEnumerable<Status> Run()
        {
            if (Target == null)
            {
                yield return Status.Fail;
            }

            if (Target is CoinPile)
            {
                DwarfBux money = (Target as CoinPile).Money;
                Creature.AI.AddMoney(money);
                Target.Die();
            }
            else if (!Creature.Inventory.Pickup(Target, Inventory.RestockType.RestockResource))
            {
                yield return Status.Fail;
            }

            //if (Creature.Faction.Designations.IsDesignation(Target, DesignationType.Gather))
            //    Creature.Faction.Designations.RemoveEntityDesignation(Target, DesignationType.Gather);
            //else
            //{
            //    yield return Status.Fail;
            //    break;
            //}

            if (Target is ResourceEntity resEnt)
            {
                var resource = resEnt.Resource;
                // Todo: This is disgusting.
                //var resource = new ResourceAmount(Target.Tags[0], 1);
                Agent.Blackboard.SetData(StashedItemOut, resource);
                Agent.Creature.NoiseMaker.MakeNoise("Stash", Agent.Position);
                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }        
    }    
}