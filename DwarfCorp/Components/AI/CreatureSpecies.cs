using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class CreatureSpecies 
    {
        public String Name;
        public int SpeciesLimit = 50;
        public String BabyType = "";
        public int PregnancyLengthHours = 24;
        public bool CanReproduce = false;
        public bool LaysEggs = false;
        public String BaseMeatResource = "Meat";
        public bool HasMeat = true;
        public bool CanSleep = false;
    }
}
