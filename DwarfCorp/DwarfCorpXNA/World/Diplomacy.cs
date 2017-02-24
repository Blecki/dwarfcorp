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

            public Politics(DateTime currentDate)
            {
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

        public FactionLibrary Factions { get; set; }


        [JsonArrayAttribute]
        public class PoliticsDictionary : Dictionary<Pair<string>, Politics>
        {
            // Empty class needed to deserialize dictionary of string pairs.
        }

        public PoliticsDictionary FactionPolitics { get; set; }


        public Diplomacy(FactionLibrary factions)
        {
            Factions = factions;
            FactionPolitics = new PoliticsDictionary();
        }

        public Politics GetPolitics(Faction factionA, Faction factionB)
        {
            return FactionPolitics[new Pair<string>(factionA.Name, factionB.Name)];
        }

        public void Initialize(DateTime now)
        {
            TimeSpan forever = new TimeSpan(999999, 0, 0, 0);
            foreach (var faction in Factions.Factions)
            {
                foreach (var otherFaction in Factions.Factions)
                {
                    Pair<string> pair = new Pair<string>(faction.Value.Name, otherFaction.Value.Name);

                    if (FactionPolitics.ContainsKey(pair)) 
                        continue;

                    if (faction.Key == otherFaction.Key)
                    {
                        FactionPolitics[pair] = new Politics()
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
                                    Time = now
                                }
                            }
                        };
                    }
                    else
                    {
                        Point c1 = faction.Value.Center;
                        Point c2 = otherFaction.Value.Center;
                        double dist = Math.Sqrt(Math.Pow(c1.X - c2.X, 2) + Math.Pow(c1.Y - c2.Y, 2));
                        // Time always takes between 1 and 4 days of travel.
                        double timeInMinutes = Math.Min(Math.Max(dist * 8.0f, 1440), 1440 * 4);

                        Politics politics = new Politics()
                        {
                            Faction = otherFaction.Value,
                            HasMet = false,
                            RecentEvents = new List<PoliticalEvent>(),
                            DistanceToCapital = new TimeSpan(0, (int)(timeInMinutes), 0)
                        };

                        politics.DispatchNewTradeEnvoy(DwarfGame.World.Time.CurrentDate);

                        if (faction.Value.Race == otherFaction.Value.Race)
                        {
                            politics.RecentEvents.Add(new PoliticalEvent()
                            {
                                Change = 0.5f,
                                Description = "we are of the same people",
                                Duration = forever,
                                Time = now
                            });

                        }

                        if (faction.Value.Race.NaturalEnemies.Any(name => name == otherFaction.Value.Race.Name))
                        {
                            if (!politics.HasEvent("we are taught to hate your kind"))
                            {
                                politics.RecentEvents.Add(new PoliticalEvent()
                                {
                                    Change = -2.0f,
                                    Description = "we are taught to hate your kind",
                                    Duration = forever,
                                    Time = now
                                });
                            }
                        }

                        FactionPolitics[pair] = politics;
                    }

                }
            }
            
        }

        public void SendTradeEnvoy(Faction natives, WorldManager world)
        {
            if (!world.World.gameState.IsActiveState) return;
            Faction.TradeEnvoy envoy = null;
            if (natives.Race.IsNative)
            {
                List<CreatureAI> creatures =
                    world.MonsterSpawner.Spawn(world.MonsterSpawner.GenerateSpawnEvent(natives,
                        world.PlayerFaction, MathFunctions.Random.Next(4) + 1, false));
                if (creatures.Count > 0)
                {
                    envoy = new Faction.TradeEnvoy()
                    {
                        Creatures = creatures,
                        OtherFaction = world.PlayerFaction,
                        ShouldRemove = false,
                        OwnerFaction = natives,
                        TradeGoods = natives.Race.GenerateResources(),
                        TradeMoney = natives.TradeMoney
                    };

                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                        ResourcePack resources = new ResourcePack(creature.Physics);
                    }
                    envoy.DistributeGoods();

                    natives.TradeEnvoys.Add(envoy);
                    world.MakeAnnouncement(String.Format("Trade envoy from {0} has arrived!", natives.Name),
                        "Click to zoom to location.", creatures.First().ZoomToMe);
                }
            }
            else
            {

                List<CreatureAI> creatures =
                    world.MonsterSpawner.Spawn(world.MonsterSpawner.GenerateSpawnEvent(natives,
                        world.PlayerFaction, MathFunctions.Random.Next(4) + 1, false));


                if (creatures.Count > 0)
                {
                    Body balloon = world.PlayerFaction.DispatchBalloon();

                    foreach (CreatureAI creature in creatures)
                    {
                        Matrix tf = creature.Physics.LocalTransform;
                        tf.Translation = balloon.LocalTransform.Translation;
                        creature.Physics.LocalTransform = tf;
                    }

                    envoy = new Faction.TradeEnvoy()
                    {
                        Creatures = creatures,
                        OtherFaction = world.PlayerFaction,
                        ShouldRemove = false,
                        OwnerFaction = natives,
                        TradeGoods = natives.Race.GenerateResources(),
                        TradeMoney = natives.TradeMoney
                    };
                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                        ResourcePack resources = new ResourcePack(creature.Physics);
                    }
                    envoy.DistributeGoods();
                    natives.TradeEnvoys.Add(envoy);
                    world.MakeAnnouncement(String.Format("Trade envoy from {0} has arrived!",
                        natives.Name), "Click to zoom to location.", creatures.First().ZoomToMe);
                }
            }

        }

        public void SendWarParty(Faction natives)
        {
            // todo
            DwarfGame.World.MakeAnnouncement(String.Format("War party from {0} has arrived!", natives.Name), null);
            Politics politics = GetPolitics(natives, DwarfGame.World.PlayerFaction);
            politics.WasAtWar = true;
            List<CreatureAI> creatures = DwarfGame.World.MonsterSpawner.Spawn(DwarfGame.World.MonsterSpawner.GenerateSpawnEvent(natives, DwarfGame.World.PlayerFaction, MathFunctions.Random.Next(5) + 1, true));

            natives.WarParties.Add(new Faction.WarParty()
            {
                Creatures = creatures,
                OtherFaction = DwarfGame.World.PlayerFaction,
                ShouldRemove = false
            });

        }


        public void Update(DwarfTime time, DateTime currentDate)
        {
            foreach (var mypolitics in FactionPolitics)
            {
                Pair<string> pair = mypolitics.Key;
                if (!pair.IsSelfPair() && pair.Contains(DwarfGame.World.PlayerFaction.Name))
                {
                   
                    Faction otherFaction = null;

                    otherFaction = pair.First.Equals(DwarfGame.World.PlayerFaction.Name) ? Factions.Factions[pair.Second] : Factions.Factions[pair.First];
                    UpdateTradeEnvoys(otherFaction);
                    UpdateWarParties(otherFaction);
                    Race race = otherFaction.Race;
                    Politics relation = mypolitics.Value;

                    
                    if (race.IsIntelligent  && !otherFaction.IsRaceFaction && 
                        relation.GetCurrentRelationship() != Relationship.Hateful)
                    {
                        if (otherFaction.TradeEnvoys.Count == 0 && !relation.TradePartyTimer.HasTriggered)
                        {
                            relation.TradePartyTimer.Update(currentDate);

                            if (relation.TradePartyTimer.HasTriggered)
                            {
                                SendTradeEnvoy(otherFaction, DwarfGame.World);
                            }
                        }
                        else if (otherFaction.TradeEnvoys.Count == 0)
                        {
                            relation.DispatchNewTradeEnvoy(DwarfGame.World.Time.CurrentDate);
                        }

                    }
                    else if (race.IsIntelligent && !otherFaction.IsRaceFaction &&
                             relation.GetCurrentRelationship() == Relationship.Hateful)
                    {
                        if (otherFaction.WarParties.Count == 0 && !relation.WarPartyTimer.HasTriggered)
                        {
                            relation.WarPartyTimer.Update(currentDate);

                            if (relation.WarPartyTimer.HasTriggered)
                            {
                                SendWarParty(otherFaction);
                            }
                        }
                        else if (otherFaction.WarParties.Count == 0)
                        {
                            relation.DispatchNewWarParty(DwarfGame.World.Time.CurrentDate);
                        }
                    }
                }
                mypolitics.Value.UpdateEvents(currentDate);
            }
        }


        public void UpdateTradeEnvoys(Faction faction)
        {
            foreach (Faction.TradeEnvoy envoy in faction.TradeEnvoys)
            {
                if (envoy.DeathTimer.Update(DwarfGame.World.Time.CurrentDate))
                {
                    envoy.Creatures.ForEach((creature) => creature.GetEntityRootComponent().Die());
                }

                Diplomacy.Politics politics = DwarfGame.World.ComponentManager.Diplomacy.GetPolitics(faction, envoy.OtherFaction);
                if (politics.GetCurrentRelationship() == Relationship.Hateful)
                {
                    RecallEnvoy(envoy);
                }
                else
                {
                    if (envoy.Creatures.Any(
                        creature => envoy.OtherFaction.AttackDesignations.Contains(creature.Physics)))
                    {

                        if (!politics.HasEvent("You attacked our trade delegates"))
                        {
                            politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                            {
                                Change = -1.0f,
                                Description = "You attacked our trade delegates",
                                Duration = new TimeSpan(1, 0, 0, 0),
                                Time = DwarfGame.World.Time.CurrentDate
                            });
                        }
                        else
                        {
                            politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                            {
                                Change = -2.0f,
                                Description = "You attacked our trade delegates more than once",
                                Duration = new TimeSpan(1, 0, 0, 0),
                                Time = DwarfGame.World.Time.CurrentDate
                            });
                        }
                    }
                }

                if (!envoy.ShouldRemove && envoy.ExpiditionState == Faction.Expidition.State.Arriving)
                {
                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                       
                        Room tradePort = envoy.OtherFaction.GetNearestRoomOfType(BalloonPort.BalloonPortName,
                            creature.Position);

                        if (creature.Tasks.Count == 0)
                        {
                            creature.Tasks.Add(new ActWrapperTask(new GoToZoneAct(creature, tradePort)) { Name = "Go to trade port.", Priority = Task.PriorityType.Urgent});
                        }

                        if (!tradePort.IsRestingOnZone(creature.Position)) continue;

                        envoy.ExpiditionState = Faction.Expidition.State.Trading;
                        GameState.Game.StateManager.PushState(new DiplomacyState(GameState.Game,
                            GameState.Game.StateManager,
                            DwarfGame.World.World, envoy)
                        {
                            Name = "DiplomacyState_" + faction.Name,
                            Envoy = envoy
                        });
                        break;
                    }
                }
                else if (envoy.ExpiditionState == Faction.Expidition.State.Leaving)
                {
                    BoundingBox worldBBox = DwarfGame.World.ChunkManager.Bounds;

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
                            creature.GetEntityRootComponent().Delete();
                            creature.IsDead = true;
                        }
                    }
                }

                if (envoy.Creatures.All(creature => creature.IsDead))
                {
                    envoy.ShouldRemove = true;
                }

               
            }

            faction.TradeEnvoys.RemoveAll(t => t.ShouldRemove);
        }

        public void UpdateWarParties(Faction faction)
        {
            foreach (Faction.WarParty party in faction.WarParties)
            {
                if (party.DeathTimer.Update(DwarfGame.World.Time.CurrentDate))
                {
                    party.Creatures.ForEach((creature) => creature.Die());
                }

                Diplomacy.Politics politics =  DwarfGame.World.ComponentManager.Diplomacy.GetPolitics(faction, party.OtherFaction);
                if (politics.GetCurrentRelationship() != Relationship.Hateful)
                {
                    RecallWarParty(party);
                }

                if (party.Creatures.All(creature => creature.IsDead))
                {
                    party.ShouldRemove = true;
                }
            }

            faction.WarParties.RemoveAll(w => w.ShouldRemove);
        }

        public static void RecallEnvoy(Faction.TradeEnvoy envoy)
        {
            // TODO: do ths more naturally
            envoy.ExpiditionState = Faction.Expidition.State.Leaving;
            foreach (CreatureAI creature in envoy.Creatures)
            {
                creature.LeaveWorld();
            }
        }

        public static void RecallWarParty(Faction.WarParty party)
        {
            // TODO: do ths more naturally
            party.ExpiditionState = Faction.Expidition.State.Leaving;
            foreach (CreatureAI creature in party.Creatures)
            {
                creature.LeaveWorld();
            }
        }

    }
}
