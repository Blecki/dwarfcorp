using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Goals
{
    public class TestGoal : Goal
    {
        public TestGoal()
        {
            Name = "Test Goal";
            Description = "This goal exists to test the goal system.";
            GoalType = GoalTypes.Achievement;
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            if (Event.EventDescription.StartsWith("built room Stockpile"))
            {
                World.MakeAnnouncement("HOLY SHIT YOU MET A GOAL!");
                State = GoalState.Complete;
            }
        }
    }
}
