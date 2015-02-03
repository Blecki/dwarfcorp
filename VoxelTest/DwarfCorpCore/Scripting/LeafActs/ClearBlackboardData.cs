using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Erases a specific named value from the blackboard.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class ClearBlackboardData : CreatureAct
    {
        private string DataKey { get; set; }

        public ClearBlackboardData(CreatureAI creature, string data) :
            base(creature)
        {
            Name = "Clear " + data;
            DataKey = data;
        }

        public override IEnumerable<Status> Run()
        {
            return Creature.ClearBlackboardData(DataKey);
        }
    }

}