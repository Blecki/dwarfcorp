using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    class ClearBlackboardData : CreatureAct
    {
        string DataKey { get; set; }
        public ClearBlackboardData(CreatureAIComponent creature, string data) :
            base(creature)
        {
            DataKey = data;
        }

        public override IEnumerable<Status> Run()
        {
            if (DataKey == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Agent.Blackboard.Erase(DataKey);
            }
        }

    }
}
