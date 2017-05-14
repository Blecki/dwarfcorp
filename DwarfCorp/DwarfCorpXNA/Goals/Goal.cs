using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals
{
    public class Goal
    {
        public GoalMemory Memory;
        public String SystemName;
        
        public String Name = "Generic Goal";
        public String Description = "This goal was not properly configured.";
        public GoalTypes GoalType = GoalTypes.UnavailableAtStartup;
        public int StockWagerCost = 0;

        public GoalState State
        {
            get { return Memory.GetState(SystemName); }
            set { Memory.SetState(SystemName, value); }
        }
        
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
    }
}
