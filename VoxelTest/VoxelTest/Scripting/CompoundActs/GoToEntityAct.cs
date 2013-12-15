using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToEntityAct : CompoundCreatureAct
    {
        public LocatableComponent Entity { get; set; }

        public GoToEntityAct()
        {

        }

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
                new Sequence(new SetTargetVoxelFromEntityAct(Agent, "EntityVoxel"),
                    new PlanAct(Agent, "PathToEntity", "EntityVoxel"),
                    new FollowPathAct(Agent, "PathToEntity"),
                    new StopAct(Agent)));
        }
    }

}