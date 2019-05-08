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
            public int Index; // Todo: Kill this.
            public string Name;
            public DwarfBux Pay;
            public int XP;
            public StatAdjustment BaseStats;
            public List<Weapon> ExtraAttacks = new List<Weapon>();
            public int HealingPower = 0;
        }

        public List<Weapon> Attacks { get; set; }
        public List<Level> Levels { get; set; }
        public string Name { get; set; }
        public Task.TaskCategory Actions = Task.TaskCategory.None;
        public CharacterMode AttackMode;
        public bool PlayerClass = false;

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
