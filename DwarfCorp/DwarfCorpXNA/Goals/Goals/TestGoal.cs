using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Goals
{
    public class TestGoal : Goal
    {
        public override void OnCreation()
        {
            Name = "Test Goal";
            Description = "This goal exists to test the goal system.";
        }
    }
}
