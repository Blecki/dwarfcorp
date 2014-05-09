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
    public class StockResourceAct : CompoundCreatureAct
    {
        public ResourceAmount ItemToStock { get; set; }
        public string ItemID { get; set; }

        public StockResourceAct()
        {

        }

        public StockResourceAct(CreatureAIComponent agent, string item) :
            base(agent)
        {
            ItemID = item;
            Tree = null;
            ItemToStock = null;
            Name = "Stock Item";
        }

        public StockResourceAct(CreatureAIComponent agent, ResourceAmount item) :
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
                {
                    ItemToStock = Agent.Blackboard.GetData<ResourceAmount>(ItemID);
                }


                if (ItemToStock != null)
                {
                    Tree = new Sequence(
                        new SetBlackboardData<ResourceAmount>(Agent, "GatheredResource", ItemToStock),
                        new SearchFreeStockpileAct(Agent, "TargetStockpile", "FreeVoxel"),
                        
                                        new Select(
                                                    new Sequence(
                                                                    new GoToVoxelAct("FreeVoxel", PlanAct.PlanType.Adjacent, Agent),
                                                                    new PutResourceInZone(Agent, "TargetStockpile", "FreeVoxel", "GatheredResource")
                                                                ),
                                                    new DropItemAct(Agent)
                                                  )
                                         
                        ) 
                     ;

                    Tree.Initialize();
                }
            }

            if (Tree == null)
            {
                yield return Status.Fail;
            }
            else
            {
                foreach (Status s in base.Run())
                {
                    yield return s;
                }
            }
        }
    }

}