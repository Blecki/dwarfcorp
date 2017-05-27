using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Elf_Betrayal : Goal
    {
        public Ordu_Elf_Betrayal()
        {
            Name = "Ordu: Blinny's revenge";
            Description = @"Blinny, chief of the Fel'al'fe, sent a dove with the following message attached: ""Did you think we would forgive you so easily? Now that the necromancers are no more, we have no use for you.""";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override ActivationResult Activate(WorldManager World)
        {
            // Spawn multiple war parties from Fel'al'fe

            return new ActivationResult { Succeeded = true };
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            // If all war parties are killed
        }
    }
}
