using System.Collections.Generic;
using System;
using System.Linq;

namespace DwarfCorp
{
    public class Difficulty
    {
        public int CombatModifier = 0;
        public String Name = "Tranquil";
        public DwarfBux StartingFunds;
        public List<String> Dwarves = new List<string>();

        public Difficulty(int v)
        { }
    }

    public static partial class Library
    {
        private static List<Difficulty> Difficulties = null;
        private static bool DifficultiesInitialized = false;

        private static void InitializeDifficulties()
        {
            if (DifficultiesInitialized)
                return;
            DifficultiesInitialized = true;

            Difficulties = FileUtils.LoadJsonListFromDirectory<Difficulty>("World/Difficulties", null, b => b.Name);
            Difficulties.Sort((a, b) => a.CombatModifier - b.CombatModifier);

            Console.WriteLine("Loaded Difficulty Library.");
        }

        public static IEnumerable<Difficulty> EnumerateDifficulties()
        {
            InitializeDifficulties();
            return Difficulties;
        }

        public static Difficulty GetDifficulty(String Name)
        {
            InitializeDifficulties();
            return Difficulties.FirstOrDefault(d => Name == d.Name);
        }
    }
}
