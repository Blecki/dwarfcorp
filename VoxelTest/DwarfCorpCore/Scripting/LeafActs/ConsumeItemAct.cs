using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature eats an item in its hands to satisfy its hunger.
    /// </summary>
    public class ConsumeItemAct : CreatureAct
    {
        public string TargetID = "HeldObject";

        public ConsumeItemAct(string itemtoConsume)
        {
            TargetID = itemtoConsume;
        }


        public ConsumeItemAct()
        {
            
        }

        public ConsumeItemAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Consume " + TargetID;
        } 

        public override IEnumerable<Status> Run()
        {
            yield return Status.Fail;  //Creature.ConsumeItem(TargetID);
        }
    }
}
