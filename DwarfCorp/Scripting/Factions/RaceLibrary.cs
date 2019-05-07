using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class RaceLibrary
    {
        private static Dictionary<string, Race> Races = null;
        private static bool Initialized = false;

        private static void Initialize()
        {
            if (Initialized)
                return;
            Initialized = true;

            Races = new Dictionary<string, Race>();
            foreach (var race in FileUtils.LoadJsonListFromDirectory<Race>(ContentPaths.World.races, null, r => r.Name))
                Races.Add(race.Name, race);

            Console.WriteLine("Loaded Race Library.");
        }

        public static Race FindRace(String Name)
        {
            Initialize();
            Race result = null;
            if (Races.TryGetValue(Name, out result))
                return result;
            return null;
        }

        public static Race GetRandomIntelligentRace()
        {
            Initialize();
            return Datastructures.SelectRandom(Races.Values.Where(r => r.IsIntelligent && r.IsNative));
        }
    }
}
