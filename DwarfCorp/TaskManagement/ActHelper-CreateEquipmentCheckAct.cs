using System.Linq;
using System;

namespace DwarfCorp
{
    public static partial class ActHelper
    {
        public enum EquipmentFallback
        {
            NoFallback,
            AllowDefault
        }

        public static Act CreateEquipmentCheckAct(CreatureAI Agent, String Slot, EquipmentFallback Fallback, params String[] RequiredTags)
        {
            if (!Agent.Stats.CurrentClass.RequiresTools)
                return new Condition(() => true);

            if (Agent.Creature.Equipment.HasValue(out var equipment))
                return new Sequence(
                    new Select(
                        new Condition(() =>
                        {
                            // We aren't holding anything! Great.
                            if (!equipment.GetItemInSlot(Slot).HasValue())
                                return true;

                            if (equipment.GetItemInSlot(Slot).HasValue(out var item) && item.ResourceType.HasValue(out var res) && res.Tags.Any(t => RequiredTags.Contains(t)))
                                return true;

                            Agent.Blackboard.SetData("item-to-remove", item);
                            return false; // We need to remove this tool!
                        }),
                        new UnequipAct(Agent) { BlackboardEntry = "item-to-remove" }),
                    new Select(
                        new Condition(() =>
                        {
                            var equipped = equipment.GetItemInSlot(Slot);

                            // If the equipped tool already matches. We shouldn't get here as the tool should already have been removed.
                            if (equipped.HasValue(out var item) && item.ResourceType.HasValue(out var res))
                                if (res.Tags.Any(t => RequiredTags.Contains(t)))
                                    return true;

                            // If the default is allowed, we do not already have a tool, and no tool is available - use default.
                            // It is assumed that the enclosing act will see that the equiped item is null and properly use the default.
                            if (Fallback == EquipmentFallback.AllowDefault && !equipment.GetItemInSlot(Slot).HasValue())
                                if (!Agent.World.GetFirstStockpileContainingResourceWithMatchingTag(RequiredTags.ToList()).HasValue)
                                    return true;

                            return false;
                        }),
                        new Sequence(
                            new FailMessage(Agent, new GetAnySingleMatchingResourceAct(Agent, RequiredTags.ToList()) { BlackboardEntry = "tool-stashed" }, "Could not locate tool."),
                            new EquipAct(Agent) { BlackboardEntry = "tool-stashed" }
                        )));
            else
            {
                Debugger.RaiseDebugWarning("Encountered entity that requires tools but does not have an equipment component.");
                return new Condition(() => false);
            }
        }
    }
}