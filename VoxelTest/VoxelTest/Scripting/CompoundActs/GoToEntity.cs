using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GoToEntity : CompoundCreatureAct
    {
        public LocatableComponent Entity { get; set;}

        public GoToEntity(LocatableComponent entity, CreatureAIComponent creature, float planRateLimit, int maxExpansions) :
            base(creature)
        {
            Entity = entity;
            Tree = new Sequence(new SetTargetEntityAct(entity, Agent),
                                new SetTargetVoxelFromEntityAct(Agent),
                                new PlanAct(Agent, planRateLimit, maxExpansions),
                                new FollowPathAct(Agent),
                                new StopAct(Agent, Agent.Stats.StoppingForce));
        }
    }
}
