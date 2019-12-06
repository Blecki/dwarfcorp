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
            else if (!String.IsNullOrEmpty(Creature.Stats.CurrentClass.FallbackTool))
                return new Resource(Creature.Stats.CurrentClass.FallbackTool);
            else
                return new Resource("Dwarf Hands");
        }
    }
}