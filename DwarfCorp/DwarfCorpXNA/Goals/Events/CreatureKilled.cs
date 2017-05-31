using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Events
{
    public class CreatureKilled : GameEvent
    {
        public Creature Agressor;
        public Creature Victim;
    }
}
