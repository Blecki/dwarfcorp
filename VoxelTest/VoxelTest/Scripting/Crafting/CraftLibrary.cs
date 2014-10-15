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
