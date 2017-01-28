// Item.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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

        public static IEnumerable<Act.Status> UnReserve(Item item)
        {
            item.ReservedFor = null;
            yield return Act.Status.Success;
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