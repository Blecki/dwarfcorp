using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Goals
{
    public class MineSand : Goal
    {
        private int Counter = 0;

        public MineSand()
        {
            Name = "Mine Sand";
            Description = "Mine 10 Sand";
            GoalType = GoalTypes.AvailableAtStartup;
        }

       

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            if (State == GoalState.Complete) return;
            if (Event is Events.DigBlock && (Event as Events.DigBlock).VoxelType.Name == "Sand")
            {
                Counter += 1;
                if (Counter == 10)
                {
                    World.MakeAnnouncement("Successfully met sand mining goal!");
                    State = GoalState.Complete;
                }
            }
        }

    }
}
