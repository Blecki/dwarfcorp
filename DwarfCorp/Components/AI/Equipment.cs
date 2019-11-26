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
    public class Equipment : GameComponent
    {
        public Equipment() { }
        public Equipment(ComponentManager Manager) : base(Manager) { }

        public Dictionary<String, Resource> EquippedItems = new Dictionary<String, Resource>();

        public MaybeNull<Resource> GetItemInSlot(String Slot)
        {
            if (EquippedItems.ContainsKey(Slot))
                return EquippedItems[Slot];
            return null;
        }

        public void EquipItem(String Slot, Resource Item)
        {
            EquippedItems[Slot] = Item;

            if (!String.IsNullOrEmpty(Item.Equipment_LayerName) && GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>().HasValue(out var sprite))
            {
                sprite.RemoveLayer(Item.Equipment_LayerType);
                sprite.AddLayer(LayeredSprites.LayerLibrary.EnumerateLayers(Item.Equipment_LayerType).Where(l => l.Names.Contains(Item.Equipment_LayerName)).FirstOrDefault(), LayeredSprites.LayerLibrary.BaseDwarfPalette);
            }
        }

        public void UnequipItem(String Slot)
        {
            if (GetItemInSlot(Slot).HasValue(out var existing) && !String.IsNullOrEmpty(existing.Equipment_LayerName) && GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>().HasValue(out var sprite))
                sprite.RemoveLayer(existing.Equipment_LayerType);

            EquippedItems.Remove(Slot);
        }
    }
}
