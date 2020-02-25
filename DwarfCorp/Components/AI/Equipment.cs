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

        public void EquipItem(Resource Item)
        {
            if (!Item.Equipable) return;
            if (String.IsNullOrEmpty(Item.Equipment_Slot)) return;

            UnequipItem(Item.Equipment_Slot);

            EquippedItems[Item.Equipment_Slot] = Item;

            if (!String.IsNullOrEmpty(Item.Equipment_LayerName) && GetRoot().GetComponent<DwarfSprites.LayeredCharacterSprite>().HasValue(out var sprite))
                if (DwarfSprites.LayerLibrary.FindLayerWithName(Item.Equipment_LayerType, Item.Equipment_LayerName).HasValue(out var layer))
                {
                    if (DwarfSprites.LayerLibrary.FindPalette(Item.Equipment_Palette).HasValue(out var palette))
                        sprite.AddLayer(layer, palette);
                    else
                        sprite.AddLayer(layer, DwarfSprites.LayerLibrary.BasePalette);
                }
        }

        public void UnequipItem(String Slot)
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

        public void AddLayersToSprite(DwarfSprites.LayeredCharacterSprite Sprite)
        {
            foreach (var item in EquippedItems.Values)
                if (!String.IsNullOrEmpty(item.Equipment_LayerName))
                    if (DwarfSprites.LayerLibrary.FindLayerWithName(item.Equipment_LayerType, item.Equipment_LayerName).HasValue(out var layer))
                    {
                        if (DwarfSprites.LayerLibrary.FindPalette(item.Equipment_Palette).HasValue(out var palette))
                            Sprite.AddLayer(layer, palette);
                        else
                            Sprite.AddLayer(layer, DwarfSprites.LayerLibrary.BasePalette);
                    }
        }

        public override void Die()
        {
            if (Active)
                DropAll();

            base.Die();
        }

        public void DropAll()
        {
            var parentBody = GetRoot();
            var myBox = GetBoundingBox();
            var box = parentBody == null ? GetBoundingBox() : new BoundingBox(myBox.Min - myBox.Center() + parentBody.Position, myBox.Max - myBox.Center() + parentBody.Position);
            var inventory = GetComponent<Inventory>();
            var flammable = GetComponent<Flammable>();

            foreach (var item in EquippedItems)
            {
                var resource = new ResourceEntity(Manager, item.Value, MathFunctions.RandVector3Box(box));
                if (inventory.HasValue(out var inv) && inv.Attacker != null && !inv.Attacker.IsDead)
                    inv.Attacker.Creature.Gather(resource, TaskPriority.Eventually);
                if (flammable.HasValue(out var flames) && flames.Heat >= flames.Flashpoint)
                       if (resource.GetRoot().GetComponent<Flammable>().HasValue(out var itemFlames))
                            itemFlames.Heat = flames.Heat;
            }            
        }
    }
}
