// CraftLibrary.cs
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
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class CraftLibrary
    {
        private static Dictionary<string, CraftItem> CraftItems = null;

        public static IEnumerable<CraftItem> EnumerateCraftables()
        {
            return CraftItems.Values;
        }

        public static CraftItem GetCraftable(string Name)
        {
            if (CraftItems.ContainsKey(Name))
                return CraftItems[Name];
            return null;
        }

        public static void Add(CraftItem craft)
        {
            CraftItems[craft.Name] = craft;
        }

        public static void InitializeDefaultLibrary()
        {
            if (CraftItems != null) return;

            var craftList = FileUtils.LoadJsonListFromMultipleSources<CraftItem>(ContentPaths.craft_items, null, c => c.Name);
            CraftItems = new Dictionary<string, CraftItem>();

            foreach (var type in craftList)
                CraftItems.Add(type.Name, type);
        }

        public static CraftItem GetRandomApplicableCraftItem(Faction faction)
        {
            const int maxIters = 100;
            for (int i = 0; i < maxIters; i++)
            {
                var item = Datastructures.SelectRandom(CraftItems.Where(k => k.Value.Type == CraftItem.CraftType.Resource));
                if (!faction.HasResources(item.Value.RequiredResources))
                {
                    continue;
                }
                if (!faction.OwnedObjects.Any(o => o.Tags.Contains(item.Value.CraftLocation)))
                {
                    continue;
                }
                return item.Value;
            }
            return null;
        }
    }
}
