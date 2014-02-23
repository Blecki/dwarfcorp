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
        public LocatableComponent ItemToGather { get; set; }
        public string ItemID { get; set; }

        public GatherItemAct()
        {
            
        }

        public IEnumerable<Status> AddItemToGatherManager()
        {
            Agent.GatherManager.ItemsToGather.Add(ItemToGather);
            yield return Status.Success;
        }

        public IEnumerable<Status> RemoveItemFromGatherManager()
        {
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
            return (!Agent.Faction.IsInStockpile(ItemToGather) && (Agent.Faction.GatherDesignations.Contains(ItemToGather)));
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

        public GatherItemAct(CreatureAIComponent agent, LocatableComponent item) :
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


        public IEnumerable<Status> Unreserve(string stockpile, string voxelID)
        {
            Stockpile pile = Agent.Blackboard.GetData<Stockpile>(stockpile);
            VoxelRef voxel = Agent.Blackboard.GetData<VoxelRef>(voxelID);

            if(pile == null || voxel == null)
            {
                yield return Status.Success;
            }
            else
            {
                pile.SetReserved(voxel, false);
                yield return Status.Success;
            }
            
        }

        public override IEnumerable<Status> Run()
        {
            if(Tree == null)
            {
                if(ItemToGather == null)
                {
                    ItemToGather = Agent.Blackboard.GetData<LocatableComponent>(ItemID);
                }


                if(ItemToGather != null)
                {
                    Tree = new Sequence(
                        new SetBlackboardData<LocatableComponent>(Agent, "GatherItem", ItemToGather),
                        //new SearchFreeStockpileAct(Agent, "TargetStockpile", "FreeVoxel"),
                        EntityIsGatherable(),
                        new Wrap(AddItemToGatherManager),
                        new GoToEntityAct(ItemToGather, Agent),
                        EntityIsGatherable(),
                        new StashAct(Agent, StashAct.PickUpType.None, null, "GatherItem", "GatheredResource"),
                        new Wrap(RemoveItemFromGatherManager),
                        new Wrap(AddStockOrder)
                        /*
                                        new Select(
                                                    new Sequence(
                                                                    new GoToVoxelAct("FreeVoxel", PlanAct.PlanType.Adjacent, Agent),
                                                                    new PutResourceInZone(Agent, "TargetStockpile", "FreeVoxel", "GatheredResource")
                                                                    //new PutItemInStockpileAct(Agent, "TargetStockpile", "FreeVoxel")
                                                                ),
                                                    new DropItemAct(Agent)
                                                  )
                                         */
                        ) | new Wrap(RemoveItemFromGatherManager);
                               //| new Wrap(() => Unreserve("TargetStockpile", "FreeVoxel"));

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