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
        public Resource ItemToStock;
        public string ItemID { get; set; }

        public StockResourceAct()
        {

        }

        public StockResourceAct(CreatureAI agent, Resource item) :
            base(agent)
        {
            ItemToStock = item;
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
                    throw new InvalidOperationException();

                Tree = new Sequence(
                        new SetBlackboardData<Resource>(Agent, "GatheredResource", ItemToStock),
                        new SearchFreeStockpileAct(Agent, "TargetStockpile", "FreeVoxel", ItemToStock),
                        new GoToNamedVoxelAct("FreeVoxel", PlanAct.PlanType.Adjacent, Agent),
                        new PutResourceInZone(Agent, "TargetStockpile", "FreeVoxel", "GatheredResource"));

                    Tree.Initialize();
            }

            return base.Run();
        }
    }

}