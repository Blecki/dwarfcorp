using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Events
{
    public class SpawnWarPartyEvent : ScheduledEvent
    {
        public string PartyFaction;
        public FactionFilter PartyFactionFilter;

        public override void Trigger(WorldManager World)
        {
            if (World.Overworld.Difficulty == 0) return;

            var faction = GetFaction(World, PartyFaction, PartyFactionFilter);
            if (!String.IsNullOrEmpty(faction) && World.Factions.Factions.ContainsKey(faction))
                World.Factions.Factions[faction].SendWarParty();

            base.Trigger(World);
        }
    }
}
