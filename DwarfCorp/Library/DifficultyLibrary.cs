using System.Collections.Generic;
using System;
using System.Linq;

namespace DwarfCorp
{
    public class Difficulty
    {
        public int Value = 0;
        public String Name = "Tranquil";
        public DwarfBux StartingFunds;
    }

    public static partial class Library
    {
        public static IEnumerable<Difficulty> EnumerateDifficulties()
        {
            yield return new Difficulty { Value = 0, Name = "Tranquil", StartingFunds = 5000 };
            yield return new Difficulty { Value = 1, Name = "Easy", StartingFunds = 5000 };
            yield return new Difficulty { Value = 2, Name = "Normal", StartingFunds = 2000 };
            yield return new Difficulty { Value = 5, Name = "Hard", StartingFunds = 1000 };
            yield return new Difficulty { Value = 10, Name = "What", StartingFunds = 100 };
        }

        public static Difficulty GetDifficulty(String Name)
        {
            return EnumerateDifficulties().FirstOrDefault(d => Name == d.Name);
        }
    }
}
