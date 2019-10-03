using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class TransferResourcesAct : CompoundCreatureAct
    {
        public Stockpile StockpileFrom = null;
        public Resource Resource = null;


        public TransferResourcesAct()
        {

        }

        public TransferResourcesAct(CreatureAI agent, Stockpile from, Resource Resource) :
            base(agent)
        {
            StockpileFrom = from;
            this.Resource = Resource;
        }

        public override void Initialize()
        {
            Tree = new Sequence(new GoToZoneAct(Agent, StockpileFrom),
                                new StashResourcesAct(Agent, StockpileFrom, Resource) { RestockType = Inventory.RestockType.RestockResource },
                                new StockResourceAct(Agent, Resource));
            base.Initialize();                     
        }
    }
}