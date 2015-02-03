using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// A static collection of resource types (should eventually be replaced with a file)
    /// </summary>
    [JsonObject(IsReference = true)]
    public class ResourceLibrary
    {
        public enum ResourceType
        {
            Wood,
            Stone,
            Dirt,
            Mana,
            Gold,
            Iron,
            Berry,
            Mushroom,
            Grain,
            Sand,
            Coal
        }

        public static Dictionary<ResourceType, Resource> Resources = new Dictionary<ResourceType, Resource>();

        public static Dictionary<ResourceType, string> ResourceNames = new Dictionary<ResourceType, string>()
        {
            {
                ResourceType.Wood, "Wood"
            },
            {
                ResourceType.Stone, "Stone"
            },
            {
                ResourceType.Dirt, "Dirt"
            },
            {
                ResourceType.Mana, "Mana"
            },
            {
                ResourceType.Gold, "Gold"
            },
            {
                ResourceType.Iron, "Iron"
            },
            {
                ResourceType.Berry, "Berry"
            },
            {
                ResourceType.Mushroom, "Mushroom"
            },
            {
                ResourceType.Grain, "Grain"
            },
            {
                ResourceType.Sand, "Sand"
            },
            {
                ResourceType.Coal, "Coal"
            }
        };


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitializeStatics();
        }
       

        public static Resource GetResourceByName(string name)
        {
            return (from pair in ResourceNames where pair.Value == name select Resources[pair.Key]).FirstOrDefault();
        }


        private static Rectangle GetRect(int x, int y)
        {
            int tileSheetWidth = AssetSettings.Default.ResourceSheet_tileWidth;
            int tileSheetHeight = AssetSettings.Default.ResourceSheet_tileHeight;
            return new Rectangle(x * tileSheetWidth, y * tileSheetHeight, tileSheetWidth, tileSheetHeight);
        }

        public void InitializeStatics()
        {
            string tileSheet = ContentPaths.Entities.Resources.resources;
            Resources = new Dictionary<ResourceType, Resource>();
            Resources[ResourceType.Wood] = new Resource(ResourceType.Wood, 1.0f, "Sometimes hard to come by! Comes from trees.", new NamedImageFrame(tileSheet, GetRect(3, 1)), Resource.ResourceTags.Material);
            Resources[ResourceType.Stone] = new Resource(ResourceType.Stone, 0.5f, "Dwarf's favorite material! Comes from the earth.", new NamedImageFrame(tileSheet, GetRect(3, 0)), Resource.ResourceTags.Material);
            Resources[ResourceType.Dirt] = new Resource(ResourceType.Dirt, 0.1f, "Can't get rid of it! Comes from the earth.", new NamedImageFrame(tileSheet, GetRect(0, 1)), Resource.ResourceTags.Material);
            Resources[ResourceType.Sand] = new Resource(ResourceType.Sand, 0.2f, "Can't get rid of it! Comes from the earth.", new NamedImageFrame(tileSheet, GetRect(1, 1)), Resource.ResourceTags.Material);
            Resources[ResourceType.Mana] = new Resource(ResourceType.Mana, 100.0f, "Mysterious properties!",
                new NamedImageFrame(tileSheet, GetRect(1, 0)), Resource.ResourceTags.Precious) { SelfIlluminating = true };
            Resources[ResourceType.Gold] = new Resource(ResourceType.Gold, 50.0f, "Shiny!", new NamedImageFrame(tileSheet, GetRect(0, 0)), Resource.ResourceTags.Precious);
            Resources[ResourceType.Coal] = new Resource(ResourceType.Coal, 10.0f, "Used as fuel", new NamedImageFrame(tileSheet, GetRect(2, 2)), Resource.ResourceTags.Material);
            Resources[ResourceType.Iron] = new Resource(ResourceType.Iron, 5.0f, "Needed to build things.", new NamedImageFrame(tileSheet, GetRect(2, 0)), Resource.ResourceTags.Material);
            Resources[ResourceType.Berry] = new Resource(ResourceType.Berry, 0.5f, "Dwarves can eat these.", new NamedImageFrame(tileSheet, GetRect(2, 1)), Resource.ResourceTags.Food) { FoodContent = 50 };
            Resources[ResourceType.Mushroom] = new Resource(ResourceType.Mushroom, 0.25f, "Dwarves can eat these.", new NamedImageFrame(tileSheet, GetRect(1, 2)), Resource.ResourceTags.Food) { FoodContent = 50 };
            Resources[ResourceType.Grain] = new Resource(ResourceType.Grain, 0.25f, "Dwarves can eat this.", new NamedImageFrame(tileSheet, GetRect(0, 2)), Resource.ResourceTags.Food) { FoodContent = 100 };
        
        }

        public ResourceLibrary()
        {
            InitializeStatics();
        }


    }

}