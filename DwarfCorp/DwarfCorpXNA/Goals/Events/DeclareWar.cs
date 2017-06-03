using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Events
{
    public class DeclareWar : GameEvent
    {
        public Faction PlayerFaction;
        public Faction OtherFaction;
    }
}
