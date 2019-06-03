using System.Collections.Generic;
using System;
using System.Linq;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static List<Embarkment> Embarkments;
        public static Embarkment DefaultEmbarkment => GetEmbarkment("Normal");
        private static bool EmbarkmentsInitialized = false;

        private static void InitializeEmbarkments()
        {
            if (EmbarkmentsInitialized)
                return;
            EmbarkmentsInitialized = true;

            Embarkments = FileUtils.LoadJsonListFromDirectory<Embarkment>(ContentPaths.World.embarks, null, e => e.Name);
            Embarkments.Sort((a, b) => b.Difficulty - a.Difficulty);

            Console.WriteLine("Loaded Embarkment Library.");
        }

        public static Embarkment GetEmbarkment(String Name)
        {
            InitializeEmbarkments();
            return Embarkments.FirstOrDefault(e => e.Name == Name);
        }

        public static IEnumerable<Embarkment> EnumerateEmbarkments()
        {
            InitializeEmbarkments();
            return Embarkments;
        }
    }
}
