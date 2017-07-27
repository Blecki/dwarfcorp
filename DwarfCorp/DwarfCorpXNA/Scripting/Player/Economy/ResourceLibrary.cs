// ResourceLibrary.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// A static collection of resource types (should eventually be replaced with a file)
    /// </summary>
    [JsonObject(IsReference = true)]
    public static class ResourceLibrary
    {

        public struct ResourceType
        {
            [JsonProperty]
            private string _value;

            public static ResourceType Wood = "Wood";
            public static ResourceType Stone = "Stone";
            public static ResourceType Dirt = "Dirt";
            public static ResourceType Mana = "Mana";
            public static ResourceType Gold = "Gold";
            public static ResourceType Iron = "Iron";
            public static ResourceType Berry = "Berry";
            public static ResourceType Mushroom = "Mushroom";
            public static ResourceType Grain = "Grain";
            public static ResourceType Sand = "Sand";
            public static ResourceType Coal = "Coal";
            public static ResourceType Meat = "Meat";
            public static ResourceType Bones = "Bone";
            public static ResourceType Gem = "Gem";
            public static ResourceType Meal = "Meal";
            public static ResourceType Ale = "Ale";
            public static ResourceType Bread = "Bread";
            public static ResourceType Trinket = "Trinket";
            public static ResourceType CaveMushroom = "Cave Mushroom";
            public static ResourceType GemTrinket = "Gem-set Trinket";
            public static ResourceType PineCone = "Pine Cone";
            public static ResourceType Coconut = "Coconut";
            public static ResourceType Cactus = "Cactus";
            public static ResourceType Egg = "Egg";
            public static ResourceType Apple = "Apple";
            public static ResourceType Glass = "Glass";
            public static ResourceType Brick = "Brick";
            public static ResourceType Coins = "Coins";

            public static implicit operator ResourceType(string value)
            {
                return new ResourceType { _value = new string(value.ToCharArray()) };
            }

            public static implicit operator string(ResourceType value)
            {
                return value._value;
            }

            public override string ToString()
            {
                return _value;
            }

            public Resource GetResource()
            {
                return ResourceLibrary.GetResourceByName(_value);
            }
        }

        public static Dictionary<ResourceType, Resource> Resources = new Dictionary<ResourceType, Resource>();

        public static IEnumerable<Resource> GetResourcesByTag(Resource.ResourceTags tag)
        {
            return Resources.Values.Where(resource => resource.Tags.Contains(tag)).ToList();
        }

        public static Resource GetLeastValuableWithTag(Resource.ResourceTags tag)
        {
            Resource min = null;
            DwarfBux minValue = decimal.MaxValue;
            foreach (var r in Resources.Values.Where(resource => resource.Tags.Contains(tag)))
            {
                if (r.MoneyValue < minValue)
                {
                    minValue = r.MoneyValue;
                    min = r;
                }
            }
            return min;
        }

        public static Resource GetResourceByName(string name)
        {
            return (from pair in Resources where pair.Value.ResourceName == name select pair.Value).FirstOrDefault();
        }


        private static Rectangle GetRect(int x, int y)
        {
            int tileSheetWidth = 32;
            int tileSheetHeight = 32;
            return new Rectangle(x * tileSheetWidth, y * tileSheetHeight, tileSheetWidth, tileSheetHeight);
        }

        public static void Add(Resource resource)
        {
            Resources[resource.ResourceName] = resource;
            if (resource.Tags.Contains(Resource.ResourceTags.Money))
            {
                EntityFactory.RegisterEntity(resource.ResourceName + " Resource", (position, data) => new CoinPile(EntityFactory.World.ComponentManager, position)
                {
                    Money = data.Has("Money") ? data.GetData<DwarfBux>("Money") : (DwarfBux)64m
                });
            }
            else
            {
                EntityFactory.RegisterEntity(resource.ResourceName + " Resource", (position, data) => new ResourceEntity(EntityFactory.World.ComponentManager, resource.Type, position));   
            }
        }

        public static void Initialize()
        {
            string tileSheet = ContentPaths.Entities.Resources.resources;
            Resources = new Dictionary<ResourceType, Resource>();
            Add(new Resource(ResourceType.Wood, 10.0m, "Sometimes hard to come by! Comes from trees.", new NamedImageFrame(tileSheet, GetRect(3, 1)), 11, Color.White, Resource.ResourceTags.Wood, Resource.ResourceTags.Material, Resource.ResourceTags.Flammable, Resource.ResourceTags.HardMaterial)
            {
                TrinketData = new Resource.TrinketInfo()
                {
                    BaseAsset = ContentPaths.Entities.DwarfObjects.trinkets_carve,
                    EncrustingAsset = ContentPaths.Entities.DwarfObjects.trinkets_carve_insets,
                    SpriteRow = 0
                }
            });
            Add(new Resource(ResourceType.Stone, 5.0m, "Dwarf's favorite material! Comes from the earth.", new NamedImageFrame(tileSheet, GetRect(3, 0)), 3, Color.White, Resource.ResourceTags.Stone, Resource.ResourceTags.Material, Resource.ResourceTags.HardMaterial)
            {
                TrinketData = new Resource.TrinketInfo()
                {
                    BaseAsset = ContentPaths.Entities.DwarfObjects.trinkets_sculpt,
                    EncrustingAsset = ContentPaths.Entities.DwarfObjects.trinkets_sculpt_insets,
                    SpriteRow = 0
                }
            });
            Add(new Resource(ResourceType.Dirt, 1.0m, "Can be used to make bricks.",
                new NamedImageFrame(tileSheet, GetRect(0, 1)), 8, Color.White, Resource.ResourceTags.Soil,
                Resource.ResourceTags.Material)
                {
                    TrinketData = new Resource.TrinketInfo()
                    {
                        BaseAsset = ContentPaths.Entities.DwarfObjects.trinkets_sculpt,
                        EncrustingAsset = ContentPaths.Entities.DwarfObjects.trinkets_sculpt_insets,
                        SpriteRow = 1
                    }
                });
            Add(new Resource(ResourceType.Sand, 2.0m, "Can be used to make glass.", new NamedImageFrame(tileSheet, GetRect(1, 1)), 9, Color.White, Resource.ResourceTags.Material, Resource.ResourceTags.Sand)
            {
                TrinketData = new Resource.TrinketInfo()
                {
                    BaseAsset = ContentPaths.Entities.DwarfObjects.trinkets_sculpt,
                    EncrustingAsset = ContentPaths.Entities.DwarfObjects.trinkets_sculpt_insets,
                    SpriteRow = 3
                }
            });
            Add(new Resource(ResourceType.Mana, 400.0m, "Mysterious properties!",
                new NamedImageFrame(tileSheet, GetRect(1, 0)), 1, Color.White, Resource.ResourceTags.Magical, Resource.ResourceTags.Precious, Resource.ResourceTags.SelfIlluminating));
            Add(new Resource(ResourceType.Gold, 500.0m, "Shiny!", new NamedImageFrame(tileSheet, GetRect(0, 0)), 0, Color.White, Resource.ResourceTags.Material, Resource.ResourceTags.Metal, Resource.ResourceTags.Precious, Resource.ResourceTags.HardMaterial)
            {
                TrinketData = new Resource.TrinketInfo()
                {
                    BaseAsset = ContentPaths.Entities.DwarfObjects.trinkets_cast,
                    EncrustingAsset = ContentPaths.Entities.DwarfObjects.trinkets_cast_insets,
                    SpriteRow = 1
                }
            });
            Add(new Resource(ResourceType.Coal, 10.0m, "Used as fuel", new NamedImageFrame(tileSheet, GetRect(2, 2)), 18, Color.White, Resource.ResourceTags.Fuel, Resource.ResourceTags.Flammable));
            Add(new Resource(ResourceType.Iron, 50.0m, "Needed to build things.", new NamedImageFrame(tileSheet, GetRect(2, 0)), 2, Color.White, Resource.ResourceTags.Metal, Resource.ResourceTags.Material, Resource.ResourceTags.HardMaterial)
            {
                TrinketData = new Resource.TrinketInfo()
                {
                    BaseAsset = ContentPaths.Entities.DwarfObjects.trinkets_cast,
                    EncrustingAsset = ContentPaths.Entities.DwarfObjects.trinkets_cast_insets,
                    SpriteRow = 0
                }
            });
            Add(new Resource(ResourceType.Berry, 5.0m, "Dwarves can eat these.", new NamedImageFrame(tileSheet, GetRect(2, 1)), 10, Color.White, Resource.ResourceTags.Edible, Resource.ResourceTags.Flammable, Resource.ResourceTags.RawFood, Resource.ResourceTags.Plantable, Resource.ResourceTags.AboveGroundPlant) { FoodContent = 50, PlantToGenerate = "Berry Bush"});
            Add(new Resource(ResourceType.Mushroom, 2.5m, "Dwarves can eat these.", new NamedImageFrame(tileSheet, GetRect(1, 2)), 17, Color.White, Resource.ResourceTags.Edible, Resource.ResourceTags.Fungus, Resource.ResourceTags.Flammable, Resource.ResourceTags.RawFood, Resource.ResourceTags.Brewable, Resource.ResourceTags.Plantable, Resource.ResourceTags.BelowGroundPlant) { FoodContent = 50, PlantToGenerate = "Mushroom" });
            Add(new Resource(ResourceType.CaveMushroom, 3.5m, "Dwarves can eat these.", new NamedImageFrame(tileSheet, GetRect(4, 1)), 12, Color.White, Resource.ResourceTags.SelfIlluminating, Resource.ResourceTags.Edible, Resource.ResourceTags.Fungus, Resource.ResourceTags.Flammable, Resource.ResourceTags.RawFood, Resource.ResourceTags.Brewable, Resource.ResourceTags.Plantable, Resource.ResourceTags.BelowGroundPlant) { FoodContent = 30, PlantToGenerate = "Cave Mushroom" });
            Add(new Resource(ResourceType.Grain, 2.5m, "Dwarves can eat this.", new NamedImageFrame(tileSheet, GetRect(0, 2)), 16, Color.White, Resource.ResourceTags.Edible, Resource.ResourceTags.Grain, Resource.ResourceTags.Flammable, Resource.ResourceTags.RawFood, Resource.ResourceTags.Brewable, Resource.ResourceTags.Bakeable, Resource.ResourceTags.Plantable, Resource.ResourceTags.AboveGroundPlant) { FoodContent = 100, PlantToGenerate = "Wheat" });
            Add(new Resource(ResourceType.Bones, 15.0m, "Came from an animal.", new NamedImageFrame(tileSheet, GetRect(3, 3)), 27, Color.White, Resource.ResourceTags.Bone, Resource.ResourceTags.Material, Resource.ResourceTags.AnimalProduct, Resource.ResourceTags.HardMaterial)
            {
                TrinketData = new Resource.TrinketInfo()
                {
                    BaseAsset = ContentPaths.Entities.DwarfObjects.trinkets_carve,
                    EncrustingAsset = ContentPaths.Entities.DwarfObjects.trinkets_carve_insets_bone,
                    SpriteRow = 1
                }
            });
            Add(new Resource("Corpse", 0.0m, "Dead carcass. Should be buried.", new NamedImageFrame(tileSheet, GetRect(3, 4)), 35, Color.White, Resource.ResourceTags.Corpse));
            Add(new Resource(ResourceType.Meat,  25.0m, "Came from an animal.",
                new NamedImageFrame(tileSheet, GetRect(3, 2)), 19, Color.White, Resource.ResourceTags.Edible,
                Resource.ResourceTags.AnimalProduct, Resource.ResourceTags.Meat, Resource.ResourceTags.RawFood) {FoodContent = 250});

            Add(new Resource(ResourceType.PineCone, 5.0m, "Grows pine trees.",
                new NamedImageFrame(tileSheet, GetRect(6, 1)), 14, Color.White, Resource.ResourceTags.Plantable,
                Resource.ResourceTags.Flammable, Resource.ResourceTags.AboveGroundPlant)
            {
                PlantToGenerate = "Pine Tree"
            });
            Add(new Resource(ResourceType.Coconut, 10.0m, "Grows palm trees.",
                new NamedImageFrame(tileSheet, GetRect(5, 1)), 13, Color.White, Resource.ResourceTags.Plantable,
                 Resource.ResourceTags.Flammable, Resource.ResourceTags.AboveGroundPlant, Resource.ResourceTags.Edible)
            {
                PlantToGenerate = "Palm Tree"
            });
            Add(new Resource(ResourceType.Apple, 5.0m, "Grows apple trees.",
                new NamedImageFrame(tileSheet, GetRect(4, 3)), 28, Color.White, Resource.ResourceTags.Plantable,
                Resource.ResourceTags.Flammable, Resource.ResourceTags.AboveGroundPlant, Resource.ResourceTags.Edible)
            {
                PlantToGenerate = "Apple Tree"
            });
            Add(new Resource(ResourceType.Cactus, 9.0m, "Grows cacti.",
                new NamedImageFrame(tileSheet, GetRect(7, 1)), 15, Color.White, Resource.ResourceTags.Plantable,
                Resource.ResourceTags.Flammable, Resource.ResourceTags.AboveGroundPlant, Resource.ResourceTags.Edible)
            {
                PlantToGenerate = "Cactus"
            });
            Add(new Resource(ResourceType.Bread, 15.0m, "A nutritious dwarf meal.", new NamedImageFrame(tileSheet, GetRect(6, 2)), 22, Color.White, Resource.ResourceTags.Edible, Resource.ResourceTags.Flammable, Resource.ResourceTags.PreparedFood)
            {
                FoodContent = 350
            });
            Add(new Resource(ResourceType.Meal, 30.0m, "A nutritious dwarf meal.", new NamedImageFrame(tileSheet, GetRect(5, 2)), 21, Color.White, Resource.ResourceTags.Edible, Resource.ResourceTags.Flammable, Resource.ResourceTags.PreparedFood)
            {
                FoodContent = 500
            });
            Add(new Resource(ResourceType.Ale, 10.0m, "All dwarves need to drink.",
                new NamedImageFrame(tileSheet, GetRect(4, 2)), 20, Color.White, Resource.ResourceTags.Edible, Resource.ResourceTags.Alcohol,
                Resource.ResourceTags.Flammable)
            {
                FoodContent = 150
            });
            Add(new Resource("Ruby", 350.0m, "Shiny!", new NamedImageFrame(tileSheet, GetRect(0, 3)), 24, Color.White, Resource.ResourceTags.Precious, Resource.ResourceTags.Gem, Resource.ResourceTags.HardMaterial)
            {
                TrinketData = new Resource.TrinketInfo()
                {
                    SpriteRow = 0
                }
            });
            Add(new Resource(Resources["Ruby"])
            {
                Type = "Emerald",
                ShortName = "Emerald",
                Image = new NamedImageFrame(tileSheet, GetRect(0, 4)),
                GuiLayers = new List<TileReference>() { new TileReference("resources", 32) },
                TrinketData = new Resource.TrinketInfo()
                {
                    SpriteRow = 3
                }
            });

            Add(new Resource(Resources["Ruby"])
            {
                Type = "Amethyst",
                ShortName = "Amethyst",
                Image = new NamedImageFrame(tileSheet, GetRect(2, 4)),
                GuiLayers = new List<TileReference>() { new TileReference("resources", 34) },
                TrinketData = new Resource.TrinketInfo()
                {
                    SpriteRow = 5
                }
            });

            Add(new Resource(Resources["Ruby"])
            {
                Type = "Garnet",
                ShortName = "Garnet",
                Image = new NamedImageFrame(tileSheet, GetRect(1, 3)),
                GuiLayers = new List<TileReference>() { new TileReference("resources", 25) },
                TrinketData = new Resource.TrinketInfo()
                {
                    SpriteRow = 1
                }
            });

            Add(new Resource(Resources["Ruby"])
            {
                Type = "Citrine",
                ShortName = "Citrine",
                Image = new NamedImageFrame(tileSheet, GetRect(2, 3)),
                GuiLayers = new List<TileReference>() { new TileReference("resources", 26) },
                TrinketData = new Resource.TrinketInfo()
                {
                    SpriteRow = 2
                }
            });

            Add(new Resource(Resources["Ruby"])
            {
                Type = "Sapphire",
                ShortName = "Sapphire",
                Image = new NamedImageFrame(tileSheet, GetRect(1, 4)),
                GuiLayers = new List<TileReference>() { new TileReference("resources", 33) },
                TrinketData = new Resource.TrinketInfo()
                {
                    SpriteRow = 4
                }
            });

            Add(new Resource(ResourceType.Egg, 5.0m, "An egg", new NamedImageFrame(tileSheet, GetRect(7, 2)), 23, Color.White, Resource.ResourceTags.Edible, Resource.ResourceTags.AnimalProduct, Resource.ResourceTags.Brewable));

            // Todo: Trinkets won't show up right in new gui.
            // Images are in crafts.png - combine into single sprite sheet.
            Add((new Resource(ResourceType.Trinket, 100.0m, "A crafted item.",
                    new NamedImageFrame(ContentPaths.Entities.DwarfObjects.crafts, 32, 0, 0), 0, Color.White, Resource.ResourceTags.Craft, Resource.ResourceTags.Encrustable)));

            Add(new Resource(ResourceType.Glass, 8.0m, "Made from sand. Allows light to pass through.", new NamedImageFrame(tileSheet, GetRect(4, 0)), 4, Color.White, Resource.ResourceTags.Material, Resource.ResourceTags.HardMaterial, Resource.ResourceTags.Craft)
            {
                CanCraft = true,
                CraftPrereqs = new List<Quantitiy<Resource.ResourceTags>>() { new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Sand) },
                TrinketData = new Resource.TrinketInfo()
                {
                    BaseAsset = ContentPaths.Entities.DwarfObjects.trinkets_cast,
                    EncrustingAsset = ContentPaths.Entities.DwarfObjects.trinkets_cast_insets,
                    SpriteRow = 0
                }
            });

            Add(new Resource(ResourceType.Brick, 4.0m, "Made from dirt. Building material.", new NamedImageFrame(tileSheet, GetRect(5, 0)), 5, Color.White, Resource.ResourceTags.Stone, Resource.ResourceTags.Material, Resource.ResourceTags.HardMaterial, Resource.ResourceTags.Craft)
            {
                CanCraft = true,
                CraftPrereqs = new List<Quantitiy<Resource.ResourceTags>>() { new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Soil) },
                TrinketData = new Resource.TrinketInfo()
                {
                    BaseAsset = ContentPaths.Entities.DwarfObjects.trinkets_sculpt,
                    EncrustingAsset = ContentPaths.Entities.DwarfObjects.trinkets_sculpt_insets,
                    SpriteRow = 2
                }
            });

            Add(new Resource(ResourceType.Coins, 64.0m, "Dwarfbux container.",
                new NamedImageFrame(tileSheet, GetRect(6, 0)), 6, Color.White, Resource.ResourceTags.Precious,
                Resource.ResourceTags.Money));



            //GenerateAnimalProducts();
            //GenerateFoods();
        }

        public static void GenerateFoods()
        {
            List<Resource> toAdd = new List<Resource>();
            foreach (Resource resource in Resources.Values)
            {
                if (resource.Tags.Contains(Resource.ResourceTags.Brewable))
                {
                    Resource toReturn = new Resource(Resources[ResourceType.Ale])
                    {
                        Type = resource.Type + " Ale"
                    };
                    toReturn.ShortName = toReturn.Type;

                    if (!Resources.ContainsKey(toReturn.Type))
                    {
                        toAdd.Add(toReturn);
                    }
                }

                if (resource.Tags.Contains(Resource.ResourceTags.Bakeable))
                {
                    Resource toReturn = new Resource(Resources[ResourceType.Bread])
                    {
                        Type = resource.Type + " Bread"
                    };
                    toReturn.ShortName = toReturn.Type;

                    if (!Resources.ContainsKey(toReturn.Type))
                    {
                        toAdd.Add(toReturn);
                    }
                }
            }

            foreach (Resource resource in toAdd)
            {
                Add(resource);
            }

            Resources.Remove(ResourceType.Ale);
            Resources.Remove(ResourceType.Bread);
        }

        public static void GenerateAnimalProducts()
        {
            string[] animals = TextGenerator.GetDefaultStrings("Text" + ProgramData.DirChar + "animal.txt");

            foreach (string animal in animals)
            {
                Resource resource = new Resource(Resources[ResourceType.Meat])
                {
                    Type = animal + " Meat"
                };
                resource.ShortName = resource.Type;

                if (!Resources.ContainsKey(resource.Type))
                    Add(resource);

                Resource boneResource = new Resource(Resources[ResourceType.Bones])
                {
                    Type = animal + " Bone"
                };
                boneResource.ShortName = boneResource.Type;

                if (!Resources.ContainsKey(boneResource.Type))
                    Add(boneResource);

            }

            Resources.Remove(ResourceType.Meat);
            Resources.Remove(ResourceType.Bones);
        }

        public static Resource CreateAle(ResourceType type)
        {
            Resource toReturn = new Resource(Resources[ResourceType.Ale])
            {
                Type = type + " Ale"
            };
            toReturn.ShortName = toReturn.Type;

            if (!Resources.ContainsKey(toReturn.Type))
                Add(toReturn);

            return toReturn;
        }

        public static Resource CreateMeal(ResourceType typeA, ResourceType typeB)
        {
            Resource componentA = Resources[typeA];
            Resource componentB = Resources[typeB];
            Resource toReturn = new Resource(Resources[ResourceType.Meal])
            {
                FoodContent = componentA.FoodContent + componentB.FoodContent,
                Type =
                    TextGenerator.GenerateRandom(new List<string>() {componentA.ResourceName, componentB.ResourceName}, TextGenerator.GetAtoms(ContentPaths.Text.Templates.food)),
                MoneyValue = 2m *(componentA.MoneyValue + componentB.MoneyValue)
            };
            toReturn.ShortName = toReturn.Type;

            if (!Resources.ContainsKey(toReturn.Type))
                Add(toReturn);

            return toReturn;
        }

        public static Resource EncrustTrinket(ResourceType resourcetype, ResourceType gemType)
        {
            Resource toReturn = new Resource(Resources[resourcetype]);
            toReturn.Type = gemType + "-encrusted " + toReturn.ResourceName;
            if (Resources.ContainsKey(toReturn.Type))
            {
                return Resources[toReturn.Type];
            }

            toReturn.MoneyValue += Resources[gemType].MoneyValue * 2m;
            toReturn.Tags = new List<Resource.ResourceTags>() {Resource.ResourceTags.Craft, Resource.ResourceTags.Precious};
            toReturn.CompositeLayers = new List<KeyValuePair<Point, string>>();
            toReturn.CompositeLayers.AddRange(Resources[resourcetype].CompositeLayers);
            toReturn.CompositeLayers.Add(
                new KeyValuePair<Point, string>(
                    new Point(Resources[resourcetype].TrinketData.SpriteColumn, Resources[gemType].TrinketData.SpriteRow),
                    Resources[resourcetype].TrinketData.EncrustingAsset));
            toReturn.GuiLayers = new List<TileReference>();
            toReturn.GuiLayers.AddRange(Resources[resourcetype].GuiLayers);
            toReturn.GuiLayers.Add(new TileReference(Resources[resourcetype].TrinketData.EncrustingAsset, Resources[gemType].TrinketData.SpriteRow * 7 + Resources[resourcetype].TrinketData.SpriteColumn));
            Add(toReturn);
            return toReturn;
        }

        public static Resource GenerateTrinket(ResourceType baseMaterial, float quality)
        {
            Resource toReturn = new Resource(Resources[ResourceType.Trinket]);

            string[] names =
            {
                "Ring",
                "Bracer",
                "Pendant",
                "Figure",
                "Earrings",
                "Staff",
                "Crown"
            };

            int[] tiles =
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6
            };

            float[] values =
            {
                1.5f,
                1.8f,
                1.6f,
                3.0f,
                2.0f,
                3.5f,
                4.0f
            };

            int item = MathFunctions.Random.Next(names.Count());

            string name = names[item];
            Point tile = new Point(tiles[item], Resources[baseMaterial].TrinketData.SpriteRow);
            toReturn.MoneyValue = values[item]*Resources[baseMaterial].MoneyValue * 3m * quality;

            string qualityType = "";

            if (quality < 0.5f)
            {
                qualityType = "Very poor";
            }
            else if (quality < 0.75)
            {
                qualityType = "Poor";
            }
            else if (quality < 1.0f)
            {
                qualityType = "Mediocre";
            }
            else if (quality < 1.25f)
            {
                qualityType = "Good";
            }
            else if (quality < 1.75f)
            {
                qualityType = "Excellent";
            }
            else if(quality < 2.0f)
            {
                qualityType = "Masterwork";
            }
            else
            {
                qualityType = "Legendary";
            }

            toReturn.Type = baseMaterial + " " + name + " (" + qualityType + ")";
            if (Resources.ContainsKey(toReturn.Type))
            {
                return Resources[toReturn.Type];
            }
            toReturn.Tint = Resources[baseMaterial].Tint;
            toReturn.CompositeLayers = new List<KeyValuePair<Point, string>>()
            {
                new KeyValuePair<Point, string>(tile, Resources[baseMaterial].TrinketData.BaseAsset)
            };
            Resource.TrinketInfo trinketInfo = Resources[baseMaterial].TrinketData;
            trinketInfo.SpriteColumn = tile.X;
            toReturn.TrinketData = trinketInfo;
            toReturn.GuiLayers = new List<TileReference>() {new TileReference(Resources[baseMaterial].TrinketData.BaseAsset, tile.Y*7 + tile.X)};
            Add(toReturn);
            toReturn.ShortName = baseMaterial + " " + names[item];
            return toReturn;
        }
        
        public static Resource CreateBread(ResourceType component)
        {
            Resource toReturn = new Resource(Resources[ResourceType.Bread])
            {
                Type = component + " Bread"
            };
            toReturn.ShortName = toReturn.Type;

            if (!Resources.ContainsKey(toReturn.Type))
                Add(toReturn);

            return toReturn;
        }
    }

}
