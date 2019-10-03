using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DwarfCorp
{
    public class TradeEnvoy : Expedition
    {
        public DwarfBux TradeMoney;
        public ResourceSet TradeGoods;
        public DateTimer WaitForTradeTimer = null;
        public DwarfBux TributeDemanded = 0m;
        [JsonIgnore] public WorldPopup TradeWidget = null;

        public TradeEnvoy()
        {

        }

        public TradeEnvoy(DateTime date) : base(date)
        {
            WaitForTradeTimer = new DateTimer(date, new TimeSpan(0, 2, 0, 0, 0));
        }

        public void StartTrading(DateTime date)
        {
            WaitForTradeTimer = new DateTimer(date, new TimeSpan(0, 2, 0, 0, 0));
        }

        public bool IsTradeWidgetValid()
        {
            return TradeWidget != null && TradeWidget.BodyToTrack != null && !TradeWidget.BodyToTrack.IsDead;
        }

        public void MakeTradeWidget(WorldManager World)
        {
            var liveCreatures = Creatures.Where(creature => creature != null && !creature.IsDead);
            if (!liveCreatures.Any())
                return;

            var zones = World.EnumerateZones().OfType<BalloonPort>();

            CreatureAI closestCreature = null;
            float closestDist = float.MaxValue;

            if (zones.Any())
            {
                var zoneCenter = zones.First().GetBoundingBox().Center();
                foreach (var creature in liveCreatures)
                {
                    float dist = (creature.Position - zoneCenter).LengthSquared();
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestCreature = creature;
                    }
                }
            }
            else
                closestCreature = liveCreatures.First();

            TradeWidget = World.UserInterface.MakeWorldPopup(new Events.TimedIndicatorWidget()
            {
                Text = string.Format("Click here to trade with the {0}!", OwnerFaction.Race.Name),
                OnClick = (gui, sender) =>
                {
                    OpenDiplomacyConversation(World);
                },
                ShouldKeep = () => { return this.ExpiditionState == Expedition.State.Trading && !this.ShouldRemove; }
            }, closestCreature.Physics, new Vector2(0, -10));
            World.MakeAnnouncement(String.Format("Click here to trade with the {0}!", OwnerFaction.Race.Name), (gui, sender) =>
            {
                OpenDiplomacyConversation(World);
            }, () => { return this.ExpiditionState == Expedition.State.Trading && !this.ShouldRemove; }, false);
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
        }

        private void OpenDiplomacyConversation(WorldManager World)
        {
            World.Paused = true;
            var name = "";
            if (Creatures.Count > 0)
            {
                name = Creatures.First().Stats.FullName;
            }
            else
            {
                name = TextGenerator.ToTitleCase(TextGenerator.GenerateRandom(Datastructures.SelectRandom(OwnerFaction.Race.NameTemplates).ToArray()));
            }
            // Prepare conversation memory for an envoy conversation.
            var cMem = World.ConversationMemory;
            cMem.SetValue("$world", new Yarn.Value(World));
            cMem.SetValue("$envoy", new Yarn.Value(this));
            cMem.SetValue("$envoy_demands_tribute", new Yarn.Value(this.TributeDemanded != 0));
            cMem.SetValue("$envoy_tribute_demanded", new Yarn.Value((float)this.TributeDemanded.Value));
            cMem.SetValue("$envoy_name", new Yarn.Value(name));
            cMem.SetValue("$envoy_faction", new Yarn.Value(OwnerFaction.ParentFaction.Name));
            cMem.SetValue("$player_faction", new Yarn.Value(this.OtherFaction));
            cMem.SetValue("$offensive_trades", new Yarn.Value(0));
            cMem.SetValue("$trades", new Yarn.Value(0));

            var politics = World.Overworld.GetPolitics(OtherFaction.ParentFaction, OwnerFaction.ParentFaction);
            cMem.SetValue("$faction_was_at_war", new Yarn.Value(politics.IsAtWar));
            cMem.SetValue("$envoy_relationship", new Yarn.Value(politics.GetCurrentRelationship().ToString()));

            GameStateManager.PushState(new YarnState(OwnerFaction.World, OwnerFaction.Race.DiplomacyConversation, "Start", cMem));
        }

        public bool UpdateWaitTimer(DateTime now)
        {
            WaitForTradeTimer.Update(now);
            return WaitForTradeTimer.HasTriggered;
        }

        public void DistributeGoods()
        {
            //if (Creatures.Count == 0) return;
            //int goodsPerCreature = TradeGoods.TotalCount / Creatures.Count;
            //int currentGood = 0;
            //foreach (CreatureAI creature in Creatures)
            //    if (creature.GetRoot().GetComponent<ResourcePack>().HasValue(out var pack))
            //    {
            //        pack.Contents.Resources.Clear();
            //        for (int i = currentGood; i < global::System.Math.Min(currentGood + goodsPerCreature, TradeGoods.TotalCount); i++)
            //            pack.Contents.AddResource(TradeGoods[i]);
            //        currentGood += goodsPerCreature;
            //    }
        }

        public void Update(WorldManager World)
        {
            if (ExpiditionState == Expedition.State.Trading)
            {
                if (UpdateWaitTimer(World.Time.CurrentDate))
                {
                    World.MakeAnnouncement(String.Format("The envoy from {0} is leaving.", OwnerFaction.ParentFaction.Name));
                    RecallEnvoy();
                }
            }

            Creatures.RemoveAll(creature => creature.IsDead);
            if (DeathTimer.Update(World.Time.CurrentDate))
                Creatures.ForEach((creature) => creature.GetRoot().Die());

            var politics = World.Overworld.GetPolitics(OwnerFaction.ParentFaction, OtherFaction.ParentFaction);
            //if (politics.GetCurrentRelationship() == Relationship.Hateful)
            //{
            //    World.MakeAnnouncement(String.Format("The envoy from {0} left: we are at war with them.", OwnerFaction.ParentFaction.Name));
            //    RecallEnvoy();
            //}
            //else
            {
                if (Creatures.Any(
                    // TODO (mklingen) why do I need this null check?
                    creature => creature.Creature != null &&
                    World.PersistentData.Designations.IsDesignation(creature.Physics, DesignationType.Attack)))
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

            if (!ShouldRemove && ExpiditionState == Expedition.State.Arriving)
            {
                foreach (var creature in Creatures)
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
                        RecallEnvoy();
                        break;
                    }

                    if (creature.Tasks.Count == 0)
                        creature.Tasks.Add(new TradeTask(tradePort, this));

                    var zoneBox = tradePort.GetBoundingBox();
                    zoneBox.Max.Y += 1;
                    zoneBox = zoneBox.Expand(1.0f);
                    if (zoneBox.Contains(creature.Position) == ContainmentType.Disjoint)
                        continue;
                    
                    if (ExpiditionState != Expedition.State.Trading || !IsTradeWidgetValid())
                        MakeTradeWidget(World);

                    StartTrading(World.Time.CurrentDate);
                    ExpiditionState = Expedition.State.Trading;
                    break;
                }
            }
            else if (ExpiditionState == Expedition.State.Leaving)
            {
                BoundingBox worldBBox = World.ChunkManager.Bounds;

                foreach (CreatureAI creature in Creatures)
                    if (creature.Tasks.Count == 0)
                        creature.LeaveWorld();

                foreach (var creature in Creatures)
                    if (MathFunctions.Dist2D(worldBBox, creature.Position) < 2.0f)
                        creature.GetRoot().Delete();
            }
            else if (!IsTradeWidgetValid())
                MakeTradeWidget(World);

            if (!OwnerFaction.ParentFaction.IsCorporate && Creatures.All(creature => creature.IsDead))
                ShouldRemove = true;
        }

        public void RecallEnvoy()
        {
            ExpiditionState = Expedition.State.Leaving;
            foreach (var creature in Creatures)
                creature.LeaveWorld();
        }
    }
}