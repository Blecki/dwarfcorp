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

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class CraftLibrary
    {
        public enum CraftItemType
        {
            BearTrap,
            Lamp
        };

        public static Dictionary<CraftItemType, CraftItem> CraftItems { get; set; }
        private static bool staticsInitialized = false;


        public CraftLibrary()
        {
            Initialize();
        }

        public static CraftItemType GetType(string name)
        {
            return (from item in CraftItems where item.Value.Name == name select item.Key).FirstOrDefault();
        }

        public static void Initialize()
        {
            if (staticsInitialized)
            {
                return;
            }

            CraftItems = new Dictionary<CraftItemType, CraftItem>()
            {
                {
                    CraftItemType.BearTrap,
                    new CraftItem()
                    {
                        CraftType = CraftItemType.BearTrap,
                        Name = "Bear Trap",
                        Description = "Triggers on enemies, doing massive damage before being destroyed",
                        RequiredResources = new List<ResourceAmount>()
                        {
                            new ResourceAmount(ResourceLibrary.ResourceType.Iron, 4)
                        },
                        Image = new ImageFrame(TextureManager.GetTexture(ContentPaths.Entities.DwarfObjects.beartrap), 32, 0, 0),
                        BaseCraftTime = 20
                    }
                },
                {
                    CraftItemType.Lamp,
                    new CraftItem()
                    {
                        CraftType = CraftItemType.Lamp,
                        Name = "Lamp",
                        Description = "Dwarves need to see sometimes too!",
                        RequiredResources = new List<ResourceAmount>()
                        {
                            new ResourceAmount(ResourceLibrary.ResourceType.Coal, 1)
                        },
                        Image = new ImageFrame(TextureManager.GetTexture(ContentPaths.Entities.Furniture.interior_furniture), 32, 0, 1),
                        BaseCraftTime = 10
                    }
                }
            };

            staticsInitialized = true;
        }
    }
}
