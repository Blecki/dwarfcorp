using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Diagnostics;

namespace DwarfCorp
{
    public class Faction // Todo: Need to trim and refactor, see if can be split into normal faction / player faction.
    {
        public String ParentFactionName = "";
        [JsonIgnore] public OverworldFaction ParentFaction;

        public Company Economy { get; set; }
        public List<TradeEnvoy> TradeEnvoys = new List<TradeEnvoy>();
        public List<WarParty> WarParties = new List<WarParty>();
        public List<GameComponent> OwnedObjects = new List<GameComponent>();
        public List<CreatureAI> Minions = new List<CreatureAI>();
        public Timer HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
        public Dictionary<ulong, VoxelHandle> GuardedVoxels = new Dictionary<ulong, VoxelHandle>();
        public List<Creature> Threats = new List<Creature>();

        [JsonIgnore] public Race Race => Library.GetRace(ParentFaction.Race);
        [JsonIgnore] public WorldManager World;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = ctx.Context as WorldManager;
            ParentFaction = World.Overworld.Natives.FirstOrDefault(n => n.Name == ParentFactionName);

            Threats.RemoveAll(threat => threat == null || threat.IsDead);
            Minions.RemoveAll(minion => minion == null || minion.IsDead);
        }

        public Faction()
        {
        }

        public Faction(WorldManager World, OverworldFaction descriptor)
        {
            this.World = World;
            ParentFaction = descriptor;
            ParentFactionName = descriptor.Name;
        }

        public static List<CreatureAI> FilterMinionsWithCapability(List<CreatureAI> minions, TaskCategory action)
        {
            return minions.Where(creature => creature.Stats.IsTaskAllowed(action)).ToList();
        }

        public void Update(DwarfTime time)
        {
            Minions.RemoveAll(m => m.IsDead);


            if (HandleThreatsTimer == null)
                HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);

            HandleThreatsTimer.Update(time);
            if (HandleThreatsTimer.HasTriggered)
                HandleThreats();

            if (World.ComponentManager.NumComponents() > 0)
                OwnedObjects.RemoveAll(obj => obj.IsDead || obj.Parent == null || !obj.Manager.HasComponent(obj.GlobalID));

            #region Update Expeditions

            foreach (TradeEnvoy envoy in TradeEnvoys)
                envoy.Update(World);

            var hadFactions = TradeEnvoys.Count > 0;
            TradeEnvoys.RemoveAll(t => t == null || t.ShouldRemove);

            if (hadFactions && TradeEnvoys.Count == 0)
            {
                var music = World.Time.IsDay() ? "main_theme_day" : "main_theme_night";
                SoundManager.PlayMusic(music);
            }

            foreach (var party in WarParties)
                party.Update(World);

            WarParties.RemoveAll(w => w.ShouldRemove);

