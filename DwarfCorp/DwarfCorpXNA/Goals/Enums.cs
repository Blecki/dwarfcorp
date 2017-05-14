using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals
{
    public enum GoalTypes
    {
        Achievement,
        UnavailableAtStartup,
        AvailableAtStartup
    }

    public enum GoalState
    {
        Unavailable,
        Available,
        Active,
        Complete
    }
}
