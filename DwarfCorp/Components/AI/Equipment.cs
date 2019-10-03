using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class EquippedItem
    {
        public Resource Resource;
    }

    public class Equipment
    {
        public Dictionary<String, EquippedItem> EquippedItems = new Dictionary<String, EquippedItem>();

        public MaybeNull<EquippedItem> GetItemInSlot(String Slot)
        {
            if (EquippedItems.ContainsKey(Slot))
                return EquippedItems[Slot];
            return null;
        }

        public void EquipItem(String Slot, EquippedItem Item)
        {
            EquippedItems[Slot] = Item;
        }

        public void UnequipItem(String Slot)
        {
            EquippedItems.Remove(Slot);
        }

    }
}
