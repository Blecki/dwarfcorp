using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should do something (anything) since there 
    /// is nothing else to do.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class LookInterestingTask : Task
    {
        public LookInterestingTask()
        {
            Name = "LookInteresting";
        }

        public override Act CreateScript(Creature creature)
        {
            return new WanderAct(creature.AI, 2, 0.5f, 1.0f);
        }

        public override float ComputeCost(Creature agent)
        {
            return 1.0f;
        }
    }

}