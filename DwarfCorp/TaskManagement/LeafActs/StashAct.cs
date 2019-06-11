using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature grabs a given item and puts it in their inventory
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class StashAct : CreatureAct
    {
        public enum PickUpType
        {
            None,
            Stockpile,
            Room
        }

        public Zone Zone { get; set; }

        public PickUpType PickType { get; set; }

        public string TargetName { get; set; }

        public string StashedItemOut { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public GameComponent Target { get { return GetTarget(); } set { SetTarget(value); } }

        public StashAct()
        {

        }

        public StashAct(CreatureAI agent, PickUpType type, Zone zone, string targetName, string stashedItemOut) :
            base(agent)
        {
            Name = "Stash " + targetName;
            PickType = type;
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
            if(Target == null)
            {
                yield return Status.Fail;
            }

            switch (PickType)
            {
                case (PickUpType.Room):
                case (PickUpType.Stockpile):
                    {
                        if (Zone == null)
                        {
                            yield return Status.Fail;
                            break;
                        }
                      
                        bool removed = Zone.Resources.RemoveResource(new ResourceAmount(Target.Tags[0]));

                        if (removed)
                        {
                            if(Creature.Inventory.Pickup(Target, Inventory.RestockType.RestockResource))
                            {
                                Agent.Blackboard.SetData(StashedItemOut, new ResourceAmount(Target));
                                Agent.Creature.NoiseMaker.MakeNoise("Stash", Agent.Position);
                                yield return Status.Success;
                            }
                            else
                            {
                                yield return Status.Fail;
                            }
                        }
                        else
                        {
                            yield return Status.Fail;
                        }
                        break;
                    }
                case (PickUpType.None):
                    {
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

                        ResourceAmount resource = new ResourceAmount(Target);
                        Agent.Blackboard.SetData(StashedItemOut, resource);
                        Agent.Creature.NoiseMaker.MakeNoise("Stash", Agent.Position);
                        yield return Status.Success;
                        break;
                    }
            }
        }
        
    }
    
}

