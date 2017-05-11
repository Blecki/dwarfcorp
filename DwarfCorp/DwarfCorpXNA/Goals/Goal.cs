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
        public GoalTypes GoalType = GoalTypes.Active;
        public int StockWagerCost = 0;

        public GoalState State
        {
            get { return Memory.GetState(SystemName); }
            set { Memory.SetState(SystemName, value); }
        }
        
        /// <summary>
        /// Called when the player selects a goal and activates it. Will NOT be called for
        /// achievement type goals.
        /// </summary>
        /// <param name="World"></param>
        public virtual void OnActivated(WorldManager World) { }

        /// <summary>
        /// Called when a goal is active and a game event occurs.
        /// </summary>
        /// <param name="World"></param>
        /// <param name="Event"></param>
        public virtual void OnGameEvent(WorldManager World, GameEvent Event) { }
    }
}
