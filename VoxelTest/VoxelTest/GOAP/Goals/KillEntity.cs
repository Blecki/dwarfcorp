using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    class KillEntity : Goal
    {
        LocatableComponent entityToKill = null;
        public Item Item { get; set; }
        public KillEntity(GOAP agent, LocatableComponent entity)
        {
            Name = "Kill Entity: " + entity.Name + " " + entity.GlobalID;
            Priority = 0.1f;
            entityToKill = entity;
            Reset(agent);
        }

        public override void Reset(GOAP agent)
        {
            Item = new Item(entityToKill.Name + " " + entityToKill.GlobalID, null, entityToKill);

            if (agent != null)
            {
                agent.Items.Add(Item);
            }

            State[GOAPStrings.TargetType] = GOAP.TargetType.Entity;
            State[GOAPStrings.TargetDead] = true;
            State[GOAPStrings.TargetEntity] = Item;
            State[GOAPStrings.AtTarget] = true;
            State[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;
            base.Reset(agent);
        }


        public override void ContextReweight(CreatureAIComponent creature)
        {
            if (entityToKill == null)
            {
                Priority = 0.0f;
            }
            else
            {
                Priority = 0.1f / ((creature.Physics.GlobalTransform.Translation - entityToKill.GlobalTransform.Translation).LengthSquared() + 0.01f) + (float)PlayState.random.NextDouble() * 0.1f;
                Cost = ((creature.Physics.GlobalTransform.Translation - entityToKill.GlobalTransform.Translation).LengthSquared());
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            if (entityToKill == null || entityToKill.IsDead)
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
