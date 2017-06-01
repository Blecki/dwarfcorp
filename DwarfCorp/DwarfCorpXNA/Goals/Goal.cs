using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp.Goals
{
    public class Goal
    {
        [JsonIgnore]
        public String Name = "Generic Goal";

        [JsonIgnore]
        public String Description = "This goal was not properly configured.";

        [JsonIgnore]
        public GoalTypes GoalType = GoalTypes.UnavailableAtStartup;

        public GoalState State = GoalState.Unavailable;
        
        public struct ActivationResult
        {
            public bool Succeeded;
            public String ErrorMessage;
        }

        /// <summary>
        /// Called when the player selects a goal and activates it. Will NOT be called for
        /// achievement type goals.
        /// </summary>
        /// <param name="World"></param>
        public virtual ActivationResult Activate(WorldManager World)
        {
            return new ActivationResult { Succeeded = true };
        }

        /// <summary>
        /// Called when a goal is active and a game event occurs.
        /// </summary>
        /// <param name="World"></param>
        /// <param name="Event"></param>
        public virtual void OnGameEvent(WorldManager World, GameEvent Event) { }

        /// <summary>
        /// Called to create a custom GUI for the goal.
        /// </summary>
        /// <param name="Widget"></param>
        public virtual void CreateGUI(Gum.Widget Widget)
        {
            Widget.Text = Description;
        }
    }
}
