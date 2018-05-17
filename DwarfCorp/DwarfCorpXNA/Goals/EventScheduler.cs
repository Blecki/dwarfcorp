using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Goals
{
    public class TimedIndicatorWidget : Gui.Widget
    {
        public Timer DeathTimer = new Timer(30.0f, true, Timer.TimerMode.Real);
        public Func<bool> ShouldKeep = null;
        public override void Construct()
        {
            var font = Root.GetTileSheet("font10") as Gui.VariableWidthFont;
            var size = font.MeasureString(Text, 256);
            // TODO (mklingensmith) why do I need this padding?
            size.X = (int)(size.X * 1.25f);
            size.Y = (int)(size.Y * 1.75f);
            Font = "font10";
            TextColor = new Microsoft.Xna.Framework.Vector4(1, 1, 1, 1);
            Border = "border-dark";
            Rect = new Microsoft.Xna.Framework.Rectangle(0, 0, size.X, size.Y);
            TextVerticalAlign = Gui.VerticalAlign.Center;
            TextHorizontalAlign = Gui.HorizontalAlign.Center;
            OnUpdate = (sender, time) =>
            {
                Update(DwarfTime.LastTime);
            };
            if (OnClick == null)
            {
                OnClick = (sender, args) =>
                {
                    Root.DestroyWidget(this);
                };
            }
            HoverTextColor = Microsoft.Xna.Framework.Color.LightGoldenrodYellow.ToVector4();
            ChangeColorOnHover = true;
            Root.RegisterForUpdate(this);
            base.Construct();
        }

        public void Update(DwarfTime time)
        {
            if (ShouldKeep == null)
            {
                DeathTimer.Update(time);
                if (DeathTimer.HasTriggered)
                {
                    Root.DestroyWidget(this);
                }
            }
            else if(!ShouldKeep())
            {
                Root.DestroyWidget(this);
            }
        }
    }

    public class ScheduledEvent
    {
        public int Likelihood = 1;
        public string Name;
        public float Difficulty = 0.0f;
        public enum TimeRestriction
        {
            All,
            OnlyDayTime,
            OnlyNightTime,
        }
        public TimeRestriction AllowedTime = TimeRestriction.All;
        public enum FactionHostilityFilter
        {
            Any,
            Hostile,
            Neutral,
            Ally,
            NotHostile,
            NotAlly
        }

        public enum FactionClaimFilter
        {
            Any,
            ClaimsTerritory,
            DoesNotClaimTerritory
        }

        public enum FactionSpecification
        {
            Specific,
            Random,
            Player,
            Motherland,
        }

        public struct FactionFilter
        {
            public FactionHostilityFilter Hostility;
            public FactionSpecification Specification;
            public FactionClaimFilter Claim;
        }

        public enum EntitySpawnLocation
        {
            BalloonPort,
            RandomZone,
            WorldEdge
        }

        public int CooldownHours = 0;
        public string AnnouncementText;
        public string AnnouncementDetails;
        public string AnnouncementSound;
        public bool PauseOnAnnouncementDetails;

        protected void Announce(WorldManager world, Body entity, bool zoomToEntity)
        {
            if (!String.IsNullOrEmpty(AnnouncementSound))
            {
                SoundManager.PlaySound(AnnouncementSound, 0.2f);
            }

            if (!String.IsNullOrEmpty(AnnouncementText))
            {
                Func<bool> keep = null;
                if (entity != null)
                {
                    keep = () => !entity.IsDead;
                }
                ScheduledEvent scheduledEvent = this;
                world.MakeAnnouncement(AnnouncementText, (sender, args) =>
                {
                    if (!String.IsNullOrEmpty(scheduledEvent.AnnouncementDetails))
                    {
                        if (entity == null)
                        {
                            world.Gui.ShowModalPopup(new Gui.Widgets.Confirm()
                            {
                                Text = scheduledEvent.AnnouncementDetails
                            });
                        }
                        else
                        {
                            world.MakeWorldPopup(scheduledEvent.AnnouncementDetails,
                                                 entity, -10);
                        }
                    }

                    if (scheduledEvent.PauseOnAnnouncementDetails)
                    {
                        world.Paused = true;
                    }

                    if (zoomToEntity && entity != null)
                    {
                        world.Camera.ZoomTo(entity.Position);
                    }

                }, keep);
            }
        }


        private bool CanSpawnFaction(WorldManager world, Faction faction, string EntityFaction, FactionFilter filter)
        {
            switch (filter.Specification)
            {
                case FactionSpecification.Motherland:
                    return faction.IsMotherland;

                case FactionSpecification.Player:
                    return faction == world.PlayerFaction;

                case FactionSpecification.Random:
                    switch (filter.Claim)
                    {
                        case FactionClaimFilter.ClaimsTerritory:
                            if (!faction.ClaimsColony)
                            {
                                return false;
                            }
                            break;
                        case FactionClaimFilter.DoesNotClaimTerritory:
                            if (faction.ClaimsColony)
                            {
                                return false;
                            }
                            break;
                        case FactionClaimFilter.Any:
                            break;
                    }
                    var relationship = world.Diplomacy.GetPolitics(faction, world.PlayerFaction).GetCurrentRelationship();
                    switch (filter.Hostility)
                    {
                        case FactionHostilityFilter.Ally:
                            return relationship == Relationship.Loving;
                        case FactionHostilityFilter.Neutral:
                            return relationship == Relationship.Indifferent;
                        case FactionHostilityFilter.Hostile:
                            return relationship == Relationship.Hateful;
                        case FactionHostilityFilter.NotHostile:
                            return relationship != Relationship.Hateful;
                        case FactionHostilityFilter.NotAlly:
                            return relationship != Relationship.Loving;
                        case FactionHostilityFilter.Any:
                            return true;
                    }
                    return true;

                case FactionSpecification.Specific:
                    return faction.Name == EntityFaction;
            }
            return false;
        }

        public string GetFaction(WorldManager world, string EntityFaction, FactionFilter EntityFactionFilter)
        {
            var factions = world.Factions.Factions.Where(f => f.Value.Race.IsIntelligent && !f.Value.IsRaceFaction &&
                CanSpawnFaction(world, f.Value, EntityFaction, EntityFactionFilter)).Select(f => f.Value).ToList();
            factions.Sort((f1, f2) => f1.DistanceToCapital.CompareTo(f2.DistanceToCapital));

            if (factions.Count == 0)
                return EntityFaction;

            float sumDistance = factions.Sum(f => 1.0f / (f.DistanceToCapital + 1.0f));
            float randPick = MathFunctions.Rand(0, sumDistance);

            float dist = 0;
            foreach(var faction in factions)
            {
                dist += 1.0f / (1.0f + faction.DistanceToCapital);
                if (randPick < dist)
                {
                    return faction.Name;
                }
            }

            return EntityFaction;
        }

        public Microsoft.Xna.Framework.Vector3 GetSpawnLocation(WorldManager world, EntitySpawnLocation SpawnLocation)
        {
            Microsoft.Xna.Framework.Vector3 location = location = VoxelHelpers.FindFirstVoxelBelowIncludeWater(new VoxelHandle(world.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(MonsterSpawner.GetRandomWorldEdge(world)))).WorldPosition + Microsoft.Xna.Framework.Vector3.Up * 1.5f;
            switch (SpawnLocation)
            {
                case EntitySpawnLocation.BalloonPort:
                    {
                        var balloonport = world.PlayerFaction.GetRooms().OfType<BalloonPort>();
                        if (balloonport.Any())
                        {
                            location = Datastructures.SelectRandom(balloonport).GetBoundingBox().Center() + Microsoft.Xna.Framework.Vector3.Up * 1.5f;
                        }
                        break;
                    }
                case EntitySpawnLocation.RandomZone:
                    {
                        var zones = world.PlayerFaction.GetRooms();
                        if (zones.Any())
                        {
                            location = Datastructures.SelectRandom(zones).GetBoundingBox().Center() + Microsoft.Xna.Framework.Vector3.Up * 1.5f;
                        }
                        break;
                    }
                case EntitySpawnLocation.WorldEdge:
                    {
                        // already computed
                        break;
                    }
            }

            return location;
        }

        /// <summary>
        /// Called when the event is first triggered.
        /// </summary>
        public virtual void Trigger(WorldManager world)
        {
            Announce(world, null, false);
        }

        /// <summary>
        /// Called continuously while the event is active (ShouldKeep() returns true)
        /// </summary>
        public virtual void Update(WorldManager world)
        {

        }

        /// <summary>
        /// Gets called when ShouldKeep() returns false
        /// </summary>
        public virtual void Deactivate(WorldManager world)
        {

        }

        /// <summary>
        /// Whether or not the event should stick around in the event manager and get updated.
        /// Deactivate will get called once this returns false.
        /// </summary>
        public virtual bool ShouldKeep(WorldManager world)
        {
            return false;
        }
    }

    public class SpawnEntityEvent : ScheduledEvent
    {
        public string EntityToSpawn;
        public string EntityFaction;
        public bool ZoomToEntity;
        public EntitySpawnLocation SpawnLocation;
        public FactionFilter EntityFactionFilter;
        public int MinEntityLevel = 1;
        public int MaxEntityLevel = 1;

        public SpawnEntityEvent()
        {

        }

        public override void Trigger(WorldManager world)
        {
            Body entity = null;
            Microsoft.Xna.Framework.Vector3 location = GetSpawnLocation(world, SpawnLocation);

            string faction = GetFaction(world, EntityFaction, EntityFactionFilter);

            bool validFaction = (!String.IsNullOrEmpty(faction) && world.Factions.Factions.ContainsKey(faction));

            if (!String.IsNullOrEmpty(EntityToSpawn))
            {
                entity = EntityFactory.CreateEntity<Body>(EntityToSpawn, location);
                if (validFaction)
                {
                    var creatures = entity.EnumerateAll().OfType<CreatureAI>();
                    foreach (var creature in creatures)
                    {
                        if (creature.Faction != null)
                        {
                            creature.Faction.Minions.Remove(creature);
                        }

                        creature.Faction = world.Factions.Factions[faction];
                        creature.Stats.LevelIndex = MathFunctions.RandInt(MinEntityLevel, MaxEntityLevel);
                    }
                }
            }

            Announce(world, entity, ZoomToEntity);
        }
    }

    public class SpawnWarPartyEvent : ScheduledEvent
    {
        public string PartyFaction;
        public FactionFilter PartyFactionFilter;

        public override void Trigger(WorldManager world)
        {
            var faction = GetFaction(world, PartyFaction, PartyFactionFilter);
            if (!String.IsNullOrEmpty(faction) && world.Factions.Factions.ContainsKey(faction))
            {
                world.Diplomacy.SendWarParty(world.Factions.Factions[faction]);
            }
            base.Trigger(world);
        }
    }

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
                envoy.TributeDemanded = (int)(TributeDemanded * world.InitialEmbark.Difficulty * MathFunctions.Rand(0.9f, 1.5f));
            }
            base.Trigger(world);
        }
    }

    public class PlagueEvent : ScheduledEvent
    {
        public override void Trigger(WorldManager world)
        {
            DiseaseLibrary.SpreadRandomDiseases(world.PlayerFaction.Minions);
            base.Trigger(world);
        }
    }

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


    [JsonObject(IsReference = true)]
    public class EventScheduler
    {
        public float CurrentDifficulty = 5;
        public float TargetDifficulty = 30;
        public float DifficultyDecayPerHour = 0.5f;
        public int MaxForecast = 10;
        public int MinSpacingHours = 1;
        public int MaxSpacingHours = 4;
        public int MinimumStartTime = 8;
        public struct EventEntry
        {
            public ScheduledEvent Event;
            public DateTime Date;
        }

        public List<EventEntry> Forecast = new List<EventEntry>();
        public List<ScheduledEvent> ActiveEvents = new List<ScheduledEvent>();
        public EventLibrary Events = new EventLibrary();
        private int previousHour = -1;

        [JsonIgnore]
        public WorldManager World;

        [OnDeserialized]
        void OnDeserializing(StreamingContext context)
        {
            // Assume the context passed in is a WorldManager
            World = ((WorldManager)context.Context);
        }

        public EventScheduler()
        {

        }

        private void PopEvent(WorldManager world)
        {
            if (Forecast.Count > 0)
            {
                ScheduledEvent currentEvent = Forecast[0].Event;
                Forecast.RemoveAt(0);
                ActivateEvent(world, currentEvent);
            }
        }

        public void ActivateEvent(WorldManager world, ScheduledEvent currentEvent)
        {
            CurrentDifficulty += currentEvent.Difficulty;
            CurrentDifficulty = Math.Max(CurrentDifficulty, 0);
            currentEvent.Trigger(world);
            ActiveEvents.Add(currentEvent);
        }

        public float ForecastDifficulty(DateTime now)
        {
            float difficulty = CurrentDifficulty;
            DateTime curr = now;
            foreach (var entry in Forecast)
            {
                var e = entry.Event;
                var duration = entry.Date - curr;
                difficulty = Math.Max((float)(difficulty - DifficultyDecayPerHour * duration.TotalHours), 0);
                difficulty += e.Difficulty;
                difficulty = Math.Max(difficulty, 0);
                curr = entry.Date;
            }
            return difficulty;
        }
        
        private bool IsNight(DateTime time)
        {
            return time.Hour < 6 || time.Hour > 20;
        }

        public void AddRandomEvent(DateTime now)
        {
            float forecast = ForecastDifficulty(now);
            bool foundEvent = false;
            var randomEvent = new ScheduledEvent();
            int iters = 0;
            var filteredEvents = Forecast.Count == 0 ? Events.Events : Events.Events.Where(e => e.Name != Forecast.Last().Event.Name).ToList();
            while (!foundEvent && iters < 100)
            {
                iters++;
                float sumLikelihood = filteredEvents.Sum(ev => ev.Likelihood);
                float randLikelihood = MathFunctions.Rand(0, sumLikelihood);
                float p = 0;
                foreach (var ev in filteredEvents)
                {
                    if (forecast + ev.Difficulty > TargetDifficulty || forecast + ev.Difficulty < 0)
                    {
                        continue;
                    }
                    p += ev.Likelihood;
                    if (randLikelihood < p)
                    {
                        randomEvent = ev;
                        foundEvent = true;
                    }
                }
            }

            if (!foundEvent)
            {
                return;
            }
            DateTime randomTime = now;
            if (Forecast.Count == 0)
            {
                randomTime = now + new TimeSpan(MinimumStartTime + MathFunctions.RandInt(MinSpacingHours, MaxSpacingHours), 0, 0);
            }
            else
            {
                randomTime = Forecast.Last().Date + new TimeSpan(MathFunctions.RandInt(MinSpacingHours, MaxSpacingHours) + Forecast.Last().Event.CooldownHours, 0, 0);
            }

            if (randomEvent.AllowedTime == ScheduledEvent.TimeRestriction.OnlyDayTime)
            {
                while (IsNight(randomTime))
                {
                    randomTime += new TimeSpan(1, 0, 0);
                }
            }
            else if (randomEvent.AllowedTime == ScheduledEvent.TimeRestriction.OnlyNightTime)
            {
                while (!IsNight(randomTime))
                {
                    randomTime += new TimeSpan(1, 0, 0);
                }
            }
            Forecast.Add(new EventEntry()
            {
                Event = randomEvent,
                Date = randomTime
            });

        }

        public void Update(WorldManager world, DateTime now)
        {
            int hour = now.Hour;

            foreach (var e in ActiveEvents)
            {
                if (e.ShouldKeep(world))
                {
                    e.Update(world);
                }
                else
                {
                    e.Deactivate(world);
                }
            }
            ActiveEvents.RemoveAll(e => !e.ShouldKeep(world));
            if (hour == previousHour)
             return;
            previousHour = hour;
            CurrentDifficulty = Math.Max(CurrentDifficulty - DifficultyDecayPerHour, 0);

            if (Forecast.Count > 0 && now > Forecast[0].Date)
            {
                PopEvent(world);
            }

            int iters = 0;
            while (Forecast.Count < MaxForecast && iters < MaxForecast * 2)
            {
                iters++;
                AddRandomEvent(now);
            }
        }
    }
}
