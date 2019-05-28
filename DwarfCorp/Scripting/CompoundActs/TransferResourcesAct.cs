using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class TransferResourcesAct : CompoundCreatureAct
    {
        public Stockpile StockpileFrom = null;
        public ResourceAmount Resources = null;


        public TransferResourcesAct()
        {

        }

        public TransferResourcesAct(CreatureAI agent, Stockpile from, ResourceAmount resources) :
            base(agent)
        {
            StockpileFrom = from;
            Resources = resources;
        }

        public override void Initialize()
        {
            Tree = new Sequence(new GoToZoneAct(Agent, StockpileFrom),
                                new StashResourcesAct(Agent, StockpileFrom, Resources) { RestockType = Inventory.RestockType.RestockResource },
                                new StockResourceAct(Agent, Resources));
            base.Initialize();                     
        }
    }
}