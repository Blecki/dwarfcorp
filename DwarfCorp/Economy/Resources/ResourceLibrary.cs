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
    // Todo: Lock down.
    public static class ResourceLibrary
    {
        private static Dictionary<String, Resource> Resources = null;
        private static bool IsInitialized = false;

        private static void Initialize()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;

            Resources = new Dictionary<String, Resource>();

            var resourceList = FileUtils.LoadJsonListFromMultipleSources<Resource>(ContentPaths.resource_items, null, r => r.Name);

            foreach (var resource in resourceList)
            {
                resource.Generated = false;
                Add(resource);
            }
        }

        public static IEnumerable<Resource> FindResourcesWithTag(Resource.ResourceTags tag)
        {
            Initialize();
            return Resources.Values.Where(resource => resource.Tags.Contains(tag));
        }

        public static Resource FindMedianWithTag(Resource.ResourceTags tag)
        {
            Initialize();
            var applicable = Resources.Values.Where(resource => resource.Tags.Contains(tag)).ToList();
            if (applicable.Count == 0) return null;
            applicable.Sort((a, b) => (int)a.MoneyValue.Value - (int)b.MoneyValue.Value);
            return applicable[applicable.Count / 2];
        }

        public static Resource GetResourceByName(string name)
        {
            Initialize();
            return Resources.ContainsKey((String) name) ? Resources[name] : null;
        }

        public static bool Exists(String Name)
        {
            Initialize();
            return Resources.ContainsKey(Name);
        }

        public static IEnumerable<Resource> Enumerate()
        {
            Initialize();
            return Resources.Values;
        }

        public static void Add(Resource resource)
        {
            Initialize();

            Resources[resource.Name] = resource;

            if (resource.Tags.Contains(Resource.ResourceTags.Money))
                EntityFactory.RegisterEntity(resource.Name + " Resource", (position, data) => new CoinPile(EntityFactory.World.ComponentManager, position)
                {
                    Money = data.Has("Money") ? data.GetData<DwarfBux>("Money") : (DwarfBux)64m
                });
            else
                EntityFactory.RegisterEntity(resource.Name + " Resource", (position, data) => new ResourceEntity(EntityFactory.World.ComponentManager, new ResourceAmount(resource, data.GetData<int>("num", 1)), position));   
        }

        public static void AddIfNew(Resource Resource)
        {
            Initialize();

            if (!Exists(Resource.Name))
                Add(Resource);
        }

        public static Resource GenerateResource(Resource From)
        {
            var r = GenerateResource();

            r.Generated = true;

            r.Name = From.Name;
            r.ShortName = From.ShortName;
            r.MoneyValue = From.MoneyValue;
            r.Description = From.Description;
            r.GuiLayers = new List<TileReference>(From.GuiLayers);
            r.Tint = From.Tint;
            r.Tags = new List<Resource.ResourceTags>(From.Tags);
            r.FoodContent = From.FoodContent;
            r.PlantToGenerate = From.PlantToGenerate;
            r.CanCraft = From.CanCraft;
            r.CraftPrerequisites = new List<Quantitiy<Resource.ResourceTags>>(From.CraftPrerequisites);
            r.CraftInfo = From.CraftInfo;
            r.MaterialStrength = From.MaterialStrength;
            r.CompositeLayers = From.CompositeLayers == null ? null : new List<Resource.CompositeLayer>(From.CompositeLayers);
            r.TrinketData = From.TrinketData;
            r.AleName = From.AleName;
            r.PotionType = From.PotionType;

            return r;
        }

        public static Resource GenerateResource()
        {
            return new Resource()
            {
                Generated = true
            };
        }
        
        public static Resource CreateAle(String type)
        {
            Initialize();

            var baseResource = GetResourceByName(type);
            var aleName = String.IsNullOrEmpty(baseResource.AleName) ? type + " Ale" : baseResource.AleName;

            if (!Exists(aleName))
            {
                var r = GenerateResource(GetResourceByName(ResourceType.Ale));
                r.Name = aleName;
                r.ShortName = aleName;
                Add(r);
            }

            return GetResourceByName(aleName);
        }

        public static Resource CreateMeal(String typeA, String typeB)
        {
            Initialize();

            var componentA = GetResourceByName(typeA);
            var componentB = GetResourceByName(typeB);
            var r = GenerateResource(GetResourceByName(ResourceType.Meal));
            r.FoodContent = componentA.FoodContent + componentB.FoodContent;
            r.Name = TextGenerator.GenerateRandom(new List<String>() { componentA.Name, componentB.Name }, TextGenerator.GetAtoms(ContentPaths.Text.Templates.food));
            r.MoneyValue = 2m * (componentA.MoneyValue + componentB.MoneyValue);
            r.ShortName = r.Name;

            AddIfNew(r);
            return GetResourceByName(r.Name);
        }

        public static Resource EncrustTrinket(String resourcetype, String gemType)
        {
            Initialize();

            var resultName = gemType + "-encrusted " + resourcetype;
            if (Exists(resultName))
                return GetResourceByName(resultName);

            var baseResource = GetResourceByName(resourcetype);
            var gemResource = GetResourceByName(gemType);

            var toReturn = GenerateResource(baseResource);
            toReturn.Name = resultName;
            toReturn.MoneyValue += gemResource.MoneyValue * 2m;
            toReturn.Tags = new List<Resource.ResourceTags>() { Resource.ResourceTags.Craft, Resource.ResourceTags.Precious };

            toReturn.CompositeLayers = new List<Resource.CompositeLayer>();
            toReturn.CompositeLayers.AddRange(baseResource.CompositeLayers);
            if (baseResource.TrinketData.EncrustingAsset != null)
                toReturn.CompositeLayers.Add(
                    new Resource.CompositeLayer
                    {
                        Asset = baseResource.TrinketData.EncrustingAsset,
                        FrameSize = new Point(32, 32),
                        Frame = new Point(baseResource.TrinketData.SpriteColumn, gemResource.TrinketData.SpriteRow)
                    });

            toReturn.GuiLayers = new List<TileReference>();
            toReturn.GuiLayers.AddRange(baseResource.GuiLayers);
            toReturn.GuiLayers.Add(new TileReference(baseResource.TrinketData.EncrustingAsset, gemResource.TrinketData.SpriteRow * 7 + baseResource.TrinketData.SpriteColumn));

            Add(toReturn);
            return toReturn;
        }

        public static Resource GenerateTrinket(String baseMaterial, float quality)
        {
            Initialize();

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

            string qualityType = "";
            if (quality < 0.5f)
                qualityType = "Very poor";
            else if (quality < 0.75)
                qualityType = "Poor";
            else if (quality < 1.0f)
                qualityType = "Mediocre";
           else if (quality < 1.25f)
                qualityType = "Good";
            else if (quality < 1.75f)
                qualityType = "Excellent";
            else if(quality < 2.0f)
                qualityType = "Masterwork";
            else
                qualityType = "Legendary";

            var item = MathFunctions.Random.Next(names.Count());
            var name = baseMaterial + " " + names[item] + " (" + qualityType + ")";

            if (Exists(name))
                return GetResourceByName(name);

            var material = GetResourceByName(baseMaterial);

            var toReturn = GenerateResource(Resources[ResourceType.Trinket]);
            toReturn.Name = name;
            toReturn.ShortName = baseMaterial + " " + names[item];
            toReturn.MoneyValue = values[item] * material.MoneyValue * 3m * quality;
            toReturn.Tint = material.Tint;

            var tile = new Point(tiles[item], material.TrinketData.SpriteRow);
            
            toReturn.CompositeLayers = new List<Resource.CompositeLayer>(new Resource.CompositeLayer[]
            {
                new Resource.CompositeLayer
                {
                    Asset = material.TrinketData.BaseAsset,
                    FrameSize = new Point(32, 32),
                    Frame = tile
                }
            });
            
            var trinketInfo = material.TrinketData;
            trinketInfo.SpriteColumn = tile.X;
            toReturn.TrinketData = trinketInfo;
            toReturn.GuiLayers = new List<TileReference>() {new TileReference(material.TrinketData.BaseAsset, tile.Y*7 + tile.X)};

            Add(toReturn);
            return toReturn;
        }
        
        public static Resource CreateBread(String component)
        {
            Initialize();

            if (Exists(component + " Bread"))
                return GetResourceByName(component + " Bread");

            var toReturn = GenerateResource(GetResourceByName(ResourceType.Bread));
            toReturn.Name = component + " Bread";
            Add(toReturn);
            return toReturn;
        }
    }

}
