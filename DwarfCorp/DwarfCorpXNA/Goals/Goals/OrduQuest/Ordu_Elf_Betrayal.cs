using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Elf_Betrayal : Goal
    {
        public int WarPartiesKilled = 0;

        public Ordu_Elf_Betrayal()
        {
            Name = "Ordu: Blinny's revenge";
            Description = @"Blinny, chief of the Fel'al'fe, sent a dove with the following message attached: ""Did you think we would forgive you so easily? Now that the necromancers are no more, we have no use for you.""";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override void Activate(WorldManager World)
        {
            // Spawn multiple war parties from Fel'al'fe
            var felFaction = World.ComponentManager.Factions.Factions.FirstOrDefault(f => f.Key == "Fel'al'fe").Value;
            var politics = World.Diplomacy.GetPolitics(World.PlayerFaction, felFaction);
            politics.RecentEvents.Add(new Diplomacy.PoliticalEvent
            {
                Change = -100.0f,
                Duration = TimeSpan.FromDays(1000),
                Time = World.Time.CurrentDate,
                Description = "We will destroy you."
            });

            for (var i = 0; i < 5; ++i)
                World.Diplomacy.SendWarParty(felFaction);
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            // If all war parties are killed
            var warPartyKilled = Event as Events.WarPartyDefeated;
            if (warPartyKilled != null && warPartyKilled.OtherFaction.Name == "Fel'al'fe")
                WarPartiesKilled += 1;

            if (WarPartiesKilled >= 5)
            {
                World.MakeAnnouncement("Fel'al'fe invasion defeated. Somehow, I don't think we've seen the last of them.");
                State = GoalState.Complete;
            }
        }
    }
}
