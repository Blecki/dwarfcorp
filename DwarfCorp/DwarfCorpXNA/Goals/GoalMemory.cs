using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals
{
    public class GoalMemory
    {
        private Dictionary<String, GoalState> States = new Dictionary<string, GoalState>();
        private Dictionary<String, int> Memory = new Dictionary<string, int>();

        public GoalState GetState(String Name)
        {
            if (States.ContainsKey(Name)) return States[Name];
            return GoalState.Unavailable;
        }

        public void SetState(String Name, GoalState State)
        {
            if (States.ContainsKey(Name)) States[Name] = State;
            else States.Add(Name, State);
        }
    }
}
