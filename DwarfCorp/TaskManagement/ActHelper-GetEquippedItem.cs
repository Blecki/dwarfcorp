using System.Linq;
using System;

namespace DwarfCorp
{
    public static partial class ActHelper
    {
        public static Resource GetEquippedItem(Creature Creature, String Slot)
        {
            if (Creature.Equipment.HasValue(out var equipment) && equipment.GetItemInSlot(Slot).HasValue(out var resource))
                return resource;
            else if (Creature.Stats.CurrentClass.HasValue(out var c) && !String.IsNullOrEmpty(c.FallbackTool))
                return new Resource(c.FallbackTool);
            else
                return new Resource("Dwarf Hands");
        }
    }
}