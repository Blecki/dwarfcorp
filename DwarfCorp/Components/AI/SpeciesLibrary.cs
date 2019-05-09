using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<String, CreatureSpecies> Species = null;
        private static bool SpeciesInitialized = false;

        private static void InitializeSpecies()
        {
            if (SpeciesInitialized)
                return;
            SpeciesInitialized = true;

            var list = FileUtils.LoadJsonListFromDirectory<CreatureSpecies>("World\\Species", null, c => c.Name);

            Species = new Dictionary<String, CreatureSpecies>();
            foreach (var item in list)
                Species.Add(item.Name, item);
        }

        public static CreatureSpecies GetSpecies(String Name)
        {
            InitializeSpecies();
            return Species[Name];
        }

        public static IEnumerable<CreatureSpecies> EnumerateSpecies()
        {
            InitializeSpecies();
            return Species.Values;
        }
    }
}
