using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GoToEntityAct : CompoundCreatureAct
    {
        public LocatableComponent Entity { get; set;}

        public bool EntityIsInHands()
        {
            return Entity == Agent.Hands.GetFirstGrab();
        }

        public Condition InHands()
        {
            return new Condition(EntityIsInHands);
        }

        public GoToEntityAct(LocatableComponent entity, CreatureAIComponent creature) :
            base(creature)
        {
            Name = "Go to entity";
            Entity = entity;
            Tree = new Sequence(new SetTargetEntityAct(entity, Agent),
                                InHands() |
                                new Sequence(new SetTargetVoxelFromEntityAct(Agent),
                                new PlanAct(Agent),
                                new FollowPathAct(Agent),
                                new StopAct(Agent)));
        }
    }
}
