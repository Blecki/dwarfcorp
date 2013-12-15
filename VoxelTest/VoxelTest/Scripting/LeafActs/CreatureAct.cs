using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class CreatureAct : Act
    {
        public CreatureAIComponent Agent { get; set; }

        [JsonIgnore]
        public Creature Creature
        {
            get { return Agent.Creature; }
        }


        public CreatureAct(CreatureAIComponent agent)
        {
            Agent = agent;
            Name = "Creature Act";
        }

        public  CreatureAct()
        {

        }
    }

}