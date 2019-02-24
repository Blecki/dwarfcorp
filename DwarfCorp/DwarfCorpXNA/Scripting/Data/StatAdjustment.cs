using System;
using System.IO;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class StatAdjustment
    {
        public String Name = "";

        public float Dexterity = 0;
        public float Constitution = 0;
        public float Strength = 0;
        public float Wisdom = 0;
        public float Charisma = 0;
        public float Intelligence = 0;
        public float Size = 0;

        public StatAdjustment()
        {

        }

        public StatAdjustment(int constant)
        {
            Dexterity = constant;
            Constitution = constant;
            Strength = constant;
            Wisdom = constant;
            Charisma = constant;
            Intelligence = constant;
            Size = constant;
        }

        public StatAdjustment Clone()
        {
            return new StatAdjustment
            {
                Name = Name,
                Dexterity = Dexterity,
                Constitution = Constitution,
                Strength = Strength,
                Wisdom = Wisdom,
                Charisma = Charisma,
                Intelligence = Intelligence,
                Size = Size
            };
        }

        public static StatAdjustment operator +(StatAdjustment a, StatAdjustment b)
        {
            if (a == null || b == null) return null;

            return new StatAdjustment()
            {
                Charisma = a.Charisma + b.Charisma,
                Constitution = a.Charisma + b.Charisma,
                Dexterity = a.Dexterity + b.Dexterity,
                Intelligence = a.Intelligence + b.Intelligence,
                Size = a.Size + b.Size,
                Strength = a.Strength + b.Strength,
                Wisdom = a.Wisdom + b.Wisdom
            };
        }

        public static StatAdjustment operator -(StatAdjustment a, StatAdjustment b)
        {
            if (a == null || b == null) return null;

            return new StatAdjustment()
            {
                Charisma = a.Charisma - b.Charisma,
                Constitution = a.Charisma - b.Charisma,
                Dexterity = a.Dexterity - b.Dexterity,
                Intelligence = a.Intelligence - b.Intelligence,
                Size = a.Size - b.Size,
                Strength = a.Strength - b.Strength,
                Wisdom = a.Wisdom - b.Wisdom
            };
        }

        public void Reset()
        {
            Charisma = 0;
            Constitution = 0;
            Dexterity = 0;
            Intelligence = 0;
            Size = 0;
            Strength = 0;
            Wisdom = 0;
        }
    }
}
