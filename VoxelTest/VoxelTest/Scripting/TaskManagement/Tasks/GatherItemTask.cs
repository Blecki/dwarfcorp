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
    internal class GatherItemTask : Task
    {
        public LocatableComponent EntityToGather = null;
        public string ZoneType = "Stockpile";

        public GatherItemTask()
        {

        }

        public GatherItemTask(LocatableComponent entity)
        {
            EntityToGather = entity;
            Name = "Gather Entity: " + entity.Name + " " + entity.GlobalID;
        }

        public override Act CreateScript(Creature creature)
        {
            return new GatherItemAct(creature.AI, EntityToGather);
        }

        public override bool IsFeasible(Creature agent)
        {
            return EntityToGather != null && !EntityToGather.IsDead && !agent.AI.GatherManager.ItemsToGather.Contains(EntityToGather);
        }

        public override float ComputeCost(Creature agent)
        {
            return EntityToGather == null  || EntityToGather.IsDead ? 1000 : (agent.AI.Position - EntityToGather.GlobalTransform.Translation).LengthSquared();
        }
    }

}