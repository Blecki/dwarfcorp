using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Necro_Betrayal : Goal
    {
        public int WarPartiesKilled = 0;

        public Ordu_Necro_Betrayal()
        {
            Name = "Ordu: Uzzikal's true nature";
            Description = @"Uzzikal writes, ""I cannot thank you enough for your addition to our ranks. Now I have enough undead minions to finally remove you pathetic dwarves from my land.""";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override void Activate(WorldManager World)
        {
            // Spawn multiple war parties from Ordu
            var orduFaction = World.ComponentManager.Factions.Factions.FirstOrDefault(f => f.Key == "Ordu").Value;
            var politics = World.Diplomacy.GetPolitics(World.PlayerFaction, orduFaction);
            politics.RecentEvents.Add(new Diplomacy.PoliticalEvent
            {
                Change = -100.0f,
                Duration = TimeSpan.FromDays(1000),
                Time = World.Time.CurrentDate,
                Description = "We will destroy you."
            });

            for (var i = 0; i < 5; ++i)
                World.Diplomacy.SendWarParty(orduFaction);
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            // If all war parties are killed
            var warPartyKilled = Event as Events.WarPartyDefeated;
            if (warPartyKilled != null && warPartyKilled.OtherFaction.Name == "Ordu")
                WarPartiesKilled += 1;

            if (WarPartiesKilled >= 5)
            {
                World.MakeAnnouncement("Ordu invasion defeated. Somehow, I don't think we've seen the last of them.");
                State = GoalState.Complete;
            }
        }
    }
}
