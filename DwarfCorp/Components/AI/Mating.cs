using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Mating
    {   
        public static bool CanMate(Creature A, Creature B)
        {
            return A.Stats.Gender != Gender.Nonbinary 
                && A.Stats.Gender != B.Stats.Gender 
                && A.Stats.CanReproduce
                && !A.IsPregnant 
                && B.Stats.CanReproduce 
                && A.Species == B.Species 
                && !B.IsPregnant;
        }

        public static void Mate(Creature A, Creature B, WorldTime time)
        {
            if (A.IsPregnant || B.IsPregnant) return;
            if (A.Stats.Gender == Gender.Nonbinary) return;
            // Can this be simplified? Is it even called if CanMate fails?
            if (A.Stats.Gender == Gender.Male) // Make sure A is the female.
            {
                var t = A;
                A = B;
                B = t;
            }

            A.CurrentPregnancy = new Pregnancy()
            {
                EndDate = time.CurrentDate + new TimeSpan(0, A.PregnancyLengthHours, 0, 0)
            };
        }

        public static string Pronoun(Gender Gender)
        {
            switch (Gender)
            {
                case Gender.Male:
                    return "he";
                case Gender.Female:
                    return "she";
                case Gender.Nonbinary:
                    return "they";
            }
            return "?";
        }

        public static string Posessive(Gender Gender)
        {
            switch (Gender)
            {
                case Gender.Male:
                    return "his";
                case Gender.Female:
                    return "her";
                case Gender.Nonbinary:
                    return "their";
            }

            return "?";
        }

        public static Gender RandomGender()
        {
            float num = MathFunctions.Rand(0.0f, 1.0f);
            if (num < 0.01f)
            {
                return Gender.Nonbinary;
            }
            else if (num < 0.505f)
            {
                return Gender.Male;
            }
            else
            {
                return Gender.Female;
            }
        }
    }
}
