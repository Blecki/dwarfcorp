using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class FailMessage : CreatureAct
    {
        public Act SubAct;
        public String Message;

        public FailMessage(CreatureAI Creature, Act SubAct, String Message)
            : base(Creature)
        {
            this.SubAct = SubAct;
            this.Message = Message;
        }

        public override void Initialize()
        {
            SubAct.Initialize();
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            while (true)
            {
                var r = SubAct.Tick();
                if (r == Status.Fail)
                    Creature.AI.SetTaskFailureReason(Message);
                yield return r;
            }
        }
    }

}