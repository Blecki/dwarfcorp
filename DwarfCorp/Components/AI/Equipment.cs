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
    public enum EquipmentSlot
    {
        None,
        Tool,
    }

    public class Equipment : GameComponent
    {
        public Equipment() { }
        public Equipment(ComponentManager Manager) : base(Manager) { }

        public Dictionary<EquipmentSlot, Resource> EquippedItems = new Dictionary<EquipmentSlot, Resource>();

        public MaybeNull<Resource> GetItemInSlot(EquipmentSlot Slot)
        {
            if (EquippedItems.ContainsKey(Slot))
                return EquippedItems[Slot];
            return null;
        }

        public void EquipItem(Resource Item)
        {
            if (Item.Equipment_Slot == EquipmentSlot.None) return;

            UnequipItem(Item.Equipment_Slot);

            EquippedItems[Item.Equipment_Slot] = Item;

            if (!String.IsNullOrEmpty(Item.Equipment_LayerName) 
                && GetRoot().GetComponent<DwarfSprites.LayeredCharacterSprite>().HasValue(out var sprite))
            {
                sprite.RemoveLayer(Item.Equipment_LayerType);
                sprite.AddLayer(DwarfSprites.LayerLibrary.EnumerateLayers(Item.Equipment_LayerType).Where(l => l.Names.Contains(Item.Equipment_LayerName)).FirstOrDefault(), DwarfSprites.LayerLibrary.BaseDwarfPalette);
            }
        }

        public void UnequipItem(EquipmentSlot Slot)
        {
            if (GetItemInSlot(Slot).HasValue(out var existing) 
                && !String.IsNullOrEmpty(existing.Equipment_LayerName) 
                && GetRoot().GetComponent< DwarfSprites.LayeredCharacterSprite>().HasValue(out var sprite))
                sprite.RemoveLayer(existing.Equipment_LayerType);

            EquippedItems.Remove(Slot);
        }

        public void UnequipItem(Resource Item)
        {
            if (GetItemInSlot(Item.Equipment_Slot).HasValue(out var res) && Object.ReferenceEquals(res, Item))
                UnequipItem(Item.Equipment_Slot);
        }
    }
}