            #endregion
        }

        public CreatureAI GetNearestMinion(Vector3 location)
        {
            float closestDist = float.MaxValue;
            CreatureAI closestMinion = null;
            foreach (CreatureAI minion in Minions)
            {
                float dist = (minion.Position - location).LengthSquared();
                if (!(dist < closestDist)) continue;
                closestDist = dist;
                closestMinion = minion;
            }

            return closestMinion;
        }

        public void HandleThreats()
        {
            List<Task> tasks = new List<Task>();
            List<Creature> threatsToRemove = new List<Creature>();
            foreach (Creature threat in Threats)
            {
                if (threat != null && !threat.IsDead)
                {
//                    if (!Designations.IsDesignation(threat.Physics, DesignationType.Attack))
                    {
                        var g = new KillEntityTask(threat.Physics, KillEntityTask.KillType.Auto);
                        //Designations.AddEntityDesignation(threat.Physics, DesignationType.Attack, null, g);
                        tasks.Add(g);
                    }
  //                  else
                    {
                        //threatsToRemove.Add(threat);
                    }
                }
                else
                {
                    threatsToRemove.Add(threat);
                }
            }

            foreach (Creature threat in threatsToRemove)
                Threats.Remove(threat);

            TaskManager.AssignTasksGreedy(tasks, Minions);
        }

        public GameComponent FindNearestItemWithTags(string tag, Vector3 location, bool filterReserved, GameComponent queryObject)
        {
            GameComponent closestItem = null;
            float closestDist = float.MaxValue;

            if (OwnedObjects == null)
                return null;

            foreach (GameComponent i in OwnedObjects)
            {
                if (i == null) continue;
                if (i.IsDead) continue;
                if (i.IsReserved && filterReserved && i.ReservedFor != queryObject) continue;
                if (i.Tags == null || !(i.Tags.Any(t => tag == t))) continue;

                float d = (i.GlobalTransform.Translation - location).LengthSquared();
                if (!(d < closestDist)) continue;
                closestDist = d;
                closestItem = i;
            }

            return closestItem;
        }

        public void AddMinion(CreatureAI minion)
        {
            Minions.Add(minion);
        }

        public List<GameComponent> GenerateRandomSpawn(int numCreatures, Vector3 position)
        {
            if (Race.CreatureTypes.Count == 0)
            {
                return new List<GameComponent>();
            }

            List<GameComponent> toReturn = new List<GameComponent>();
            for (int i = 0; i < numCreatures; i++)
            {
                string creature = Race.CreatureTypes[MathFunctions.Random.Next(Race.CreatureTypes.Count)];
                Vector3 offset = MathFunctions.RandVector3Cube() * 2;

                var voxelUnder = VoxelHelpers.FindFirstVoxelBelowIncludingWater(new VoxelHandle(
                    World.ChunkManager, GlobalVoxelCoordinate.FromVector3(position + offset)));
                if (voxelUnder.IsValid)
                {
                    var body = EntityFactory.CreateEntity<GameComponent>(creature, voxelUnder.WorldPosition + new Vector3(0.5f, 1, 0.5f));
                    var ai = body.EnumerateAll().OfType<CreatureAI>().FirstOrDefault();

                    if (ai != null)
                    {
                        ai.Faction.Minions.Remove(ai);

                        Minions.Add(ai);
                        ai.Creature.Faction = this;
                    }

                    toReturn.Add(body);
                }
            }

            return toReturn;
        }

        public void AddMoney(DwarfBux money)
        {
            if (money == 0.0m)
                return;

            if (Economy != null)
                Economy.Funds += money;
        }

        public TradeEnvoy SendTradeEnvoy()
        {
            if (!World.EnumerateZones().Any(room => room is BalloonPort && room.IsBuilt))
            {
                World.MakeAnnouncement(String.Format("Trade envoy from {0} left: No balloon port!", ParentFaction.Name));
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.15f);
                return null;
            }

            TradeEnvoy envoy = null;

            var creatures = World.MonsterSpawner.Spawn(World.MonsterSpawner.GenerateSpawnEvent(this, World.PlayerFaction, MathFunctions.Random.Next(4) + 1, false));

            envoy = new TradeEnvoy(World.Time.CurrentDate)
            {
                Creatures = creatures,
                OtherFaction = World.PlayerFaction,
                ShouldRemove = false,
                OwnerFaction = this,
                TradeGoods = Race.GenerateTradeItems(World),
                TradeMoney = new DwarfBux((decimal)MathFunctions.Rand(150.0f, 550.0f))
            };

            if (Race.IsNative)
            {
                if (Economy == null)
                {
                    Economy = new Company(this, 1000.0m, new CompanyInformation()
                    {
                        Name = ParentFaction.Name
                    });
                }

                foreach (CreatureAI creature in envoy.Creatures)
                {
                    creature.Physics.AddChild(new ResourcePack(World.ComponentManager));
                    creature.Physics.AddChild(new Flag(World.ComponentManager, Vector3.Up * 0.5f + Vector3.Backward * 0.25f, Economy.Information));
                }
            }
            else
            {
                GameComponent balloon = null;

                var rooms = World.EnumerateZones().Where(room => room.Type.Name == "Balloon Port").ToList();

                if (rooms.Count != 0)
                {
                    Vector3 pos = rooms.First().GetBoundingBox().Center();
                    balloon = Balloon.CreateBalloon(pos + new Vector3(0, 1000, 0), pos + Vector3.UnitY * 15, World.ComponentManager, this);
                }

                if (balloon != null)
                {
                    foreach (CreatureAI creature in creatures)
                    {
                        Matrix tf = creature.Physics.LocalTransform;
                        tf.Translation = balloon.LocalTransform.Translation;
                        creature.Physics.LocalTransform = tf;
                    }
                }
                else
                {
                    if (Economy == null)
                    {
                        Economy = new Company(this, 1000.0m, new CompanyInformation()
                        {
                            Name = ParentFaction.Name
                        });
                    }

                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                        creature.Physics.AddChild(new ResourcePack(World.ComponentManager));
                        creature.Physics.AddChild(new Flag(World.ComponentManager, Vector3.Up * 0.5f + Vector3.Backward * 0.25f, Economy.Information));
                    }
                }
            }

            foreach (CreatureAI creature in envoy.Creatures)
                creature.Physics.AddChild(new ResourcePack(World.ComponentManager));

            envoy.DistributeGoods();
            TradeEnvoys.Add(envoy);

            World.MakeAnnouncement(new DwarfCorp.Gui.Widgets.QueuedAnnouncement
            {
                Text = String.Format("Trade envoy from {0} has arrived!", ParentFaction.Name),
                ClickAction = (gui, sender) =>
                {
                    if (envoy.Creatures.Count > 0)
                    {
                        envoy.Creatures.First().ZoomToMe();
                        World.UserInterface.MakeWorldPopup(String.Format("Traders from {0} ({1}) have entered our territory.\nThey will try to get to our balloon port to trade with us.", ParentFaction.Name, Race.Name),
                            envoy.Creatures.First().Physics, -10);
                    }
                },
                ShouldKeep = () =>
                {
                    return envoy.ExpiditionState == Expedition.State.Arriving;
                }
            });

            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);

            World.Tutorial("trade");
            if (!String.IsNullOrEmpty(Race.TradeMusic))
                SoundManager.PlayMusic(Race.TradeMusic);

            return envoy;
        }

        public WarParty SendWarParty()
        {
            World.Tutorial("war");
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
            Politics politics = World.Overworld.GetPolitics(ParentFaction, World.PlayerFaction.ParentFaction);
            politics.IsAtWar = true;
            List<CreatureAI> creatures = World.MonsterSpawner.Spawn(World.MonsterSpawner.GenerateSpawnEvent(this, World.PlayerFaction, MathFunctions.Random.Next(World.Overworld.Difficulty) + 1, false));
            var party = new WarParty(World.Time.CurrentDate)
            {
                Creatures = creatures,
                OtherFaction = World.PlayerFaction,
                ShouldRemove = false,
                OwnerFaction = this
            };
            WarParties.Add(party);

            World.MakeAnnouncement(new Gui.Widgets.QueuedAnnouncement()
            {
                Text = String.Format("A war party from {0} has arrived!", ParentFaction.Name),
                SecondsVisible = 60,
                ClickAction = (gui, sender) =>
                {
                    if (party.Creatures.Count > 0)
                    {
                        party.Creatures.First().ZoomToMe();
                        World.UserInterface.MakeWorldPopup(String.Format("Warriors from {0} ({1}) have entered our territory. They will prepare for a while and then attack us.", ParentFaction.Name, Race.Name), party.Creatures.First().Physics, -10);
                    }
                },
                ShouldKeep = () =>
                {
                    return party.ExpiditionState == Expedition.State.Arriving;
                }
            });

            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.15f);

            foreach (var creature in creatures)
            {
                if (Economy == null)
                    Economy = new Company(this, (decimal)MathFunctions.Rand(1000, 9999), null);

                if (Economy.Information == null)
                    Economy.Information = new CompanyInformation();

                creature.Physics.AddChild(new Flag(World.ComponentManager, Vector3.Up * 0.5f + Vector3.Backward * 0.25f, Economy.Information));
            }
            return party;
        }

    }
}
