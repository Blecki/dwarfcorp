using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;

namespace DwarfCorp.Goals.Goals
{
    /*
    public class MineSand : Goal
    {
        public int Counter = 0;

        public MineSand()
        {
            Name = "Mine Sand";
            Description = "Mine 10 Sand";
            GoalType = GoalTypes.AvailableAtStartup;
        }

        public override void CreateGUI(Widget Widget)
        {
            if (State == GoalState.Available)
                Widget.Text = Description + "\nActivation cost: $500.";
            else if (State == GoalState.Active)
                Widget.Text = Description + String.Format("\n{0} of 10 mined.", Counter);
            else
                Widget.Text = Description + "\nAwarded $1000.";
        }

        public override ActivationResult CanActivate(WorldManager World)
        {
            if (World.PlayerCompany.Assets < 500)
                return new ActivationResult
                {
                    ErrorMessage = "You do not have enough $.",
                    Succeeded = false
                };
            else
            {
                return new ActivationResult
                {
                    Succeeded = true
                };
            }
        }

        public override void Activate(WorldManager World)
        {
                World.LoseBux(5);
        }

        public override void OnGameEvent(WorldManager World, GameEvent Event)
        {
            if (State == GoalState.Complete) return;
            if (Event is Events.DigBlock && (Event as Events.DigBlock).VoxelType.Name == "Sand")
            {
                Counter += 1;
                if (Counter == 10)
                {
                    World.MakeAnnouncement(Description + "\nSuccessfully met sand mining goal!");
                    World.AwardBux(1000);
                    State = GoalState.Complete;
                }
            }
        }
    }
     */
}
