using System.Linq;
using System;

namespace DwarfCorp
{
    public static partial class ActHelper
    {
        public static Act CreateToolCheckAct(CreatureAI Creature, bool AllowHands, params String[] ToolType)
        {
            return new Sequence(
                new Not(new Domain(() =>
                    {
                        if (Creature.Stats.Equipment.GetItemInSlot("tool").HasValue(out var tool) && tool.ResourceType.HasValue(out var res))
                            return !res.Tags.Any(t => ToolType.Contains(t));
                        return true;
                    },
                    new UnequipToolAct())),
                new Select(
                    new Condition(() =>
                    {
                        if (!Creature.Stats.CurrentClass.RequiresTools) return true;

                        // If hands are allowed, we do not already have a tool, and no tool is available - use hands.
                        if (AllowHands && !Creature.Stats.Equipment.GetItemInSlot("tool").HasValue())
                            if (!Creature.World.GetFirstStockpileContainingResourceWithMatchingTag(ToolType.ToList()).HasValue)
                                return true;

                        if (Creature.Stats.Equipment.GetItemInSlot("tool").HasValue(out var tool) && tool.ResourceType.HasValue(out var res))
                            return res.Tags.Any(t => ToolType.Contains(t));

                        return false;
                    }),
                    new Sequence(
                        new GetAnySingleMatchingResourceAct(Creature, ToolType.ToList()) { BlackboardEntry = "tool-stashed" },
                        new EquipToolAct(Creature) { BlackboardEntry = "tool-stashed" }
                    )));
        }
    }
}