using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Events
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
                var envoy = world.Factions.Factions[faction].SendTradeEnvoy();
                if (envoy != null)
                    envoy.TributeDemanded = (int)(TributeDemanded * world.Overworld.Difficulty.CombatModifier * MathFunctions.Rand(0.9f, 1.5f));
            }
            base.Trigger(world);
        }
    }
}
