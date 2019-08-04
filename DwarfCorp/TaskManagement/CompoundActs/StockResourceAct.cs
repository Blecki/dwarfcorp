using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature takes an item to an open stockpile and leaves it there.
    /// </summary>
    public class StockResourceAct : CompoundCreatureAct
    {
        public ResourceAmount ItemToStock { get; set; }
        public string ItemID { get; set; }

        public StockResourceAct()
        {

        }

        public StockResourceAct(CreatureAI agent, string item) :
            base(agent)
        {
            ItemID = item;
            Tree = null;
            ItemToStock = null;
            Name = "Stock Item";
        }

        public StockResourceAct(CreatureAI agent, ResourceAmount item) :
            base(agent)
        {
            ItemToStock = item.CloneResource();
            Name = "Stock Item";
            Tree = null;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            if (Tree == null)
            {
                if (ItemToStock == null)
                {
                    ItemToStock = Agent.Blackboard.GetData<ResourceAmount>(ItemID);
                }


                if (ItemToStock != null)
                {
                    Tree = new Sequence(
                        new SetBlackboardData<ResourceAmount>(Agent, "GatheredResource", ItemToStock.CloneResource()),
                        new SearchFreeStockpileAct(Agent, "TargetStockpile", "FreeVoxel", ItemToStock),
                        new GoToNamedVoxelAct("FreeVoxel", PlanAct.PlanType.Adjacent, Agent),
                        new PutResourceInZone(Agent, "TargetStockpile", "FreeVoxel", "GatheredResource"));

                    Tree.Initialize();
                }
            }

            return base.Run();
        }
    }

}