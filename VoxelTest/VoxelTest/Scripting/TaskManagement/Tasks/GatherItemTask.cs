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
        public uint EntityToGather = 0;
        public string ZoneType = "Stockpile";

        public GatherItemTask()
        {

        }

        public GatherItemTask(LocatableComponent entity)
        {
            EntityToGather = entity.GlobalID;
            Name = "Gather Entity: " + entity.Name + " " + entity.GlobalID;
        }

        public override Act CreateScript(Creature creature)
        {
            return new GatherItemAct(creature.AI, PlayState.ComponentManager.Components[EntityToGather] as LocatableComponent);
        }

        public override bool IsFeasible(Creature agent)
        {
            return agent.Faction.Stockpiles.Any(stockpile => !stockpile.IsFull());
        }

        public override float ComputeCost(Creature agent)
        {
            LocatableComponent component = PlayState.ComponentManager.Components[EntityToGather] as LocatableComponent;
            return component == null ? 1000 : (agent.AI.Position - component.GlobalTransform.Translation).LengthSquared();
        }
    }

}