using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    class PutHeldItemInZone : Goal
    {
        Item entityToGater = null;
        Zone m_zone;
        public PutHeldItemInZone(GOAP agent, Zone zone)
        {
            if (agent != null)
            {
                entityToGater = (Item)agent.Belief[GOAPStrings.HeldObject];
            }
            Name = "Put Held Object in: " + m_zone;
            Priority = 0.1f;
            m_zone = zone;
            Reset(agent);
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            
            return new Sequence(new GetNearestFreeVoxelInZone(Agent.Creature, m_zone, "FreeVoxel"),
                                new GoToNamedVoxelAct("FreeVoxel", Agent.Creature),
                                new PutItemInZoneAct(Agent.Creature, m_zone));
             
             
        }

        public override void Reset(GOAP agent)
        {
            if (agent != null)
            {
                entityToGater = (Item)agent.Belief[GOAPStrings.HeldObject];
            }

            if (agent != null)
            {
                State[GOAPStrings.TargetType] = GOAP.TargetType.None;
                State[GOAPStrings.TargetEntity] = agent.Belief[GOAPStrings.HeldObject];
                entityToGater = (Item)agent.Belief[GOAPStrings.HeldObject];
                State[GOAPStrings.TargetEntityInZone] = true;
                State[GOAPStrings.CurrentZone] = m_zone;
                State[GOAPStrings.HeldObject] = null;
                State[GOAPStrings.HandState] = GOAP.HandState.Empty;
                State[GOAPStrings.AtTarget] = true;
                State[GOAPStrings.TargetDead] = false;
                State[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;
            }


            if (entityToGater != null)
            {
                agent.Items.Add(entityToGater);
            }

            base.Reset(agent);
        }

        public override void ContextReweight(CreatureAIComponent creature)
        {
            if (entityToGater == null)
            {
                Priority = 0.0f;
                Cost = 999f;
            }
            else
            {
                Priority = 0.1f / ((creature.Physics.GlobalTransform.Translation - entityToGater.userData.GlobalTransform.Translation).LengthSquared() + 0.01f) + (float)PlayState.random.NextDouble() * 0.1f;
                Cost = ((creature.Physics.GlobalTransform.Translation - entityToGater.userData.GlobalTransform.Translation).LengthSquared());
            }
        }

        public override List<Action> GetPresetPlan(CreatureAIComponent creature, GOAP agent)
        {
            List<Action> toReturn = new List<Action>();
            toReturn.Add(new SetTargetZone(creature, m_zone));
            toReturn.Add(new GoToTargetZone());
            toReturn.Add(new Stop());
            toReturn.Add(new PutHeldObjectInZone(creature, m_zone));
            return toReturn;
        }  

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            Reset(creature.Goap);
            if (entityToGater == null || entityToGater.userData.IsDead)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
