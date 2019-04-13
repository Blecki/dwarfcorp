using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Goals
{
    public class SpawnWarPartyEvent : ScheduledEvent
    {
        public string PartyFaction;
        public FactionFilter PartyFactionFilter;

        public override void Trigger(WorldManager world)
        {
            if (world.InitialEmbark.Difficulty == 0) return;

            var faction = GetFaction(world, PartyFaction, PartyFactionFilter);
            if (!String.IsNullOrEmpty(faction) && world.Factions.Factions.ContainsKey(faction))
            {
                world.Diplomacy.SendWarParty(world.Factions.Factions[faction]);
            }
            base.Trigger(world);
        }
    }
}
