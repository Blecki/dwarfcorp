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
    [JsonObject(IsReference =true)]
    public class TradeEnvoy : Expedition
    {
        public TradeEnvoy()
        {

        }

        public TradeEnvoy(DateTime date) : base(date)
        {
            WaitForTradeTimer = new DateTimer(date, new TimeSpan(0, 6, 0, 0, 0));
        }

        public void StartTrading(DateTime date)
        {
            WaitForTradeTimer = new DateTimer(date, new TimeSpan(0, 6, 0, 0, 0));
        }

        public DwarfBux TradeMoney { get; set; }
        public List<ResourceAmount> TradeGoods { get; set; }
        public DateTimer WaitForTradeTimer = null;
        public DwarfBux TributeDemanded = 0m;
        [JsonIgnore] public WorldPopup TradeWidget = null;

        public bool IsTradeWidgetValid()
        {
            return TradeWidget != null && TradeWidget.BodyToTrack != null && !TradeWidget.BodyToTrack.IsDead;
        }

        public void MakeTradeWidget(WorldManager World)
        {
            var liveCreatures = Creatures.Where(creature => creature != null && !creature.IsDead);
            if (!liveCreatures.Any())
            {
                return;
            }

            var zones = World.PlayerFaction.RoomBuilder.DesignatedRooms.OfType<BalloonPort>();

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
            {
                closestCreature = liveCreatures.First();
            }

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

            var politics = World.Diplomacy.GetPolitics(OtherFaction, OwnerFaction);
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
            if (Creatures.Count == 0) return;
            int goodsPerCreature = TradeGoods.Count / Creatures.Count;
            int currentGood = 0;
            foreach (CreatureAI creature in Creatures)
            {
                ResourcePack pack = creature.GetRoot().GetComponent<ResourcePack>();
                if (pack != null)
                {
                    pack.Contents.Resources.Clear();
                    for (int i = currentGood; i < global::System.Math.Min(currentGood + goodsPerCreature, TradeGoods.Count); i++)
                    {
                        pack.Contents.AddResource(TradeGoods[i]);
                    }
                    currentGood += goodsPerCreature;
                }
            }
        }
    }
}