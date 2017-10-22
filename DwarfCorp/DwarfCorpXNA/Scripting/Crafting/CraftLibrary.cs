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
        public static Dictionary<string, CraftItem> CraftItems { get; set; }
        private static bool staticsInitialized = false;


        public CraftLibrary()
        {
            Initialize();
        }


        public static void Initialize()
        {
            if (staticsInitialized)
            {
                return;
            }

            CraftItems = new Dictionary<string, CraftItem>()
            {
                {
                   "Bear Trap",
                    new CraftItem()
                    {
                        Name = "Bear Trap",
                        Description = "Damages enemies and then explodes.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Metal, 4)
                        },
                        Icon = new Gui.TileReference("beartrap", 0),
                        BaseCraftTime = 60,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Lamp",
                    new CraftItem()
                    {
                        Name = "Lamp",
                        Description = "Dwarves need to see sometimes too!",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Fuel, 1)
                        },
                        Icon = new Gui.TileReference("furniture", 9),
                        BaseCraftTime = 10,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Wooden Ladder",
                    new CraftItem()
                    {
                        Name = "Wooden Ladder",
                        Description = "Allows dwarves to climb up and down",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood, 1)
                        },
                        Icon = new Gui.TileReference("furniture", 2),
                        BaseCraftTime = 10,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.NearWall},
                        CraftLocation = ""
                    }
                },
                {
                    "Stone Ladder",
                    new CraftItem()
                    {
                        Name = "Stone Ladder",
                        Description = "Allows dwarves to climb up and down",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Stone, 1)
                        },
                        Icon = new Gui.TileReference("furniture", 66),
                        BaseCraftTime = 20,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.NearWall},
                        CraftLocation = ""
                    }
                },
                {
                    "Metal Ladder",
                    new CraftItem()
                    {
                        Name = "Metal Ladder",
                        Description = "Allows dwarves to climb up and down",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Metal, 1)
                        },
                        Icon = new Gui.TileReference("furniture", 67),
                        BaseCraftTime = 30,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.NearWall},
                        CraftLocation = ""
                    }
                },
                {
                    "Wooden Door",
                    new CraftItem()
                    {
                        Name = "Wooden Door",
                        Description = "Keep monsters out, and dwarves in.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood, 1)
                        },
                        Icon = new Gui.TileReference("furniture", 11),
                        BaseCraftTime = 30,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.NearWall},
                        CraftLocation = ""
                    }
                },
                {
                    "Stone Door",
                    new CraftItem()
                    {
                        Name = "Stone Door",
                        Description = "Keep monsters out, and dwarves in.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Stone, 1)
                        },
                        Icon = new Gui.TileReference("furniture", 64),
                        BaseCraftTime = 30,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.NearWall},
                        CraftLocation = ""
                    }
                },
                {
                    "Metal Door",
                    new CraftItem()
                    {
                        Name = "Metal Door",
                        Description = "Keep monsters out, and dwarves in.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Metal, 1)
                        },
                        Icon = new Gui.TileReference("furniture", 65),
                        BaseCraftTime = 40,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.NearWall},
                        CraftLocation = ""
                    }
                },
                {
                    "Trinket",
                    new CraftItem()
                    {
                        Name = "Trinket",
                        Description = "Get creative juices flowing and make a work of art.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Material, 3)
                        },
                        Icon = new Gui.TileReference("crafts", 0),
                        BaseCraftTime = 45,
                        Type = CraftItem.CraftType.Resource,
                        ResourceCreated = "Trinket",
                        Verb = "Craft",
                        CurrentVerb = "Crafting",
                        PastTeseVerb = "Crafted"
                    }
                },
                {
                    "Gem-set Trinket",
                    new CraftItem()
                    {
                        Name = "Gem-set Trinket",
                        Description = "Encrust a work of art with gems.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Encrustable, 1),
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Gem, 1)
                        },
                        Icon = new Gui.TileReference("crafts", 1),
                        BaseCraftTime = 55,
                        Type = CraftItem.CraftType.Resource,
                        ResourceCreated = "Gem-set Trinket",
                        Verb = "Encrust",
                        CurrentVerb = "Encrusting",
                        PastTeseVerb = "Encrusted"
                    }
                },
                {
                    "Meal",
                    new CraftItem()
                    {
                        Name = "Meal",
                        Description = "Take raw food and cook something",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.RawFood, 1),
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.RawFood, 1),
                        },
                        Icon = new Gui.TileReference("resources", 21),
                        BaseCraftTime = 45,
                        Type = CraftItem.CraftType.Resource,
                        ResourceCreated = "Meal",
                        CraftLocation = "Cutting Board",
                        Verb = "Cook",
                        PastTeseVerb = "Cooked",
                        CurrentVerb = "Cooking",
                        AllowHeterogenous = true
                    }
                },
                {
                    "Bread",
                    new CraftItem()
                    {
                        Name = "Bread",
                        Description = "Turn bakeable food into bread.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Bakeable, 1)
                        },
                        Icon = new Gui.TileReference("resources", 22),
                        BaseCraftTime = 45,
                        Type = CraftItem.CraftType.Resource,
                        ResourceCreated = "Bread",
                        CraftLocation = "Stove",
                        Verb = "Bake",
                        PastTeseVerb = "Baked",
                        CurrentVerb = "Baking"
                    }
                },
                {
                    "Ale",
                    new CraftItem()
                    {
                        Name = "Ale",
                        Description = "Turn brewable food into alcohol",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Brewable, 1)
                        },
                        Icon = new Gui.TileReference("resources", 20),
                        BaseCraftTime = 45,
                        Type = CraftItem.CraftType.Resource,
                        ResourceCreated = "Ale",
                        CraftLocation = "Barrel",
                        Verb = "Brew",
                        PastTeseVerb = "Brewed",
                        CurrentVerb = "Brewing"
                    }
                },
                {
                    "Turret",
                    new CraftItem()
                    {
                        Name = "Turret",
                        Description = "Crossbow automatically targets enemies with magical power.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Metal, 2),
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 1),
                        },
                        Icon = new Gui.TileReference("furniture", 57),
                        BaseCraftTime = 90,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Chair",
                    new CraftItem()
                    {
                        Name = "Chair",
                        Description = "Dwarves sit here and relax. Place next to tables for eating.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood, 1),
                        },
                        Icon = new Gui.TileReference("furniture", 50),
                        BaseCraftTime = 9,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Table",
                    new CraftItem()
                    {
                        Name = "Table",
                        Description = "Dwarves gather around tables to eat. Build chairs next to the table.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood, 4),
                        },
                        Icon = new Gui.TileReference("furniture", 48),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Anvil",
                    new CraftItem()
                    {
                        Name = "Anvil",
                        Description = "Used to craft more complex items.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Metal, 4),
                        },
                        Icon = new Gui.TileReference("furniture", 24),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Forge",
                    new CraftItem()
                    {
                        Name = "Forge",
                        Description = "Dwarves use the forge to refine materials.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Fuel, 4),
                        },
                        Icon = new Gui.TileReference("furniture", 25),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround}
                    }
                },
                {
                    "Stove",
                    new CraftItem()
                    {
                        Name = "Stove",
                        Description = "Dwarves use the stove to cook meals.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Fuel, 2),
                        },
                        Icon = new Gui.TileReference("furniture", 35),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Kitchen Table",
                    new CraftItem()
                    {
                        Name = "Kitchen Table",
                        Description = "Dwarves use the cutting board to cook meals.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood, 2),
                             new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Metal, 2),
                        },
                        Icon = new Gui.TileReference("furniture", 56),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Barrel",
                    new CraftItem()
                    {
                        Name = "Barrel",
                        Description = "Dwarves use the still to brew ale.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood, 2),
                             new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Brewable, 4),
                        },
                        Icon = new Gui.TileReference("furniture", 1),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Bed",
                    new CraftItem()
                    {
                        Name = "Bed",
                        Description = "Dwarves use the bed to sleep.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood, 4),
                        },
                        Icon = new Gui.TileReference("furniture", 3),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Strawman",
                    new CraftItem()
                    {
                        Name = "Strawman",
                        Description = "Dwarves can train by hitting the strawman.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Grain, 4),
                        },
                        Icon = new Gui.TileReference("furniture", 41),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Potions",
                    new CraftItem()
                    {
                        Name = "Potions",
                        Description = "Dwarves can do magical research here.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 2),
                        },
                        Icon = new Gui.TileReference("furniture", 33),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Books",
                    new CraftItem()
                    {
                        Name = "Books",
                        Description = "Dwarves can do magical research here.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 2),
                        },
                        Icon = new Gui.TileReference("furniture", 32),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
                {
                    "Bookshelf",
                    new CraftItem()
                    {
                        Name = "Bookshelf",
                        Description = "Dwarves can do magical research here.",
                        RequiredResources = new List<Quantitiy<Resource.ResourceTags>>()
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 4),
                        },
                        Icon = new Gui.TileReference("furniture", 32),
                        BaseCraftTime = 150,
                        Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround},
                        CraftLocation = ""
                    }
                },
            };

            foreach (var res in ResourceLibrary.Resources.Where(res => res.Value.CanCraft))
            {
                CraftItems[res.Key] = ResourceToCraftItem(res.Key);
            }

            staticsInitialized = true;
        }

        public static CraftItem ResourceToCraftItem(ResourceLibrary.ResourceType resource)
        {
            Resource res = ResourceLibrary.GetResourceByName(resource);
            return new CraftItem()
            {
                Name = resource,
                BaseCraftTime = 30,
                CraftLocation = "Forge",
                Description = res.Description,
                Icon = res.GuiLayers[0],
                RequiredResources = res.CraftPrereqs,
                ResourceCreated = resource,
                Type = CraftItem.CraftType.Resource
            };
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
                return item.Value.Clone();
            }
            return null;
        }
    }
}
