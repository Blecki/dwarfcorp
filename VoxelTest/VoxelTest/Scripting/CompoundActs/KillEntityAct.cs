using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class KillEntityAct : CompoundCreatureAct
    {
        public LocatableComponent Entity { get; set; }

        public KillEntityAct(LocatableComponent entity, CreatureAIComponent creature) :
            base(creature)
        {
            Entity = entity;
            Name = "Kill Entity";
            Tree = new Sequence(new GoToEntityAct(entity, creature),
                                new MeleeAct(Agent));
        }
    }
}
