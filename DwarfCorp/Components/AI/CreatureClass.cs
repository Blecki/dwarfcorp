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
        public TaskCategory Actions = TaskCategory.None;
        public CharacterMode AttackMode;
        public bool PlayerClass = false;
        public bool RequiresSupervision = true;
        public string FallbackTool = "";
        public string JobDescription = "There is no description for this class.";
        public bool RequiresTools = false;
        public bool TriggersMourning = true;
        public List<Resource> StartingEquipment = new List<Resource>();

        // Todo: Should just include name of attack animation. Kinda what the AttackMode is.

        public CreatureClass()
        {
        }
    }
}
