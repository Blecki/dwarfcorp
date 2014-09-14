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
            BearTrap
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
                        Name = "Bear Trap",
                        RequiredResources = new List<ResourceAmount>()
                        {
                            new ResourceAmount(ResourceLibrary.ResourceType.Iron, 4)
                        }
                    }

                }
            };

            staticsInitialized = true;
        }
    }
}
