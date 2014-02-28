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

        public CreatureAIComponent Creature { get; set; }
        public List<LocatableComponent> ItemsToGather { get; set; }
        public List<StockOrder> StockOrders { get; set; }

        public GatherManager(CreatureAIComponent creature)
        {
            Creature = creature;
            ItemsToGather = new List<LocatableComponent>();
            StockOrders = new List<StockOrder>();
        }


    }
}
