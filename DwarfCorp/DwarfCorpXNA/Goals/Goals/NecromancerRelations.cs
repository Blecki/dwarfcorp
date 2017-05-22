using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;

namespace DwarfCorp.Goals.Goals
{
    public class NR_Start : Goal
    {
        public NR_Start()
        {
            Name = "Uzzikal the Necromancer";
            Description = "Uzzikal, king of the Necromancers of Urdu, wishes to trade with you.";
            GoalType = GoalTypes.AvailableAtStartup;
        }

        public override ActivationResult Activate(WorldManager World)
        {
            // Create Urdu faction, add to FactionLibrary and World.Natives
            var urduFaction = new Faction(World)
            {
                Race = World.ComponentManager.Factions.Races["Undead"],
                Name = "Urdu",
                PrimaryColor = new HSLColor(300.0, 100.0, 100.0),
                SecondaryColor = new HSLColor(300.0, 50.0, 50.0),
                TradeMoney = (decimal)1000.0f,
                Center = new Microsoft.Xna.Framework.Point(MathFunctions.RandInt(0, Overworld.Map.GetLength(0)), MathFunctions.RandInt(0, Overworld.Map.GetLength(1)))
            };

            World.ComponentManager.Factions.Factions.Add("Urdu", urduFaction);
            World.Natives.Add(urduFaction);
            World.ComponentManager.Diplomacy.InitializeFactionPolitics(urduFaction, World.Time.CurrentDate);

            // Spawn trade convoy from Urdu
            // Does nothing because playstate is not active state. :(
            World.ComponentManager.Diplomacy.SendTradeEnvoy(urduFaction, World);

            return new ActivationResult { Succeeded = true };
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            var trade = Event as Events.Trade;
            if (trade != null)
            {
                if (trade.OtherFaction.Name == "Urdu")
                {
                    State = GoalState.Complete;
                    World.MakeAnnouncement("Goal complete. Traded with Urdu.");
                    World.GoalManager.TryActivateGoal(World, World.GoalManager.FindGoal("Bullion for the Dead"));
                }
            }
        }
    }

    public class NR_Bullion : Goal
    {
        private DwarfBux Gold;
        private Gum.Widget CustomGUI;

        public NR_Bullion()
        {
            Name = "Bullion for the Dead";
            Description = @"You received a letter from Uzzikal the Necromancer. ""Dear Fleshy Dwarf, I am certain you hate the Elves as much as I do. We have traded favorably in the past, so I am coming to you for aid. We need gold to press our advantage against those toy-making fools. We are willing to trade for it, but we need 500 gold."" So, it looks like all you have to do is trade the Undead 500 gold.";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override void BuildCustomGUI(Widget Widget)
        {
            base.BuildCustomGUI(Widget);

            CustomGUI = Widget;
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            if (State == GoalState.Complete) return;
            var trade = Event as Events.Trade;
            if (trade != null && trade.OtherFaction.Name == "Urdu")
            {
                Gold += trade.PlayerGold - trade.OtherGold;
                if (Gold >= 500)
                {
                    State = GoalState.Complete;
                    World.MakeAnnouncement("Goal met. You have traded 500 gold to Uzzikal.");
                }

                if (CustomGUI != null) CustomGUI.Text = Description + "\n" + ((Gold >= 500) ? "Goal met!" :
                        String.Format("{0}/500", Gold));
            }
        }
    }
}
