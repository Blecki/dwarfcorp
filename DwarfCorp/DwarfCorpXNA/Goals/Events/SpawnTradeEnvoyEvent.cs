using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Goals
{
    public class SpawnTradeEnvoyEvent : ScheduledEvent
    {
        public string PartyFaction;
        public FactionFilter PartyFactionFilter;
        public DwarfBux TributeDemanded = 0m;

        public override void Trigger(WorldManager world)
        {
            var faction = GetFaction(world, PartyFaction, PartyFactionFilter);
            if (!String.IsNullOrEmpty(faction) && world.Factions.Factions.ContainsKey(faction))
            {
                var envoy = world.Diplomacy.SendTradeEnvoy(world.Factions.Factions[faction], world);
                if (envoy != null)
                {
                    envoy.TributeDemanded = (int)(TributeDemanded * world.InitialEmbark.Difficulty * MathFunctions.Rand(0.9f, 1.5f));
                }
            }
            base.Trigger(world);
        }
    }
}
