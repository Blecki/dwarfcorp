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
                Resources[resource.TypeName] = resource;

                //if (resource.Tags.Contains("Money"))
                //    EntityFactory.RegisterEntity(resource.TypeName + " Resource", (position, data) => new CoinPile(EntityFactory.World.ComponentManager, position)
                //    {
                //        Money = data.Has("Money") ? data.GetData<DwarfBux>("Money") : (DwarfBux)64m
                //    });
                //else
                //    EntityFactory.RegisterEntity(resource.TypeName + " Resource", (position, data) => new ResourceEntity(EntityFactory.World.ComponentManager, new Resource(resource.TypeName), position));
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

        public static IEnumerable<ResourceType> EnumerateResourceTypes()
        {
            InitializeResources();
            return Resources.Values;
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
        public class MetaResourceFactoryAttribute : Attribute
        {
            public String Name;

            public MetaResourceFactoryAttribute(String Name)
            {
                this.Name = Name;
            }
        }

        private static Dictionary<String, Func<CreatureAI, Resource, List<Resource>, MaybeNull<Resource>>> MetaResourceFactories;

        private static void InitializeMetaResourceFactories()
        {
            if (MetaResourceFactories != null)
                return;

            MetaResourceFactories = new Dictionary<string, Func<CreatureAI, Resource, List<Resource>, MaybeNull<Resource>>>();
            foreach (var method in AssetManager.EnumerateModHooks(typeof(MetaResourceFactoryAttribute), typeof(MaybeNull<Resource>), new Type[]
            {
                typeof(CreatureAI),
                typeof(Resource),
                typeof(List<Resource>)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is MetaResourceFactoryAttribute) as MetaResourceFactoryAttribute;
                if (attribute == null) continue;
                MetaResourceFactories[attribute.Name] = (agent, @base, ingredients) =>
                {
                    var r = method.Invoke(null, new Object[] { agent, @base, ingredients }) as MaybeNull<Resource>?;
                    if (r.HasValue)
                        return r.Value;
                    else
                        return null;
                };
            }
        }

        public static MaybeNull<Resource> CreateMetaResource(String FactoryName, CreatureAI Agent, Resource Base, List<Resource> Ingredients)
        {
            InitializeMetaResourceFactories();
            if (MetaResourceFactories.ContainsKey(FactoryName))
                return MetaResourceFactories[FactoryName](Agent, Base, Ingredients);
            else
                return null;
        }

        [MetaResourceFactory("Ale")]
        private static MaybeNull<Resource> _makeAle(CreatureAI Agent, Resource Base, List<Resource> Ingredients)
        {
            InitializeResources();

            if (Ingredients.Count == 0)
                return null;

            if (Ingredients[0].ResourceType.HasValue(out var baseType) && !String.IsNullOrEmpty(baseType.AleName))
                return new Resource(Base.TypeName) { DisplayName = baseType.AleName }; // Todo: Just require all brewable resources to set their alename.
            else
                return new Resource(Base.TypeName) { DisplayName = Ingredients[0].DisplayName + " Ale" };
        }

        [MetaResourceFactory("Meal")]
        private static MaybeNull<Resource> _makeMeal(CreatureAI Agent, Resource Base, List<Resource> Ingredients)
        {
            InitializeResources();

            if (Ingredients.Count < 2)
                return null;

            if (Ingredients[0].ResourceType.HasValue(out var a) && Ingredients[1].ResourceType.HasValue(out var b))
                return new Resource(Base.TypeName)
                {
                    FoodContent = a.FoodContent + b.FoodContent,
                    MoneyValue = 2m * (a.MoneyValue + b.MoneyValue),
                    DisplayName = TextGenerator.GenerateRandom(new List<String>() { a.DisplayName, b.DisplayName }, TextGenerator.GetAtoms(ContentPaths.Text.Templates.food))
                };
            else
                return null;
        }

        [MetaResourceFactory("GemTrinket")]
        private static MaybeNull<Resource> _makeEncrustedTrinket(CreatureAI Agent, Resource Base, List<Resource> Ingredients)
        {
            InitializeResources();

            if (Ingredients.Count < 2)
                return null;

            Resource baseResource = null;
            Resource gemResource = null;
            foreach (var ingredient in Ingredients)
                if (ingredient.ResourceType.HasValue(out var res))
                {
                    if (res.Tags.Contains("Encrustable"))
                        baseResource = ingredient;
                    else if (res.Tags.Contains("Gem"))
                        gemResource = ingredient;
                }

            if (baseResource == null || gemResource == null || baseResource.EncrustingDataEx == null || baseResource.EncrustingDataEx.EncrustingGraphic == null)
                return null;

            var r = new Resource(Base.TypeName);
            r.DisplayName = gemResource.DisplayName + "-encrusted " + baseResource.DisplayName;

            if (gemResource.ResourceType.HasValue(out var gem))
                r.MoneyValue = baseResource.MoneyValue + gem.MoneyValue * 2m;

            r.Gui_Graphic = baseResource.Gui_Graphic.Clone();
            r.Gui_Graphic.NextLayer = baseResource.EncrustingDataEx.EncrustingGraphic.Clone();
            r.Gui_Graphic.NextLayer.Palette = gemResource.Trinket_JewellPalette;

            return r;
        }

        [MetaResourceFactory("Trinket")]
        private static MaybeNull<Resource> _makeTrinket(CreatureAI Agent, Resource Base, List<Resource> Ingredients)
        {
            InitializeResources();

            if (Ingredients.Count == 0 || Ingredients[0].TrinketDataEx == null)
                return null;

            var item = Ingredients[0].TrinketDataEx.SelectRandom();

            var quality = Agent != null ? (Agent.Stats.Dexterity + Agent.Stats.Intelligence) / 15.0f * MathFunctions.Rand(0.5f, 1.75f) : MathFunctions.Rand(0.1f, 3.0f);

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
            else if (quality < 2.0f)
                qualityType = "Masterwork";
            else
                qualityType = "Legendary";

            var r = new Resource(Base.TypeName);
            r.DisplayName = Ingredients[0].DisplayName + " " + item.Name + " (" + qualityType + ")";

            r.MoneyValue =item.Value * Ingredients[0].MoneyValue * 3m * quality;
            r.Tint = Ingredients[0].Tint;
            r.EncrustingDataEx = item;
            r.Gui_Graphic = item.Graphic.Clone();

            return r;
        }

        [MetaResourceFactory("Bread")]
        private static MaybeNull<Resource> _makeBread(CreatureAI Agent, Resource Base, List<Resource> Ingredients)
        {
            InitializeResources();

            if (Ingredients.Count == 0)
                return null;

            return new Resource(Base.TypeName) { DisplayName = Ingredients[0].DisplayName + " Bread" };
        }

        [MetaResourceFactory("Normal")]
        private static MaybeNull<Resource> _metaResourcePassThroughFactory(CreatureAI Agent, Resource Base, List<Resource> Ingredients)
        {
            InitializeResources();

            if (Ingredients.Count == 0)
                return null;

            return Base;
        }
    }
}
