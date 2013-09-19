using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Item : IEquatable<Item>
    {
        public string ID;
        public Zone Zone;
        public LocatableComponent userData;
        public bool canGrab = true;
        public CreatureAIComponent reservedFor = null;
        public static Dictionary<string, Item> ItemDictionary = new Dictionary<string, Item>();

        public static Item CreateItem(string name, Zone z, LocatableComponent userData)
        {
            if (ItemDictionary.ContainsKey(name))
            {
                ItemDictionary[name].Zone = z;
                ItemDictionary[name].userData = userData;
                return ItemDictionary[name];
            }
            else
            {
                ItemDictionary[name] = new Item(name, z, userData);
                return ItemDictionary[name];
            }
        }

        public Item(string id, Zone zone, LocatableComponent userData)
        {
            this.userData = userData;
            ID = id;
            Zone = zone;
        }

        public static Item FindItem(LocatableComponent component)
        {
            string name = component.Name + " " + component.GlobalID;

            if (ItemDictionary.ContainsKey(name))
            {
                return ItemDictionary[name];
            }
            else
            {
                return null;
            }
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public bool Equals(Item other)
        {
            return ID.Equals(other.ID);
        }

        public override bool Equals(object obj)
        {
            if (obj is Item)
            {
                return Equals((Item)obj);
            }
            else
            {
                return false;
            }

        }
    }
}
