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

            var resourceList = FileUtils.LoadJsonListFromDirectory<ResourceType>("World\\ResourceItems", null, r => r.Name);

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

        public static ResourceType FindMedianResourceTypeWithTag(String tag)
        {
            InitializeResources();
            var applicable = Resources.Values.Where(resource => resource.Tags.Contains(tag)).ToList();
            if (applicable.Count == 0) return null;
            applicable.Sort((a, b) => (int)a.MoneyValue.Value - (int)b.MoneyValue.Value);
            return applicable[applicable.Count / 2];
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

            Resources[resource.Name] = resource;

            if (resource.Tags.Contains("Money"))
                EntityFactory.RegisterEntity(resource.Name + " Resource", (position, data) => new CoinPile(EntityFactory.World.ComponentManager, position)
                {
                    Money = data.Has("Money") ? data.GetData<DwarfBux>("Money") : (DwarfBux)64m
                });
            else
                EntityFactory.RegisterEntity(resource.Name + " Resource", (position, data) => new ResourceEntity(EntityFactory.World.ComponentManager, new Resource(resource.Name), position));   
        }

        public static MaybeNull<Resource> CreateAleResourceType(String type)
        {
            InitializeResources();
            
            var r = new Resource("Ale");
            if (GetResourceType(type).HasValue(out var baseResource))
                r.GeneratedName = String.IsNullOrEmpty(baseResource.AleName) ? type + " Ale" : baseResource.AleName;
            return r;
        }

        public static MaybeNull<Resource> CreateMealResourceType(String typeA, String typeB)
        {
            InitializeResources();
            var r = new Resource("Meal");

            var componentA = GetResourceType(typeA);
            var componentB = GetResourceType(typeB);
            if (componentA.HasValue(out var A) && componentB.HasValue(out var B))
            {
                r.SetMetaData("Food Content", A.FoodContent + B.FoodContent);
                r.SetMetaData("Value", 2m * (A.MoneyValue + B.MoneyValue));
                r.GeneratedName = TextGenerator.GenerateRandom(new List<String>() { A.Name, B.Name }, TextGenerator.GetAtoms(ContentPaths.Text.Templates.food));
            }

            return r;
        }

        public static MaybeNull<Resource> CreateEncrustedTrinketResourceType(Resource BaseResource, Resource GemResource)
        {
            InitializeResources();

            var r = new Resource("Trinket");
            r.GeneratedName = GemResource.Type + "-encrusted " + BaseResource.GeneratedName;

            if (GemResource.ResourceType.HasValue(out var gem))
                r.SetMetaData("Value", BaseResource.GetMetaData<DwarfBux>("Value", 0m) + gem.MoneyValue * 2m);

            var compositeLayers = new List<ResourceType.CompositeLayer>();
            compositeLayers.AddRange(BaseResource.GetMetaData<List<ResourceType.CompositeLayer>>("Composite Layers", new List<ResourceType.CompositeLayer>()));

            var guiLayers = new List<TileReference>();
            guiLayers.AddRange(BaseResource.GetMetaData<List<TileReference>>("Gui Layers", new List<TileReference>()));

            var trinketData = BaseResource.GetMetaData<ResourceType.TrinketInfo>("Trinket Data", BaseResource.ResourceType.HasValue(out var baseRes) ? baseRes.TrinketData : new ResourceType.TrinketInfo());

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

            r.SetMetaData("Composite Layers", compositeLayers);
            r.SetMetaData("Gui Layers", guiLayers);

            return r;
        }

        public static MaybeNull<Resource> CreateTrinketResourceType(String baseMaterial, float quality)
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
            r.GeneratedName = name;
            
            if (GetResourceType(baseMaterial).HasValue(out var material))
            {
                r.SetMetaData("Value", values[item] * material.MoneyValue * 3m * quality);
                r.SetMetaData("Tint", material.Tint);

                var tile = new Point(tiles[item], material.TrinketData.SpriteRow);

                r.SetMetaData("Composite Layers", new List<ResourceType.CompositeLayer>(new ResourceType.CompositeLayer[]
                {
                    new ResourceType.CompositeLayer
                    {
                        Asset = material.TrinketData.BaseAsset,
                        FrameSize = new Point(32, 32),
                        Frame = tile
                    }
                }));

                var trinketInfo = new ResourceType.TrinketInfo
                {
                    BaseAsset = material.TrinketData.BaseAsset,
                    EncrustingAsset = material.TrinketData.EncrustingAsset,
                    SpriteColumn = tile.X,
                    SpriteRow = material.TrinketData.SpriteRow
                };

                r.SetMetaData("Trinket Data", trinketInfo);

                r.SetMetaData("Gui Layers", new List<TileReference>() { new TileReference(material.TrinketData.BaseAsset, tile.Y * 7 + tile.X) });

                return r;
            }

            return null;
        }
        
        public static MaybeNull<Resource> CreateBreadResourceType(String component)
        {
            InitializeResources();
            return new Resource("Bread") { GeneratedName = component + " Bread" };
        }
    }

}
