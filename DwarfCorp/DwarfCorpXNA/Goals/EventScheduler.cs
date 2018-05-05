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

    [JsonObject(IsReference = true)]
    public struct ScheduledEvent
    {
        public string Name;
        public float Difficulty;
        public string EntityToSpawn;
        public string EntityFaction;
        public int CooldownHours;
        public bool RestrictToNightTime;
        public bool RestrictToDayTime;
        public bool GiveDisease;
        public enum FactionFilter
        {
            None,
            RandomHostile,
            RandomAlly,
            RandomNeutral,
            Random,
            RandomNonHostile,
            Specific
        }

        public FactionFilter EntityFactionFilter;

        public enum EntitySpawnLocation
        {
            BalloonPort,
            RandomZone,
            WorldEdge
        }
        public EntitySpawnLocation SpawnLocation;
        public string AnnouncementText;
        public string AnnouncementDetails;
        public string AnnouncementSound;
        public bool PauseOnAnnouncementDetails;
        public bool ZoomToEntity;

        public bool CreateTradeEnvoy;
        public bool CreateWarParty;

        [JsonIgnore]
        public DateTime Date;

        public void Activate(WorldManager world)
        {
            Body entity = null;
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

            string faction = EntityFaction;
            switch (EntityFactionFilter)
            {
                case FactionFilter.None:
                case FactionFilter.Specific:
                    break;
                case FactionFilter.Random:
                    faction = Datastructures.SelectRandom(world.Factions.Factions.Where(f => f.Value.Race.IsIntelligent && !f.Value.IsRaceFaction && f.Value != world.PlayerFaction)).Value.Name;
                    break;
                case FactionFilter.RandomAlly:
                    faction = Datastructures.SelectRandom(world.Factions.Factions.Where(f => f.Value.Race.IsIntelligent &&
                        f.Value != world.PlayerFaction &&
                        world.Diplomacy.GetPolitics(f.Value, world.PlayerFaction).GetCurrentRelationship() == Relationship.Loving)).Value.Name;
                    break;
                case FactionFilter.RandomHostile:
                    faction = Datastructures.SelectRandom(world.Factions.Factions.Where(f => f.Value.Race.IsIntelligent && !f.Value.IsRaceFaction && 
                                                                                        f.Value != world.PlayerFaction &&
                                                                                        world.Diplomacy.GetPolitics(f.Value, world.PlayerFaction).GetCurrentRelationship() == Relationship.Hateful)).Value.Name;
                    break;
                case FactionFilter.RandomNeutral:
                    faction = Datastructures.SelectRandom(world.Factions.Factions.Where(f => f.Value.Race.IsIntelligent && !f.Value.IsRaceFaction &&
                                                                f.Value != world.PlayerFaction &&
                                                                world.Diplomacy.GetPolitics(f.Value, world.PlayerFaction).GetCurrentRelationship() == Relationship.Indifferent)).Value.Name;
                    break;

                case FactionFilter.RandomNonHostile:
                    faction = Datastructures.SelectRandom(world.Factions.Factions.Where(f => f.Value.Race.IsIntelligent && !f.Value.IsRaceFaction &&
                                                                f.Value != world.PlayerFaction &&
                                                                world.Diplomacy.GetPolitics(f.Value, world.PlayerFaction).GetCurrentRelationship() != Relationship.Hateful)).Value.Name;
                    break;
            }

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
                    }
                }
            }

            if (validFaction)
            {
                if (CreateTradeEnvoy)
                {
                    world.Diplomacy.SendTradeEnvoy(world.Factions.Factions[faction], world);
                }

                if (CreateWarParty)
                {
                    world.Diplomacy.SendWarParty(world.Factions.Factions[faction]);
                }
            }

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

                    if (scheduledEvent.ZoomToEntity && entity != null)
                    {
                        world.Camera.ZoomTo(entity.Position);
                    }

                }, keep);
            }

            if (GiveDisease)
            {
                DiseaseLibrary.SpreadRandomDiseases(world.PlayerFaction.Minions);
            }
        }
    }

    public class EventLibrary
    {
        public List<ScheduledEvent> Events = new List<ScheduledEvent>();

        public EventLibrary()
        {
            Events = new List<ScheduledEvent>()
            {
                new ScheduledEvent()
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
                    RestrictToNightTime = true
                },
                new ScheduledEvent()
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
                new ScheduledEvent()
                {
                    Name = "Send Trade Envoy",
                    Difficulty = -10,
                    EntityFactionFilter = ScheduledEvent.FactionFilter.RandomNonHostile,
                    SpawnLocation = ScheduledEvent.EntitySpawnLocation.WorldEdge,
                    CreateTradeEnvoy = true,
                    CooldownHours = 8,
                    RestrictToDayTime = true
                },
                new ScheduledEvent()
                {
                    Name = "Send War Party",
                    Difficulty = 10,
                    EntityFactionFilter = ScheduledEvent.FactionFilter.RandomHostile,
                    SpawnLocation = ScheduledEvent.EntitySpawnLocation.WorldEdge,
                    CreateWarParty = true,
                    CooldownHours = 8
                },
                new ScheduledEvent()
                {
                    Name = "Plague",
                    Difficulty = 20,
                    GiveDisease = true,
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
        public List<ScheduledEvent> Forecast = new List<ScheduledEvent>();
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
                ScheduledEvent currentEvent = Forecast[0];
                Forecast.RemoveAt(0);
                CurrentDifficulty += currentEvent.Difficulty;
                CurrentDifficulty = Math.Max(CurrentDifficulty, 0);
                currentEvent.Activate(world);
            }
        }

        public float ForecastDifficulty(DateTime now)
        {
            float difficulty = CurrentDifficulty;
            DateTime curr = now;
            foreach (var e in Forecast)
            {
                var duration = e.Date - curr;
                difficulty = Math.Max((float)(difficulty - DifficultyDecayPerHour * duration.TotalHours), 0);
                difficulty += e.Difficulty;
                difficulty = Math.Max(difficulty, 0);
                curr = e.Date;
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
            var filteredEvents = Forecast.Count == 0 ? Events.Events : Events.Events.Where(e => e.Name != Forecast.Last().Name);
            while (!foundEvent && iters < 100)
            {
                iters++;
                randomEvent = Datastructures.SelectRandom(filteredEvents);
                if (forecast + randomEvent.Difficulty > TargetDifficulty || forecast + randomEvent.Difficulty < 0)
                {
                    continue;
                }
                foundEvent = true;
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
                randomTime = Forecast.Last().Date + new TimeSpan(MathFunctions.RandInt(MinSpacingHours, MaxSpacingHours) + Forecast.Last().CooldownHours, 0, 0);
            }

            if (randomEvent.RestrictToDayTime)
            {
                while (IsNight(randomTime))
                {
                    randomTime += new TimeSpan(1, 0, 0);
                }
            }
            else if (randomEvent.RestrictToNightTime)
            {
                while (!IsNight(randomTime))
                {
                    randomTime += new TimeSpan(1, 0, 0);
                }
            }

            randomEvent.Date = randomTime;
            Forecast.Add(randomEvent);
        }

        public void Update(WorldManager world, DateTime now)
        {
            int hour = now.Hour;
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
