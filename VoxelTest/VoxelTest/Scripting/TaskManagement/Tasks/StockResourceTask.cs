using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should pick up an item and put it in a stockpile.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class StockResourceTask : Task
    {
        public ResourceAmount EntityToGather = null;
        public string ZoneType = "Stockpile";

        public StockResourceTask()
        {

        }

        public StockResourceTask(ResourceAmount entity)
        {
            EntityToGather = entity;
            Name = "Stock Entity: " + entity.ResourceType.ResourceName + " " + entity.NumResources;
        }

        public override Act CreateScript(Creature creature)
        {
            return new StockResourceAct(creature.AI, EntityToGather);
        }

        public override bool IsFeasible(Creature agent)
        {
            return agent.Faction.HasFreeStockpile();
        }

        public override float ComputeCost(Creature agent)
        {
            return 1.0f;
        }


    }

}