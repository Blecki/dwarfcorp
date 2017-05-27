using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Necro_Betrayal : Goal
    {
        public Ordu_Necro_Betrayal()
        {
            Name = "Ordu: Uzzikal's true nature";
            Description = @"Uzzikal writes, ""I cannot thank you enough for your addition to our ranks. Now I have enough undead minions to finally remove you pathetic dwarves from my land.""";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override ActivationResult Activate(WorldManager World)
        {
            // Spawn multiple war parties from Ordu

            return new ActivationResult { Succeeded = true };
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            // If all war parties are killed..
        }
    }
}
