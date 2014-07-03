using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// An item keeps track of an entity in the context of it existing in a zone.
    /// </summary>
    public class Item : IEquatable<Item>
    {
        public string ID;
        public Zone Zone;
        public Body UserData;
        public bool CanGrab = true;
        public CreatureAI ReservedFor = null;

        public bool IsInZone
        {
            get { return Zone != null; }
        }

        public bool IsInStockpile
        {
            get { return Zone != null && Zone is Stockpile; }
        }

        public bool HasUserData
        {
            get { return UserData != null; }
        }

        public static Dictionary<string, Item> ItemDictionary = new Dictionary<string, Item>();

        public static Item CreateItem(Zone z, Body body)
        {
            return CreateItem(body.Name + body.GlobalID, z, body);
        }

        public static Item CreateItem(string name, Zone z, Body userData)
        {
            if(ItemDictionary.ContainsKey(name))
            {
                ItemDictionary[name].Zone = z;
                ItemDictionary[name].UserData = userData;
                return ItemDictionary[name];
            }
            else
            {
                ItemDictionary[name] = new Item(name, z, userData);
                return ItemDictionary[name];
            }
        }

        public Item(string id, Zone zone, Body userData)
        {
            this.UserData = userData;
            ID = id;
            Zone = zone;
        }

        public static Item FindItem(Body component)
        {
            string name = component.Name + " " + component.GlobalID;

            if(ItemDictionary.ContainsKey(name))
            {
                return ItemDictionary[name];
            }
            else
            {
                return CreateItem(name, null, component);
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
            var a = obj as Item;
            if(a != null)
            {
                return Equals(a);
            }
            else
            {
                return false;
            }
        }
    }

}