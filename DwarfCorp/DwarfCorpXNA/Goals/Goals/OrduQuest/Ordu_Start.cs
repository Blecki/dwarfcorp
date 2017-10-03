using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Start : Goal
    {
        public Ordu_Start()
        {
            Name = "Ordu: Uzzikal the Necromancer";
            Description = "Uzzikal, king of the Necromancers of Ordu, wishes to trade with you.";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override void Activate(WorldManager World)
        {
            Faction orduFaction = null;

            if (!World.Factions.Factions.ContainsKey("Ordu"))
            {

                // Create Ordu faction, add to FactionLibrary and World.Natives
                orduFaction = new Faction(World)
                {
                    Race = World.Factions.Races["Undead"],
                    Name = "Ordu",
                    PrimaryColor = new HSLColor(300.0, 100.0, 100.0),
                    SecondaryColor = new HSLColor(300.0, 50.0, 50.0),
                    TradeMoney = (decimal)1000.0f,
                    Center = new Microsoft.Xna.Framework.Point(MathFunctions.RandInt(0, Overworld.Map.GetLength(0)), MathFunctions.RandInt(0, Overworld.Map.GetLength(1)))
                };

                World.Factions.Factions.Add("Ordu", orduFaction);
                World.Natives.Add(orduFaction);
                World.Diplomacy.InitializeFactionPolitics(orduFaction, World.Time.CurrentDate);
            }
            else
                orduFaction = World.Factions.Factions["Ordu"];

            // Spawn trade convoy from Ordu
            World.Diplomacy.SendTradeEnvoy(orduFaction, World);
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            var trade = Event as Events.Trade;
            if (trade != null)
            {
                if (trade.OtherFaction.Name == "Ordu")
                {
                    State = GoalState.Complete;
                    World.MakeAnnouncement("Goal complete. Traded with Ordu.");
                    World.GoalManager.TryActivateGoal(World, World.GoalManager.FindGoal(typeof(Ordu_Bullion)));
                }
            }
        }
    }
}
