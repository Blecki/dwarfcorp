using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Necro_Envoy : Goal
    {
        public Ordu_Necro_Envoy()
        {
            Name = "Ordu: Siding with the Elves";
            Description = "Blinny, Chief Fel'al'fe, urges you to reconsider your aggression toward his people. The Necromancers are evil, he says. You can prove your alliance by destroying an envoy from Ordu. Uzzikal will be displeased.";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override ActivationResult Activate(WorldManager World)
        {
            var otherChoice = World.GoalManager.FindGoal(typeof(Ordu_Elf_Invasion));
            otherChoice.State = GoalState.Unavailable;

            var orduFaction = World.ComponentManager.Factions.Factions.FirstOrDefault(f => f.Key == "Ordu").Value;
            World.ComponentManager.Diplomacy.SendTradeEnvoy(orduFaction, World);

            return new ActivationResult { Succeeded = true };
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            var tradeEnvoyKilled = Event as Events.TradeEnvoyKilled;
            if (tradeEnvoyKilled != null && tradeEnvoyKilled.OtherFaction.Name == "Fel'al'fe")
            {
                State = GoalState.Complete;
                World.MakeAnnouncement("Blinny is pleased that you reconsidered.");
                World.GoalManager.UnlockGoal(typeof(Ordu_Necro_Invasion));
            }
        }
    }
}
