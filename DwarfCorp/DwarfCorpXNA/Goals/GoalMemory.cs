using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals
{
    public class GoalMemory
    {
        private Dictionary<String, GoalState> GoalStates;

        public GoalState GetState(String Name)
        {
            if (GoalStates.ContainsKey(Name)) return GoalStates[Name];
            return GoalState.Unavailable;
        }

        public void SetState(String Name, GoalState State)
        {
            if (GoalStates.ContainsKey(Name)) GoalStates[Name] = State;
            else GoalStates.Add(Name, State);
        }
    }
}
