using System.Linq;
using System;

namespace DwarfCorp
{
    public static partial class ActHelper
    {
        public static Act CreateToolCheckAct(CreatureAI Creature, params String[] ToolType)
        {
            return new Select(
                new Condition(() =>
                {
                    if (!Creature.Stats.CurrentClass.RequiresTools) return true;
                    if (Creature.Stats.Equipment.GetItemInSlot("tool").HasValue(out var tool) && Library.GetResourceType(tool.Resource).HasValue(out var res))
                        return res.Tags.Any(t => ToolType.Contains(t));
                    return false;
                }),
                new Sequence(
                    new GetAnySingleMatchingResourceAct(Creature, ToolType.ToList()),
                    new EquipToolAct(Creature))
                );
        }
    }
}