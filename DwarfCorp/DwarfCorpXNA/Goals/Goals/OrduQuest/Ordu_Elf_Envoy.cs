using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Elf_Envoy : Goal
    {
        public Ordu_Elf_Envoy()
        {
            Name = "Ordu: The Fel'al'fe Envoy";
            Description = "A trade envoy from the Fel'al'fe is on its way. Uzzikal of Ordu asks you to join him in his quest to destroy the elves. You can start by declaring war on this envoy, and destroying it.";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override void Activate(WorldManager World)
        {
            // Create Ordu faction, add to FactionLibrary and World.Natives
            var felFaction = new Faction(World)
            {
                Race = World.Factions.Races["Elf"],
                Name = "Fel'al'fe",
                PrimaryColor = new HSLColor(120.0, 100.0, 100.0),
                SecondaryColor = new HSLColor(300.0, 50.0, 50.0),
                TradeMoney = (decimal)1000.0f,
                Center = new Microsoft.Xna.Framework.Point(MathFunctions.RandInt(0, Overworld.Map.GetLength(0)), MathFunctions.RandInt(0, Overworld.Map.GetLength(1)))
            };

            World.Factions.Factions.Add("Fel'al'fe", felFaction);
            World.Natives.Add(felFaction);
            World.Diplomacy.InitializeFactionPolitics(felFaction, World.Time.CurrentDate);

            // Spawn trade convoy from Fel
            World.Diplomacy.SendTradeEnvoy(felFaction, World);
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            var tradeEnvoyKilled = Event as Events.TradeEnvoyKilled;
            if (tradeEnvoyKilled != null && tradeEnvoyKilled.OtherFaction.Name == "Fel'al'fe")
            { 
                State = GoalState.Complete;
                World.MakeAnnouncement("Uzzikal approves of your actions against the elves.");
                World.GoalManager.UnlockGoal(typeof(Ordu_Necro_Envoy));
                World.GoalManager.UnlockGoal(typeof(Ordu_Elf_Invasion));
            }
        }
    }
}
