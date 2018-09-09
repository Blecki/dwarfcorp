/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Trigger : Goal
    {
        public Ordu_Trigger()
        {
            GoalType = GoalTypes.Achievement;
            Name = "Ordu: A letter from Ordu";
            Description = "Uzzikal, king of the Necromancers of Ordu, has noticed your trading activity and sent a letter.";
        }

        public override void OnGameEvent(WorldManager World, Trigger Event)
        {
            if (Event is Triggers.Trade)
            {
                State = GoalState.Complete;
                World.GoalManager.UnlockGoal(typeof(Ordu_Start));
                World.MakeAnnouncement("New message available.");
                World.AwardBux(1);
            }
        }
    }
}*/