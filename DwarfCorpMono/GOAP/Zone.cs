using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class Zone
    {
        public string ID = "";
        public List<Item> Items = new List<Item>();
        public LocatableComponent userData = null;

        public Zone(string id)
        {
            ID = id;
        }

        public void AddItem(Item i)
        {
            if (!Items.Contains(i))
            {
                Items.Add(i);
            }
        }

        public void RemoveItem(Item i)
        {
            if (Items.Contains(i))
            {
                Items.Remove(i);
            }
        }

        public void RemoveFirstItem(string name)
        {
            Item i = GetItemWithName(name);
            if (i != null)
            {
                RemoveItem(i);
            }
        }

        public Item GetItemWithName(string name)
        {
            foreach (Item i in Items)
            {
                if (i.ID.Equals(name))
                {
                    return i;
                }
            }

            return null;
        }
    }

}
