using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Elf_Invasion : Goal
    {
        public Ordu_Elf_Invasion()
        {
            Name = "Ordu: Siding with the Undead";
            Description = "Uzzikal urges you to stay the course and prepare for an elf invasion. He admits that he may be evil, but reminds you that you ambushed a caravan of unsuspecting traders. You're at least as evil as he.";
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
