using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Goals
{
    public class EventLibrary
    {
        public List<ScheduledEvent> Events = new List<ScheduledEvent>();

        public EventLibrary()
        {
            Events = new List<ScheduledEvent>()
            {
                new SpawnEntityEvent()
                {
                    Name = "Spawn Kobold",
                    Difficulty = 5,
                    AnnouncementText = "A Kobold has come!",
                    AnnouncementDetails = "A Kobold has snuck into our colony. It will try to steal from us. Be on alert!",
                    EntityToSpawn = "Kobold",
                    EntityFaction = "Carnivore",
                    SpawnLocation = ScheduledEvent.EntitySpawnLocation.WorldEdge,
                    ZoomToEntity = true,
                    AnnouncementSound = ContentPaths.Audio.Oscar.sfx_gui_negative_generic,
                    PauseOnAnnouncementDetails = true,
                    CooldownHours = 2,
                    AllowedTime = ScheduledEvent.TimeRestriction.OnlyNightTime
                },
                new SpawnEntityEvent()
                {
                    Name = "Spawn Gremlin",
                    Difficulty = 20,
                    AnnouncementText = "A Gremlin has come!",
                    AnnouncementDetails = "A Gremlin has snuck into our colony. It will try to sabotage us. Be on alert!",
                    EntityToSpawn = "Gremlin",
                    EntityFaction = "Carnivore",
                    SpawnLocation = ScheduledEvent.EntitySpawnLocation.WorldEdge,
                    ZoomToEntity = true,
                    AnnouncementSound = ContentPaths.Audio.Oscar.sfx_gui_negative_generic,
                    PauseOnAnnouncementDetails = true,
                    CooldownHours = 2
                },
                new SpawnTradeEnvoyEvent()
                {
                    Name = "Send Trade Envoy",
                    Difficulty = -10,
                    Likelihood = 5,
                    PartyFactionFilter = new ScheduledEvent.FactionFilter()
                    {
                        Specification = ScheduledEvent.FactionSpecification.Random,
                        Hostility = ScheduledEvent.FactionHostilityFilter.NotHostile
                    },
                    CooldownHours = 8,
                    AllowedTime = ScheduledEvent.TimeRestriction.OnlyDayTime
                },
                new SpawnTradeEnvoyEvent()
                {
                    Name = "Demand Tribute",
                    Difficulty = 5,
                    Likelihood = 3,
                    TributeDemanded = 100,
                    PartyFactionFilter = new ScheduledEvent.FactionFilter()
                    {
                        Specification = ScheduledEvent.FactionSpecification.Random,
                        Hostility = ScheduledEvent.FactionHostilityFilter.Neutral,
                        Claim = ScheduledEvent.FactionClaimFilter.ClaimsTerritory
                    },
                    CooldownHours = 8,
                    AllowedTime = ScheduledEvent.TimeRestriction.OnlyDayTime
                },
                new SpawnWarPartyEvent()
                {
                    Name = "Send War Party",
                    Difficulty = 10,
                    Likelihood = 4,
                    PartyFactionFilter = new ScheduledEvent.FactionFilter()
                    {
                        Specification = ScheduledEvent.FactionSpecification.Random,
                        Hostility = ScheduledEvent.FactionHostilityFilter.Hostile
                    },
                    CooldownHours = 8
                },
                new PlagueEvent()
                {
                    Name = "Plague",
                    Difficulty = 20,
                    CooldownHours = 2
                },
            };

        }
    }
}
