using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class GatherManager
    {
        public struct StockOrder
        {
            public ResourceAmount Resource;
            public Zone Destination;
        }

        public CreatureAI Creature { get; set; }
        public List<Body> ItemsToGather { get; set; }
        public List<StockOrder> StockOrders { get; set; }

        public GatherManager(CreatureAI creature)
        {
            Creature = creature;
            ItemsToGather = new List<Body>();
            StockOrders = new List<StockOrder>();
        }


    }
}
