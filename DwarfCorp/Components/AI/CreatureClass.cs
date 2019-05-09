using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class CreatureClass 
    {
        public class Level
        {
            public string Name;
            public DwarfBux Pay;
            public int XP;
            public StatAdjustment BaseStats;
            public List<Weapon> ExtraWeapons = new List<Weapon>();
            public int HealingPower = 0;
        }

        public List<Weapon> Weapons;
        public List<Level> Levels;
        public string Name;
        public Task.TaskCategory Actions = Task.TaskCategory.None;
        public CharacterMode AttackMode;
        public bool PlayerClass = false;

        public int SpeciesLimit = 50;
        public String BabyType = "";
        public int PregnancyLengthHours = 24;
        public bool CanReproduce = false;
        public bool LaysEggs = false;

        // Todo: Should just include name of attack animation. Kinda what the AttackMode is.

        public bool IsTaskAllowed(Task.TaskCategory TaskCategory)
        {
            return (Actions & TaskCategory) == TaskCategory;
        }

        public CreatureClass()
        {
        }
    }
}
