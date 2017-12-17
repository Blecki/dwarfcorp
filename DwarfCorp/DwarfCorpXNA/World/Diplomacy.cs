// Diplomacy.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Diplomacy
    {
        public class PoliticalEvent
        {
            public float Change { get; set; }
            public string Description { get; set; }
            public TimeSpan Duration { get; set; }
            public DateTime Time { get; set; }
        }

        public class Politics
        {
            public Faction Faction { get; set; }
            public List<PoliticalEvent> RecentEvents { get; set; }
            public bool HasMet { get; set; }
            public bool WasAtWar { get; set; }
            public TimeSpan DistanceToCapital { get; set; }
            public DateTimer WarPartyTimer { get; set; }
            public DateTimer TradePartyTimer { get; set; }

            public Politics()
            {
                
            }

            public Politics(DateTime currentDate, TimeSpan distanceToCapital)
            {
                DistanceToCapital = distanceToCapital;
                WasAtWar = false;
                HasMet = false;
                WarPartyTimer = new DateTimer(currentDate, DistanceToCapital)
                {
                    TriggerOnce = true
                };

                TradePartyTimer = new DateTimer(currentDate, DistanceToCapital)
                {
                    TriggerOnce = true
                };
            }

            public void DispatchNewTradeEnvoy(DateTime currentDate)
            {
                TradePartyTimer = new DateTimer(currentDate, DistanceToCapital)
                {
                    TriggerOnce = true
                };
            }

            public void DispatchNewWarParty(DateTime currentDate)
            {
                WarPartyTimer = new DateTimer(currentDate, DistanceToCapital)
                {
                    TriggerOnce = true
                };
            }

            public Relationship GetCurrentRelationship()
            {
                float feeling = GetCurrentFeeling();

                if (feeling < -0.5f)
                {
                    return Relationship.Hateful;
                }
                else if (feeling < 0.5f)
                {
                    return Relationship.Indifferent;
                }
                else
                {
                    return Relationship.Loving;
                }
            }

            public bool HasEvent(string text)
            {
                return RecentEvents.Any(e => e.Description == text);
            }

            public float GetCurrentFeeling()
            {
                return RecentEvents.Sum(e => e.Change);
            }

            public void UpdateEvents(DateTime currentDate)
            {
                RecentEvents.RemoveAll((e) => currentDate - e.Time > e.Duration);
            }
        }

        [JsonIgnore]
        public FactionLibrary Factions { get { return World.Factions; }}


        [JsonArrayAttribute]
        public class PoliticsDictionary : Dictionary<Pair<string>, Politics>
        {
            // Empty class needed to deserialize dictionary of string pairs.
        }

        public PoliticsDictionary FactionPolitics { get; set; }

        [JsonIgnore]
        public WorldManager World { get; set; }

        public void OnDeserializing(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
        }

        private TradeEnvoy CurrentTradeEnvoy = null;
        private WarParty CurrentWarParty = null;
        public DateTime TimeOfLastTrade = new DateTime();
        public string LastTradeEmpire = "";

        public Diplomacy()
        {
            
        }

        public Diplomacy(WorldManager world)
        {
            World = world;
            FactionPolitics = new PoliticsDictionary();
            TimeOfLastTrade = world.Time.CurrentDate;
        }

        public Politics GetPolitics(Faction factionA, Faction factionB)
        {
            return FactionPolitics[new Pair<string>(factionA.Name, factionB.Name)];
        }

        public void InitializeFactionPolitics(Faction New, DateTime Now)
        {
            TimeSpan forever = new TimeSpan(999999, 0, 0, 0);

            foreach (var faction in Factions.Factions)
            {
                Pair<string> pair = new Pair<string>(faction.Value.Name, New.Name);

                if (FactionPolitics.ContainsKey(pair))
                    continue;

                if (faction.Key == New.Name)
                {
                    FactionPolitics[pair] = new Politics(Now, new TimeSpan(0, 0, 0))
                    {
                        Faction = faction.Value,
                        HasMet = true,
                        RecentEvents = new List<PoliticalEvent>()
                            {
                                new PoliticalEvent()
                                {
                                    Change = 1.0f,
                                    Description = "we are of the same faction",
                                    Duration = forever,
                                    Time = Now
                                }
                            }
                    };
                }
                else
                {
                    Point c1 = faction.Value.Center;
                    Point c2 = New.Center;
                    double dist = Math.Sqrt(Math.Pow(c1.X - c2.X, 2) + Math.Pow(c1.Y - c2.Y, 2));
                    // Time always takes between 1 and 4 days of travel.
                    double timeInMinutes = Math.Min(Math.Max(dist * 2.0f, 1440), 1440 * 4) + MathFunctions.RandInt(0, 250);

                    Politics politics = new Politics(Now, new TimeSpan(0, (int)(timeInMinutes), 0))
                    {
                        Faction = New,
                        HasMet = false,
                        RecentEvents = new List<PoliticalEvent>(),
                    };

                    politics.DispatchNewTradeEnvoy(Now);

                    if (faction.Value.Race == New.Race)
                    {
                        politics.RecentEvents.Add(new PoliticalEvent()
                        {
                            Change = 0.5f,
                            Description = "we are of the same people",
                            Duration = forever,
                            Time = Now
                        });

                    }

                    if (faction.Value.Race.NaturalEnemies.Any(name => name == New.Race.Name))
                    {
                        if (!politics.HasEvent("we are taught to hate your kind"))
                        {
                            politics.RecentEvents.Add(new PoliticalEvent()
                            {
                                Change = -10.0f, // Make this negative and we get an instant war party rush.
                                Description = "we are taught to hate your kind",
                                Duration = forever,
                                Time = Now
                            });
                        }
                    }

                    FactionPolitics[pair] = politics;
                }

            }

            FactionPolitics[new Pair<string>("Undead", "Player")].RecentEvents.Add(new PoliticalEvent()
            {
                Change = -10.0f,
                Description = "Test hate",
                Duration = forever,
                Time = Now
            });
        }

        public void Initialize(DateTime now)
        {
            TimeSpan forever = new TimeSpan(999999, 0, 0, 0);
            foreach (var faction in Factions.Factions)
                InitializeFactionPolitics(faction.Value, now);
        }

        public TradeEnvoy SendTradeEnvoy(Faction natives, WorldManager world)
        {
            if (!world.PlayerFaction.GetRooms().Any(room => room is BalloonPort && room.IsBuilt))
            {
                world.MakeAnnouncement(String.Format("Trade envoy from {0} left. No balloon port!", natives.Name));
                return null;
            }
            TradeEnvoy envoy = null;

            List<CreatureAI> creatures =
                world.MonsterSpawner.Spawn(world.MonsterSpawner.GenerateSpawnEvent(natives,
                world.PlayerFaction, MathFunctions.Random.Next(4) + 1, false));

            if (natives.TradeMoney < 100m)
            {
                natives.TradeMoney += MathFunctions.Rand(250.0f, 5000.0f);
            }

            envoy = new TradeEnvoy(world.Time.CurrentDate)
            {
                Creatures = creatures,
                OtherFaction = world.PlayerFaction,
                ShouldRemove = false,
                OwnerFaction = natives,
                TradeGoods = natives.Race.GenerateResources(),
                TradeMoney = natives.TradeMoney
            };

            if (natives.Race.IsNative)
            {
                if (natives.Economy == null)
                {
                    natives.Economy = new Economy(natives, 1000.0m, World, new CompanyInformation()
                    {
                        Name = natives.Name
                    });
                }

                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                       creature.Physics.AddChild(new ResourcePack(World.ComponentManager));
                        if (natives.Economy == null)
                        {
                            natives.Economy = new Economy(natives, 1000.0m, World, new CompanyInformation()
                            {
                                Name = natives.Name
                            });
                        }

                    creature.Physics.AddChild(new Flag(World.ComponentManager, Vector3.Up * 0.5f + Vector3.Backward * 0.25f, natives.Economy.Company.Information));
                }
            }
            else
            {
                Body balloon = world.PlayerFaction.DispatchBalloon();

                foreach (CreatureAI creature in creatures)
                {
                    Matrix tf = creature.Physics.LocalTransform;
                    tf.Translation = balloon.LocalTransform.Translation;
                    creature.Physics.LocalTransform = tf;
                }
            }

            foreach (CreatureAI creature in envoy.Creatures)
                creature.Physics.AddChild(new ResourcePack(World.ComponentManager));

            envoy.DistributeGoods();
            natives.TradeEnvoys.Add(envoy);
            world.MakeAnnouncement(new DwarfCorp.Gui.Widgets.QueuedAnnouncement
            {
                Text = String.Format("Trade envoy from {0} has arrived!", natives.Name),
                ClickAction = (gui, sender) =>
                {
                    envoy.Creatures.First().ZoomToMe();
                    gui.ShowModalPopup(gui.ConstructWidget(new Gui.Widgets.Popup
                    {
                        Text = String.Format("Traders from {0} ({1}) have entered our territory. They will try to get to our balloon port to trade with us.", natives.Name, natives.Race.Name),
                        OkayText = "Okay!",
                        OnClose = (widget) =>
                        {
                            sender.Keep = false;
                        }
                    }));
                },
                ShouldKeep = () =>
                {
                    return envoy.ExpiditionState == Expedition.State.Arriving;
                }
            });

            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);


            world.Tutorial("trade");
            if (!String.IsNullOrEmpty(natives.Race.TradeMusic))
                SoundManager.PlayMusic(natives.Race.TradeMusic);

            return envoy;
        }

        public WarParty SendWarParty(Faction natives)
        {
            natives.World.MakeAnnouncement(String.Format("War party from {0} has arrived!", natives.Name), null);
            natives.World.Tutorial("war");
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
            Politics politics = GetPolitics(natives, natives.World.PlayerFaction);
            politics.WasAtWar = true;
            List<CreatureAI> creatures = natives.World.MonsterSpawner.Spawn(natives.World.MonsterSpawner.GenerateSpawnEvent(natives, natives.World.PlayerFaction, MathFunctions.Random.Next(5) + 1, true));
            var party = new WarParty(natives.World.Time.CurrentDate)
            {
                Creatures = creatures,
                OtherFaction = natives.World.PlayerFaction,
                ShouldRemove = false,
                OwnerFaction = natives
            };
            natives.WarParties.Add(party);

            foreach (var creature in creatures)
            {
                if (natives.Economy == null)
                {
                    natives.Economy = new Economy(natives, (decimal)MathFunctions.Rand(1000, 9999), World, null);
                }
                if (natives.Economy.Company.Information == null)
                    natives.Economy.Company.Information = new CompanyInformation();

                creature.Physics.AddChild(new Flag(World.ComponentManager, Vector3.Up * 0.5f + Vector3.Backward * 0.25f, natives.Economy.Company.Information));
            }
            return party;
        }

        public void Update(DwarfTime time, DateTime currentDate, WorldManager world)
        {
            World = world;
#if UPTIME_TEST
            return;
#endif
            var timeSinceLastTrade = world.Time.CurrentDate  - TimeOfLastTrade;
            foreach (var mypolitics in FactionPolitics)
            {
                Pair<string> pair = mypolitics.Key;
                if (!pair.IsSelfPair() && pair.Contains(world.PlayerFaction.Name))
                {
                   
                    Faction otherFaction = null;

                    otherFaction = pair.First.Equals(world.PlayerFaction.Name) ? Factions.Factions[pair.Second] : Factions.Factions[pair.First];
                    UpdateTradeEnvoys(otherFaction);
                    UpdateWarParties(otherFaction);
                    Politics relation = mypolitics.Value;

                    bool needsNewTradeEnvoy = true;

                    if (CurrentTradeEnvoy != null)
                    {
                        needsNewTradeEnvoy = CurrentTradeEnvoy.Creatures.Count == 0 || 
                            CurrentTradeEnvoy.Creatures.All(creature => creature.IsDead);
                    }

                    if (needsNewTradeEnvoy)
                    {
                        CurrentTradeEnvoy = null;
                    }

                    bool needsNewWarparty = true;

                    if (CurrentWarParty != null)
                    {
                        needsNewWarparty = CurrentWarParty.Creatures.Count == 0 ||
                            CurrentWarParty.Creatures.All(creature => creature.IsDead);
                    }

                    if (needsNewWarparty)
                    {
                        CurrentWarParty = null;
                    }
                   

                    if (needsNewTradeEnvoy && otherFaction.Race.IsIntelligent  && !otherFaction.IsRaceFaction && 
                        relation.GetCurrentRelationship() != Relationship.Hateful)
                    {
                        if (otherFaction.TradeEnvoys.Count == 0)
                        {
                            relation.TradePartyTimer.Update(currentDate);

                            if (relation.TradePartyTimer.HasTriggered && timeSinceLastTrade.TotalDays > 1.0 && LastTradeEmpire != otherFaction.Name)
                            {
                                relation.TradePartyTimer.Reset(World.Time.CurrentDate);
                                CurrentTradeEnvoy = SendTradeEnvoy(otherFaction, world);
                                TimeOfLastTrade = world.Time.CurrentDate;
                                LastTradeEmpire = otherFaction.Name;
                            }
                        }
                        else if (otherFaction.TradeEnvoys.Count == 0)
                        {
                            relation.DispatchNewTradeEnvoy(world.Time.CurrentDate);
                        }

                    }
                    else if (needsNewWarparty &&
                             otherFaction.Race.IsIntelligent && !otherFaction.IsRaceFaction &&
                             relation.GetCurrentRelationship() == Relationship.Hateful)
                    {
                        if (otherFaction.WarParties.Count == 0 && !relation.WarPartyTimer.HasTriggered)
                        {
                            relation.WarPartyTimer.Update(currentDate);

                            if (relation.WarPartyTimer.HasTriggered)
                            {
                                CurrentWarParty = SendWarParty(otherFaction);
                            }
                        }
                        else if (otherFaction.WarParties.Count == 0)
                        {
                            relation.DispatchNewWarParty(world.Time.CurrentDate);
                        }
                    }
                }
                mypolitics.Value.UpdateEvents(currentDate);
            }
        }

        [JsonObject(IsReference =true)]
        public class TradeTask : Task
        {
            public Zone TradePort;
            public TradeEnvoy Envoy;

            public TradeTask()
            {

            }

            public TradeTask(Zone tradePort, TradeEnvoy envoy)
            {
                Name = "Trade";
                Priority = PriorityType.High;
                TradePort = tradePort;
                Envoy = envoy;
            }

            IEnumerable<Act.Status> RecallEnvoyOnFail(TradeEnvoy envoy)
            {
                Diplomacy.RecallEnvoy(envoy);
                TradePort.Faction.World.MakeAnnouncement("Envoy from " + envoy.OwnerFaction.Name + " left. Trade port inaccessible.");
                yield return Act.Status.Success;
            }

            public override Act CreateScript(Creature agent)
            {
                return new GoToZoneAct(agent.AI, TradePort) | new Wrap(() => RecallEnvoyOnFail(Envoy));
            }

            public override Task Clone()
            {
                return new TradeTask(TradePort, Envoy);
            }
        }

        public void UpdateTradeEnvoys(Faction faction)
        {
            foreach (TradeEnvoy envoy in faction.TradeEnvoys)
            {
                if (envoy.DeathTimer.Update(faction.World.Time.CurrentDate))
                {
                    envoy.Creatures.ForEach((creature) => creature.GetRoot().Die());
                }

                Diplomacy.Politics politics = faction.World.Diplomacy.GetPolitics(faction, envoy.OtherFaction);
                if (politics.GetCurrentRelationship() == Relationship.Hateful)
                {
                    RecallEnvoy(envoy);
                }
                else
                {
                    if (envoy.Creatures.Any(
                        creature => envoy.OtherFaction.Designations.IsDesignation(creature.Physics, DesignationType.Attack)))
                    {

                        if (!politics.HasEvent("You attacked our trade delegates"))
                        {
                            politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                            {
                                Change = -1.0f,
                                Description = "You attacked our trade delegates",
                                Duration = new TimeSpan(1, 0, 0, 0),
                                Time = faction.World.Time.CurrentDate
                            });
                        }
                        else
                        {
                            politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                            {
                                Change = -2.0f,
                                Description = "You attacked our trade delegates more than once",
                                Duration = new TimeSpan(1, 0, 0, 0),
                                Time = faction.World.Time.CurrentDate
                            });
                        }
                    }
                }

                if (!envoy.ShouldRemove && envoy.ExpiditionState == Expedition.State.Arriving)
                {
                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                       
                        Room tradePort = envoy.OtherFaction.GetNearestRoomOfType(BalloonPort.BalloonPortName,
                            creature.Position);

                        if (tradePort == null)
                        {
                            World.MakeAnnouncement("We need a balloon trade port to trade.");
                            World.Tutorial("trade");
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
                            RecallEnvoy(envoy);
                            break;
                        }

                        if (creature.Tasks.Count == 0)
                        {
                            TradeEnvoy envoy1 = envoy;
                            creature.Tasks.Add(new TradeTask(tradePort, envoy1));
                        }

                        if (!tradePort.IsRestingOnZone(creature.Position)) continue;

                        envoy.ExpiditionState = Expedition.State.Trading;

                        World.Paused = true;
                        GameState.Game.StateManager.PushState(new Dialogue.DialogueState(
                            GameState.Game,
                            GameState.Game.StateManager,
                            envoy,
                            World.PlayerFaction,
                            World));
                        //GameState.Game.StateManager.PushState(new DiplomacyState(GameState.Game,
                        //    GameState.Game.StateManager,
                        //    faction.World, envoy)
                        //{
                        //    Name = "DiplomacyState_" + faction.Name,
                        //    Envoy = envoy
                        //});
                        break;
                    }
                }
                else if (envoy.ExpiditionState == Expedition.State.Leaving)
                {
                    BoundingBox worldBBox = faction.World.ChunkManager.Bounds;

                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                        if (creature.Tasks.Count == 0)
                        {
                            creature.LeaveWorld();
                        }
                    }

                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                        if (MathFunctions.Dist2D(worldBBox, creature.Position) < 2.0f)
                        {
                            creature.GetRoot().Delete();
                        }
                    }
                }

                if (envoy.Creatures.All(creature => creature.IsDead))
                {
                    envoy.ShouldRemove = true;

                    World.GoalManager.OnGameEvent(new Goals.Events.TradeEnvoyKilled
                    {
                        PlayerFaction = envoy.OtherFaction,
                        OtherFaction = envoy.OwnerFaction
                    });
                }

               
            }

            bool hadFactions = faction.TradeEnvoys.Count > 0;
            faction.TradeEnvoys.RemoveAll(t => t.ShouldRemove);

            if (hadFactions && faction.TradeEnvoys.Count == 0)
            {
                var music = World.Time.IsDay() ? "main_theme_day" : "main_theme_night";
                SoundManager.PlayMusic(music);
            }
        }

        public void UpdateWarParties(Faction faction)
        {
            foreach (var party in faction.WarParties)
            {
                if (party.DeathTimer.Update(faction.World.Time.CurrentDate))
                {
                    party.Creatures.ForEach((creature) => creature.Die());
                }

                foreach (var creature in party.Creatures)
                {
                    if (MathFunctions.RandEvent(0.001f))
                    {
                        creature.AssignTask(new ActWrapperTask(new GetMoneyAct(creature, (decimal)MathFunctions.Rand(0, 64.0f), party.OtherFaction))
                        {
                            Priority = Task.PriorityType.Medium
                        });
                    }
                }

                Diplomacy.Politics politics =  faction.World.Diplomacy.GetPolitics(faction, party.OtherFaction);

                if (politics.GetCurrentRelationship() != Relationship.Hateful)
                {
                    RecallWarParty(party);
                }

                if (party.Creatures.All(creature => creature.IsDead))
                {
                    party.ShouldRemove = true;

                    // Killed entire war party. Wonderful!
                    World.GoalManager.OnGameEvent(new Goals.Events.WarPartyDefeated
                    {
                        PlayerFaction = party.OtherFaction,
                        OtherFaction = party.OwnerFaction
                    });
                }
            }

            faction.WarParties.RemoveAll(w => w.ShouldRemove);
        }

        public static void RecallEnvoy(TradeEnvoy envoy)
        {
            // TODO: do ths more naturally
            envoy.ExpiditionState = Expedition.State.Leaving;
            foreach (CreatureAI creature in envoy.Creatures)
            {
                creature.LeaveWorld();
            }
        }

        public static void RecallWarParty(WarParty party)
        {
            // TODO: do ths more naturally
            party.ExpiditionState = Expedition.State.Leaving;
            foreach (CreatureAI creature in party.Creatures)
            {
                creature.LeaveWorld();
            }
        }

    }
}
