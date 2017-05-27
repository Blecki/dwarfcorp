using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Necro_Invasion : Goal
    {
        public Ordu_Necro_Invasion()
        {
            Name = "Ordu: Siding with the Elves";
            Description = "Blinny, Chief Fel'al'fe, urges you to reconsider your aggression toward his people. The Necromancers are evil, he says. Side with him, and Uzzikal will be displeased.";
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
