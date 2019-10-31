using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<String, ResourceType> Resources = null;
        private static bool ResourcesInitialized = false;

        private static void InitializeResources()
        {
            if (ResourcesInitialized)
                return;
            ResourcesInitialized = true;

            Resources = new Dictionary<String, ResourceType>();

            var resourceList = FileUtils.LoadJsonListFromDirectory<ResourceType>("World\\ResourceItems", null, r => r.TypeName);

            foreach (var resource in resourceList)
            {
                resource.Generated = false;
                AddResourceType(resource);
            }

            Console.WriteLine("Loaded Resource Library.");
        }

        public static IEnumerable<ResourceType> EnumerateResourceTypesWithTag(String tag)
        {
            InitializeResources();
            return Resources.Values.Where(resource => resource.Tags.Contains(tag));
        }

        public static MaybeNull<ResourceType> GetResourceType(string name)
        {
            InitializeResources();
            return Resources.ContainsKey((String) name) ? Resources[name] : null;
        }

        public static bool DoesResourceTypeExist(String Name)
        {
            InitializeResources();
            return Resources.ContainsKey(Name);
        }

        public static IEnumerable<ResourceType> EnumerateResourceTypes()
        {
            InitializeResources();
            return Resources.Values;
        }

        public static void AddResourceType(ResourceType resource)
        {
            InitializeResources();

            Resources[resource.TypeName] = resource;

            if (resource.Tags.Contains("Money"))
                EntityFactory.RegisterEntity(resource.TypeName + " Resource", (position, data) => new CoinPile(EntityFactory.World.ComponentManager, position)
                {
                    Money = data.Has("Money") ? data.GetData<DwarfBux>("Money") : (DwarfBux)64m
                });
            else
                EntityFactory.RegisterEntity(resource.TypeName + " Resource", (position, data) => new ResourceEntity(EntityFactory.World.ComponentManager, new Resource(resource.TypeName), position));   
        }

        public static MaybeNull<Resource> CreateAleResourceType(String type)
        {
            InitializeResources();
            if (GetResourceType(type).HasValue(out var baseResource) && !String.IsNullOrEmpty(baseResource.AleName))
                return new Resource("Ale") { DisplayName = baseResource.AleName };
            else
                return new Resource("Ale") { DisplayName = type + " Ale" };
        }

        public static MaybeNull<Resource> CreateMealResource(String typeA, String typeB)
        {
            InitializeResources();
            var r = new Resource("Meal");

            var componentA = GetResourceType(typeA);
            var componentB = GetResourceType(typeB);
            if (componentA.HasValue(out var A) && componentB.HasValue(out var B))
            {
                r.FoodContent = A.FoodContent + B.FoodContent;
                r.MoneyValue = 2m * (A.MoneyValue + B.MoneyValue);
                r.DisplayName = TextGenerator.GenerateRandom(new List<String>() { A.DisplayName, B.DisplayName }, TextGenerator.GetAtoms(ContentPaths.Text.Templates.food));
            }

            return r;
        }

        public static MaybeNull<Resource> CreateEncrustedTrinketResourceType(Resource BaseResource, Resource GemResource)
        {
            InitializeResources();

            var r = new Resource("Trinket");
            r.DisplayName = GemResource.TypeName + "-encrusted " + BaseResource.DisplayName;

            if (GemResource.ResourceType.HasValue(out var gem))
                r.MoneyValue = BaseResource.MoneyValue + gem.MoneyValue * 2m;

            var compositeLayers = new List<ResourceType.CompositeLayer>();
            compositeLayers.AddRange(BaseResource.CompositeLayers);

            var guiLayers = new List<TileReference>();
            guiLayers.AddRange(BaseResource.GuiLayers);

            var trinketData = BaseResource.TrinketData;

            if (GemResource.ResourceType.HasValue(out var gemRes))
            {
                if (trinketData.EncrustingAsset != null)
                    compositeLayers.Add(
                            new ResourceType.CompositeLayer
                            {
                                Asset = trinketData.EncrustingAsset,
                                FrameSize = new Point(32, 32),
                                Frame = new Point(trinketData.SpriteColumn, gemRes.TrinketData.SpriteRow)
                            });

                guiLayers.Add(new TileReference(trinketData.EncrustingAsset, gemRes.TrinketData.SpriteRow * 7 + trinketData.SpriteColumn));
            }

            r.CompositeLayers = compositeLayers;
            r.GuiLayers = guiLayers;

            return r;
        }

        public static MaybeNull<Resource> CreateTrinketResource(String baseMaterial, float quality)
        {
            InitializeResources();

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

            var r = new Resource("Trinket");
            r.DisplayName = name;
            
            if (GetResourceType(baseMaterial).HasValue(out var material))
            {
                r.MoneyValue = values[item] * material.MoneyValue * 3m * quality;
                r.Tint = material.Tint;

                var tile = new Point(tiles[item], material.TrinketData.SpriteRow);

                r.CompositeLayers = new List<ResourceType.CompositeLayer>(new ResourceType.CompositeLayer[]
                {
                    new ResourceType.CompositeLayer
                    {
                        Asset = material.TrinketData.BaseAsset,
                        FrameSize = new Point(32, 32),
                        Frame = tile
                    }
                });

                var trinketInfo = new ResourceType.TrinketInfo
                {
                    BaseAsset = material.TrinketData.BaseAsset,
                    EncrustingAsset = material.TrinketData.EncrustingAsset,
                    SpriteColumn = tile.X,
                    SpriteRow = material.TrinketData.SpriteRow
                };

                r.TrinketData = trinketInfo;

                r.GuiLayers = new List<TileReference>() { new TileReference(material.TrinketData.BaseAsset, tile.Y * 7 + tile.X) };

                return r;
            }

            return null;
        }
        
        public static MaybeNull<Resource> CreateBreadResource(String component)
        {
            InitializeResources();
            return new Resource("Bread") { DisplayName = component + " Bread" };
        }
    }

}
