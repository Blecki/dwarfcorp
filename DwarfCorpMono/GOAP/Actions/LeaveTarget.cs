using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class LeaveTarget : Action
    {
        public LeaveTarget()
        {
            Name = "LeaveTarget";

            PreCondition = new WorldState();
            PreCondition[GOAPStrings.AtTarget] = true;

            Effects = new WorldState();
            Effects[GOAPStrings.AtTarget] = false;

            Cost = 1.0f;
        }
    }
}
