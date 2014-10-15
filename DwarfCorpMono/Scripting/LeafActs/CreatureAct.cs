using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class CreatureAct : Act
    {
        public CreatureAIComponent Agent { get; set; }
        public Creature Creature { get { return Agent.Creature; } }


        public CreatureAct(CreatureAIComponent agent)
        {
            Agent = agent;
            Name = "Creature Act";
        }


    }
}
