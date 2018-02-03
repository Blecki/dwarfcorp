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
using System.Text;
using System;

namespace DwarfCorp
{

    /// <summary>
    /// A static collection of resource types (should eventually be replaced with a file)
    /// </summary>
    [JsonObject(IsReference = true)]
    public static class ResourceLibrary
    {
        public static Dictionary<ResourceType, Resource> Resources = new Dictionary<ResourceType, Resource>();


        public static IEnumerable<Resource> GetResourcesByTag(Resource.ResourceTags tag)
        {
            return Resources.Values.Where(resource => resource.Tags.Contains(tag));
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
            return Resources.ContainsKey((ResourceType) name) ? Resources[name] : null;
        }


        private static Rectangle GetRect(int x, int y)
        {
            int tileSheetWidth = 32;
            int tileSheetHeight = 32;
            return new Rectangle(x * tileSheetWidth, y * tileSheetHeight, tileSheetWidth, tileSheetHeight);
        }

        public static void Add(Resource resource)
        {
            Resources[resource.Name] = resource;

            if (resource.Tags.Contains(Resource.ResourceTags.Money))
            {
                EntityFactory.RegisterEntity(resource.Name + " Resource", (position, data) => new CoinPile(EntityFactory.World.ComponentManager, position)
                {
                    Money = data.Has("Money") ? data.GetData<DwarfBux>("Money") : (DwarfBux)64m
                });
            }
            else
            {
                EntityFactory.RegisterEntity(resource.Name + " Resource", (position, data) => new ResourceEntity(EntityFactory.World.ComponentManager, new ResourceAmount(resource, data.GetData<int>("num", 1)), position));   
            }
        }

        public static void Initialize()
        {
            string tileSheet = ContentPaths.Entities.Resources.resources;
            Resources = new Dictionary<ResourceType, Resource>();

            var resourceList = FileUtils.LoadJsonListFromMultipleSources<Resource>(ContentPaths.resource_items, null, r => r.Name);

            foreach (var resource in resourceList)
                Add(resource);
        }

        private static Dictionary<string, string> MeatAssets = new Dictionary<string, string>()
        {
            {
                "Bird",
                "Bird Meat"
            },
            {
                "Chicken",
                "Bird Meat"
            },
            {
                "Turkey",
                "Bird Meat"
            },
            {
                "Penguin",
                "Bird Meat"
            }
        };


        public static Resource GetMeat(string species)
        {
            if (MeatAssets.ContainsKey(species))
            {
                return Resources[MeatAssets[species]];
            }
            return Resources[ResourceType.Meat];
        }

        public static Resource CreateAle(ResourceType type)
        {
            Resource toReturn = new Resource(Resources[ResourceType.Ale])
            {
                Name = string.IsNullOrEmpty(Resources[type].AleName) ? type + " Ale" : Resources[type].AleName
            };
            toReturn.ShortName = toReturn.Name;

            if (!Resources.ContainsKey(toReturn.Name))
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
                Name =
                    TextGenerator.GenerateRandom(new List<string>() {componentA.Name, componentB.Name}, TextGenerator.GetAtoms(ContentPaths.Text.Templates.food)),
                MoneyValue = 2m *(componentA.MoneyValue + componentB.MoneyValue)
            };
            toReturn.ShortName = toReturn.Name;

            if (!Resources.ContainsKey(toReturn.Name))
                Add(toReturn);

            return toReturn;
        }

        public static Resource EncrustTrinket(ResourceType resourcetype, ResourceType gemType)
        {
            Resource toReturn = new Resource(Resources[resourcetype]);
            toReturn.Name = gemType + "-encrusted " + toReturn.Name;
            if (Resources.ContainsKey(toReturn.Name))
            {
                return Resources[toReturn.Name];
            }

            toReturn.MoneyValue += Resources[gemType].MoneyValue * 2m;
            toReturn.Tags = new List<Resource.ResourceTags>() {Resource.ResourceTags.Craft, Resource.ResourceTags.Precious};
            toReturn.CompositeLayers = new List<Resource.CompositeLayer>();
            toReturn.CompositeLayers.AddRange(Resources[resourcetype].CompositeLayers);
            if (Resources[resourcetype].TrinketData.EncrustingAsset != null)
            {
                toReturn.CompositeLayers.Add(
                    new Resource.CompositeLayer
                    {
                        Asset = Resources[resourcetype].TrinketData.EncrustingAsset,
                        FrameSize = new Point(32, 32),
                        Frame = new Point(Resources[resourcetype].TrinketData.SpriteColumn, Resources[gemType].TrinketData.SpriteRow)
                    });
            }
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

            toReturn.Name = baseMaterial + " " + name + " (" + qualityType + ")";
            if (Resources.ContainsKey(toReturn.Name))
            {
                return Resources[toReturn.Name];
            }
            toReturn.Tint = Resources[baseMaterial].Tint;
            toReturn.CompositeLayers = new List<Resource.CompositeLayer>(new Resource.CompositeLayer[]
            {
                new Resource.CompositeLayer
                {
                    Asset = Resources[baseMaterial].TrinketData.BaseAsset,
                    FrameSize = new Point(32, 32),
                    Frame = tile
                }
            });
            
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
                Name = component + " Bread"
            };
            toReturn.ShortName = toReturn.Name;

            if (!Resources.ContainsKey(toReturn.Name))
                Add(toReturn);

            return toReturn;
        }
    }

}
