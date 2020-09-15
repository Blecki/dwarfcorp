using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<string, Race> Races = null;
        private static bool RacesInitialized = false;

        private static void InitializeRaces()
        {
            if (RacesInitialized)
                return;
            RacesInitialized = true;

            Races = new Dictionary<string, Race>();
            foreach (var race in FileUtils.LoadJsonListFromDirectory<Race>("World\\Races", null, r => r.Name))
                Races.Add(race.Name, race);

            Console.WriteLine("Loaded Race Library.");
        }

        public static MaybeNull<Race> GetRace(String Name)
        {
            InitializeRaces();
            Race result = null;
            if (Races.TryGetValue(Name, out result))
                return result;
            return null;
        }

        public static MaybeNull<Race> GetRandomIntelligentRace()
        {
            InitializeRaces();
            return Datastructures.SelectRandom(Races.Values.Where(r => r.IsIntelligent && r.IsNative));
        }
    }
}
