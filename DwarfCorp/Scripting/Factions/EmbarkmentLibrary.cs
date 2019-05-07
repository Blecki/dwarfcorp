using System.Collections.Generic;
using System;
using System.Linq;

namespace DwarfCorp
{
    public class EmbarkmentLibrary
    {
        private static List<Embarkment> Embarkments;
        public static Embarkment DefaultEmbarkment => GetEmbarkment("Normal");

        public static void InitializeDefaultLibrary()
        {
            Embarkments = FileUtils.LoadJsonListFromDirectory<Embarkment>(ContentPaths.World.embarks, null, e => e.Name);
            Embarkments.Sort((a, b) => b.Difficulty - a.Difficulty);

            Console.WriteLine("Loaded Embarkment Library.");
        }

        public static Embarkment GetEmbarkment(String Name)
        {
            return Embarkments.FirstOrDefault(e => e.Name == Name);
        }

        public static IEnumerable<Embarkment> Enumerate()
        {
            return Embarkments;
        }
    }
}
