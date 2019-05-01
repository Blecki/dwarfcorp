using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public static class CreatureClassLibrary
    {
        private static Dictionary<String, CreatureClass> Classes = null;
        private static bool Initialized = false;

        private static void Initialize()
        {
            if (Initialized)
                return;
            Initialized = true;

            var list = FileUtils.LoadJsonListFromDirectory<CreatureClass>(ContentPaths.creature_classes, null, c => c.Name);

            Classes = new Dictionary<String, CreatureClass>();
            foreach (var item in list)
                Classes.Add(item.Name, item);
        }

        public static CreatureClass GetClass(String Name)
        {
            Initialize();
            return Classes[Name];
        }

        public static IEnumerable<CreatureClass> EnumerateClasses()
        {
            Initialize();
            return Classes.Values;
        }
    }
}
