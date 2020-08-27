using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<String, Loadout> Loadouts = null;
        private static bool LoadoutsInitialized = false;

        private static void InitializeLoadouts()
        {
            if (LoadoutsInitialized)
                return;
            LoadoutsInitialized = true;

            var list = FileUtils.LoadJsonListFromDirectory<Loadout>("World\\Loadouts", null, c => c.Name);

            Loadouts = new Dictionary<String, Loadout>();
            foreach (var item in list)
                Loadouts.Add(item.Name, item);
        }

        public static MaybeNull<Loadout> GetLoadout(String Name)
        {
            InitializeLoadouts();
            if (Loadouts.ContainsKey(Name))
                return Loadouts[Name];
            return null;
        }

        public static IEnumerable<Loadout> EnumerateLoadouts()
        {
            InitializeLoadouts();
            return Loadouts.Values;
        }
    }
}
