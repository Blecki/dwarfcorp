using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<String, CreatureClass> Classes = null;
        private static bool ClassesInitialized = false;

        private static void InitializeClasses()
        {
            if (ClassesInitialized)
                return;
            ClassesInitialized = true;

            var list = FileUtils.LoadJsonListFromDirectory<CreatureClass>("World\\Classes", null, c => c.Name);

            Classes = new Dictionary<String, CreatureClass>();
            foreach (var item in list)
                Classes.Add(item.Name, item);
        }

        public static MaybeNull<CreatureClass> GetClass(String Name)
        {
            InitializeClasses();
            if (Classes.ContainsKey(Name))
                return Classes[Name];
            return null;
        }

        public static IEnumerable<CreatureClass> EnumerateClasses()
        {
            InitializeClasses();
            return Classes.Values;
        }
    }
}
