using System.Linq;
using System;

namespace DwarfCorp
{
    public static partial class ActHelper
    {
        public static void ApplyWearToTool(CreatureAI Creature, float Wear)
        {
            if (Creature.Stats.Equipment.GetItemInSlot("tool").HasValue(out var tool))
                tool.Wear += Wear;
        }
    }
}