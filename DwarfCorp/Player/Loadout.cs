using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class Loadout 
    {
        public string Name;
        public bool StartAsManager = false;
        public TaskCategory Actions = TaskCategory.None;
        public string Description = "There is no description for this class.";
        public List<Resource> StartingEquipment = new List<Resource>();
        public StatAdjustment StartingStats = new StatAdjustment { Charisma = 3, Constitution = 3, Dexterity = 3, Intelligence = 3, Strength = 3, Wisdom = 3 };


        public Loadout()
        {
        }
    }
}
