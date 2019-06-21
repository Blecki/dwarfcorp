using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class Expedition
    {
        public enum State
        {
            Leaving,
            Arriving,
            Fighting,
            Trading
        }

        public DateTimer DeathTimer;
        public List<CreatureAI> Creatures = new List<CreatureAI>();
        public Faction OwnerFaction;
        public Faction OtherFaction;
        public State ExpiditionState = State.Arriving;
        public bool ShouldRemove = false;

        public Expedition()
        {

        }

        public Expedition(DateTime Date)
        {
            DeathTimer = new DateTimer(Date, new TimeSpan(1, 12, 0, 0));
        }
    }
}