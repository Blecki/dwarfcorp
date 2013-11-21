using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    internal class ClearBlackboardData : CreatureAct
    {
        private string DataKey { get; set; }

        public ClearBlackboardData(CreatureAIComponent creature, string data) :
            base(creature)
        {
            Name = "Clear " + data;
            DataKey = data;
        }

        public override IEnumerable<Status> Run()
        {
            if(DataKey == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Agent.Blackboard.Erase(DataKey);
                yield return Status.Success;
            }
        }
    }

}