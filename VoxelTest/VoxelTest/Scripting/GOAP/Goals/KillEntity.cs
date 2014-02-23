using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class KillEntity : Goal
    {
        public LocatableComponent EntityToKill = null;
        public Item Item { get; set; }

        public KillEntity(GOAP agent, LocatableComponent entity)
        {
            Name = "Kill Entity: " + entity.Name + " " + entity.GlobalID;
            Priority = 0.1f;
            EntityToKill = entity;
            Reset(agent);
        }

        public override void Reset(GOAP agent)
        {
            Item = new Item(EntityToKill.Name + " " + EntityToKill.GlobalID, null, EntityToKill);

            if(agent != null)
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
            if(EntityToKill == null)
            {
                Priority = 0.0f;
            }
            else
            {
                Priority = 0.1f / ((creature.Physics.GlobalTransform.Translation - EntityToKill.GlobalTransform.Translation).LengthSquared() + 0.01f) + (float) PlayState.Random.NextDouble() * 0.1f;
                Cost = ((creature.Physics.GlobalTransform.Translation - EntityToKill.GlobalTransform.Translation).LengthSquared());
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            if(EntityToKill == null || EntityToKill.IsDead)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            return new KillEntityAct(EntityToKill, creature);
        }
    }

}