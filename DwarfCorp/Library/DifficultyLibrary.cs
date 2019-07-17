using System.Collections.Generic;
using System;
using System.Linq;

namespace DwarfCorp
{
    public class Difficulty
    {
        public int Value = 0;
        public String Name = "Tranquil";
    }

    public static partial class Library
    {
        public static IEnumerable<Difficulty> EnumerateDifficulties()
        {
            yield return new Difficulty { Value = 0, Name = "Tranquil" };
            yield return new Difficulty { Value = 1, Name = "Easy" };
            yield return new Difficulty { Value = 2, Name = "Normal" };
            yield return new Difficulty { Value = 5, Name = "Hard" };
            yield return new Difficulty { Value = 10, Name = "What" };
        }

        public static int GetDifficulty(String Name)
        {
            return EnumerateDifficulties().FirstOrDefault(d => Name == d.Name).Value;
        }
    }
}
