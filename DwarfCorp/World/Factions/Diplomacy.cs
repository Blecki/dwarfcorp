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
    // Todo: Combine with WorldManager!!
    public class Diplomacy
    {
        [JsonIgnore] public WorldManager World;

        public void OnDeserializing(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
        }

        public Diplomacy()
        {
            
        }

        public Diplomacy(WorldManager world)
        {
            World = world;
        }

        // Todo: Combine war party and trade envoy handling code?
        public TradeEnvoy SendTradeEnvoy(Faction natives, WorldManager world)
        {
            if (!world.EnumerateZones().Any(room => room is BalloonPort && room.IsBuilt))
            {
                world.MakeAnnouncement(String.Format("Trade envoy from {0} left: No balloon port!", natives.ParentFaction.Name));
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.15f);
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
                TradeGoods = natives.Race.GenerateResources(world),
                TradeMoney = natives.TradeMoney
            };

            if (natives.Race.IsNative)
            {
                if (natives.Economy == null)
                {
                    natives.Economy = new Company(natives, 1000.0m, new CompanyInformation()
                    {
                        Name = natives.ParentFaction.Name
                    });
                }

                foreach (CreatureAI creature in envoy.Creatures)
                {
                    creature.Physics.AddChild(new ResourcePack(World.ComponentManager));
                    creature.Physics.AddChild(new Flag(World.ComponentManager, Vector3.Up * 0.5f + Vector3.Backward * 0.25f, natives.Economy.Information));
                }
            }
            else
            {
                GameComponent balloon = null;

                    var rooms = World.EnumerateZones().Where(room => room.Type.Name == "Balloon Port").ToList();

                if (rooms.Count != 0)
                {
                    Vector3 pos = rooms.First().GetBoundingBox().Center();
                    balloon = Balloon.CreateBalloon(pos + new Vector3(0, 1000, 0), pos + Vector3.UnitY * 15, World.ComponentManager, natives);
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
                    if (natives.Economy == null)
                    {
                        natives.Economy = new Company(natives, 1000.0m, new CompanyInformation()
                        {
                            Name = natives.ParentFaction.Name
                        });
                    }

                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                        creature.Physics.AddChild(new ResourcePack(World.ComponentManager));
                        creature.Physics.AddChild(new Flag(World.ComponentManager, Vector3.Up * 0.5f + Vector3.Backward * 0.25f, natives.Economy.Information));
                    }
                }
            }

            foreach (CreatureAI creature in envoy.Creatures)
                creature.Physics.AddChild(new ResourcePack(World.ComponentManager));

            envoy.DistributeGoods();
            natives.TradeEnvoys.Add(envoy);
            world.MakeAnnouncement(new DwarfCorp.Gui.Widgets.QueuedAnnouncement
            {
                Text = String.Format("Trade envoy from {0} has arrived!", natives.ParentFaction.Name),
                ClickAction = (gui, sender) =>
                {
                    if (envoy.Creatures.Count > 0)
                    {
                        envoy.Creatures.First().ZoomToMe();
                        World.UserInterface.MakeWorldPopup(String.Format("Traders from {0} ({1}) have entered our territory.\nThey will try to get to our balloon port to trade with us.", natives.ParentFaction.Name, natives.Race.Name),
                            envoy.Creatures.First().Physics, -10);
                    }
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
            natives.World.Tutorial("war");
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
            Politics politics = World.GetPolitics(natives, natives.World.PlayerFaction);
            politics.IsAtWar = true;
            List<CreatureAI> creatures = natives.World.MonsterSpawner.Spawn(natives.World.MonsterSpawner.GenerateSpawnEvent(natives, natives.World.PlayerFaction, MathFunctions.Random.Next(World.Overworld.Difficulty) + 1, false));
            var party = new WarParty(natives.World.Time.CurrentDate)
            {
                Creatures = creatures,
                OtherFaction = natives.World.PlayerFaction,
                ShouldRemove = false,
                OwnerFaction = natives
            };
            natives.WarParties.Add(party);
            natives.World.MakeAnnouncement(new Gui.Widgets.QueuedAnnouncement()
            {
                Text = String.Format("A war party from {0} has arrived!", natives.ParentFaction.Name),
                SecondsVisible = 60,
                ClickAction = (gui, sender) =>
                {
                    if (party.Creatures.Count > 0)
                    {
                        party.Creatures.First().ZoomToMe();
                        World.UserInterface.MakeWorldPopup(String.Format("Warriors from {0} ({1}) have entered our territory. They will prepare for a while and then attack us.", natives.ParentFaction.Name, natives.Race.Name), party.Creatures.First().Physics, -10);
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
                if (natives.Economy == null)
                {
                    natives.Economy = new Company(natives, (decimal)MathFunctions.Rand(1000, 9999), null);
                }
                if (natives.Economy.Information == null)
                    natives.Economy.Information = new CompanyInformation();

                creature.Physics.AddChild(new Flag(World.ComponentManager, Vector3.Up * 0.5f + Vector3.Backward * 0.25f, natives.Economy.Information));
            }
            return party;
        }

        public void Update(DwarfTime time, DateTime currentDate, WorldManager world)
        {
            World = world;
#if UPTIME_TEST
            return;
#endif

            foreach (var faction in World.Factions.Factions)
            {
                UpdateTradeEnvoys(faction.Value);
                UpdateWarParties(faction.Value);
            }
        }

        public void UpdateTradeEnvoys(Faction faction)
        {
            foreach (TradeEnvoy envoy in faction.TradeEnvoys)
            {
                if (envoy.ExpiditionState == Expedition.State.Trading)
                {
                    if (envoy.UpdateWaitTimer(World.Time.CurrentDate))
                    {
                        World.MakeAnnouncement(String.Format("The envoy from {0} is leaving.", envoy.OwnerFaction.ParentFaction.Name));
                        RecallEnvoy(envoy);
                    }
                }

                envoy.Creatures.RemoveAll(creature => creature.IsDead);
                if (envoy.DeathTimer.Update(faction.World.Time.CurrentDate))
                {
                    envoy.Creatures.ForEach((creature) => creature.GetRoot().Die());
                }

                var politics = faction.World.GetPolitics(faction, envoy.OtherFaction);
                if (politics.GetCurrentRelationship() == Relationship.Hateful)
                {
                    World.MakeAnnouncement(String.Format("The envoy from {0} left: we are at war with them.", envoy.OwnerFaction.ParentFaction.Name));
                    RecallEnvoy(envoy);
                }
                else
                {
                    if (envoy.Creatures.Any(
                        // TODO (mklingen) why do I need this null check?
                        creature => creature.Creature != null && 
                        envoy.OtherFaction.Designations.IsDesignation(creature.Physics, DesignationType.Attack)))
                    {

                        if (!politics.HasEvent("You attacked our trade delegates"))
                        {
                            politics.AddEvent(new PoliticalEvent()
                            {
                                Change = -1.0f,
                                Description = "You attacked our trade delegates",
                            });
                        }
                        else
                        {
                            politics.AddEvent(new PoliticalEvent()
                            {
                                Change = -2.0f,
                                Description = "You attacked our trade delegates more than once",
                            });
                        }
                    }
                }

                if (!envoy.ShouldRemove && envoy.ExpiditionState == Expedition.State.Arriving)
                {
                    foreach (CreatureAI creature in envoy.Creatures)
                    {
                       
                        var tradePort = World.GetNearestRoomOfType("Balloon Port", creature.Position);

                        if (tradePort == null)
                        {
                            World.MakeAnnouncement("We need a balloon trade port to trade.", null, () =>
                            {
                                return World.GetNearestRoomOfType("Balloon Port", creature.Position) == null;
                            });
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

                        if (envoy.ExpiditionState != Expedition.State.Trading ||
                            !envoy.IsTradeWidgetValid())
                        {
                            envoy.MakeTradeWidget(World);
                        }
                        envoy.StartTrading(World.Time.CurrentDate);
                        envoy.ExpiditionState = Expedition.State.Trading;
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
                else
                {
                    if (!envoy.IsTradeWidgetValid())
                    {
                        envoy.MakeTradeWidget(World);
                    }
                }
                if (envoy.Creatures.All(creature => creature.IsDead))
                    envoy.ShouldRemove = true;
            }

            bool hadFactions = faction.TradeEnvoys.Count > 0;
            faction.TradeEnvoys.RemoveAll(t => t == null || t.ShouldRemove);

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
                bool doneWaiting = party.UpdateTimer(World.Time.CurrentDate);
                party.Creatures.RemoveAll(creature => creature.IsDead);
                if (party.DeathTimer.Update(World.Time.CurrentDate))
                    party.Creatures.ForEach((creature) => creature.Die());

                var politics = faction.World.GetPolitics(faction, party.OtherFaction);

                if (politics.GetCurrentRelationship() != Relationship.Hateful)
                    RecallWarParty(party);

                if (party.Creatures.All(creature => creature.IsDead))
                    party.ShouldRemove = true;

                if (!doneWaiting)
                    continue;
                else
                {
                    foreach (var creature in party.OwnerFaction.Minions)
                    {
                        if (creature.Tasks.Count == 0)
                        {
                            CreatureAI enemyMinion = party.OtherFaction.GetNearestMinion(creature.Position);
                            if (enemyMinion != null)// && !enemyMinion.Stats.IsFleeing)
                                creature.AssignTask(new KillEntityTask(enemyMinion.Physics, KillEntityTask.KillType.Auto));
                        }
                    }

                    if (party.ExpiditionState == Expedition.State.Arriving)
                    {
                        World.MakeAnnouncement(String.Format("The war party from {0} is attacking!", party.OwnerFaction.ParentFaction.Name));
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.15f);
                        party.ExpiditionState = Expedition.State.Fighting;
                    }
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
