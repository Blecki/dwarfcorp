using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature takes an item to an open stockpile and leaves it there.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GatherItemAct : CompoundCreatureAct
    {
        public Body ItemToGather { get; set; }
        public string ItemID { get; set; }

        public GatherItemAct()
        {
            
        }

        public IEnumerable<Status> AddItemToGatherManager()
        {
            Agent.GatherManager.ItemsToGather.Add(ItemToGather);
            yield return Status.Success;
        }

        public IEnumerable<Status> Finally()
        {
            yield return Status.Fail;
        }

        public IEnumerable<Status> RemoveItemFromGatherManager()
        {
            if (Creature.Inventory.Resources.IsFull() && !ItemToGather.IsDead)
            {
                yield return Status.Fail;
            }

            if(Agent.GatherManager.ItemsToGather.Contains(ItemToGather))
            {
                Agent.GatherManager.ItemsToGather.Remove(ItemToGather);
            }
            yield return Status.Success;
        }

        public IEnumerable<Status> AddStockOrder()
        {
            Agent.GatherManager.StockOrders.Add(new GatherManager.StockOrder()
            {
                Destination = null,
                Resource = new ResourceAmount(ItemToGather)
            });

            yield return Status.Success;
        }


        public bool IsGatherable()
        {
            return (Agent.Faction.GatherDesignations.Contains(ItemToGather) && !Creature.Inventory.Resources.IsFull());
        }

        public Act EntityIsGatherable()
        {
            return new Condition(IsGatherable);
        }

        public GatherItemAct(CreatureAIComponent agent, string item) :
            base(agent)
        {
            ItemID = item;
            Tree = null;
            ItemToGather = null;
            Name = "Gather Item";
        }

        public GatherItemAct(CreatureAIComponent agent, Body item) :
            base(agent)
        {
            ItemToGather = item;
            Name = "Gather Item";
            Tree = null;
        }

        public override void Initialize()
        {
            base.Initialize();
        }



        public override IEnumerable<Status> Run()
        {
            if(Tree == null)
            {
                if(ItemToGather == null)
                {
                    ItemToGather = Agent.Blackboard.GetData<Body>(ItemID);
                }


                if(ItemToGather != null)
                {
                    Tree = new Sequence(
                        new SetBlackboardData<Body>(Agent, "GatherItem", ItemToGather),
                        EntityIsGatherable(),
                        new Wrap(AddItemToGatherManager),
                        new GoToEntityAct(ItemToGather, Agent),
                        EntityIsGatherable(),
                        new StashAct(Agent, StashAct.PickUpType.None, null, "GatherItem", "GatheredResource"),
                        new Wrap(RemoveItemFromGatherManager),
                        new Wrap(AddStockOrder)
                        ) | (new Wrap(RemoveItemFromGatherManager) & new Wrap(Finally) & false);

                    Tree.Initialize();
                }
            }

            if(Tree == null)
            {
                yield return Status.Fail;
            }
            else
            {
                foreach(Status s in base.Run())
                {
                    yield return s;
                }
            }
        }
    }

}