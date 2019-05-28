using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<string, CraftItem> CraftItems = null;
        private static bool CraftLibraryInitialized = false;

        public static IEnumerable<CraftItem> EnumerateCraftables()
        {
            InitializeCraftLibrary();
            return CraftItems.Values;
        }

        public static CraftItem GetCraftable(string Name)
        {
            InitializeCraftLibrary();
            if (CraftItems.ContainsKey(Name))
                return CraftItems[Name];
            return null;
        }

        public static void AddCraftable(CraftItem craft)
        {
            InitializeCraftLibrary();
            CraftItems[craft.Name] = craft;
        }

        private static void InitializeCraftLibrary()
        {
            if (CraftLibraryInitialized)
                return;
            CraftLibraryInitialized = true;

            var craftList = FileUtils.LoadJsonListFromDirectory<CraftItem>(ContentPaths.craft_items, null, c => c.Name);
            CraftItems = new Dictionary<string, CraftItem>();

            foreach (var type in craftList)
            {
                type.InitializeStrings();
                CraftItems.Add(type.Name, type);
            }

            Console.WriteLine("Loaded Craft Library.");
        }

        public static CraftItem GetRandomApplicableCraftItem(Faction faction, WorldManager World)
        {
            InitializeCraftLibrary();

            const int maxIters = 100;

            for (int i = 0; i < maxIters; i++)
            {
                var item = Datastructures.SelectRandom(CraftItems.Where(k => k.Value.Type == CraftItem.CraftType.Resource));
                if (!World.HasResources(item.Value.RequiredResources))
                    continue;
                if (!faction.OwnedObjects.Any(o => o.Tags.Contains(item.Value.CraftLocation)))
                    continue;
                return item.Value;
            }

            return null;
        }
    }
}
