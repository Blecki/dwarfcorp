using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Elf_Invasion : Goal
    {
        public int WarPartiesKilled = 0;

        public Ordu_Elf_Invasion()
        {
            Name = "Ordu: Siding with the Undead";
            Description = "Uzzikal urges you to stay the course and prepare for an elf invasion. He admits that he may be evil, but reminds you that you ambushed a caravan of unsuspecting traders. You're at least as evil as he.";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override void Activate(WorldManager World)
        {
            var otherChoice = World.GoalManager.FindGoal(typeof(Ordu_Necro_Envoy));
            otherChoice.State = GoalState.Unavailable;

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
                World.MakeAnnouncement("Fel'al'fe invasion defeated. Uzzikal has news.");
                World.GoalManager.UnlockGoal(typeof(Ordu_Necro_Betrayal));
                State = GoalState.Complete;
            }
        }
    }
}
