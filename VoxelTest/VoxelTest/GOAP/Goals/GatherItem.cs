using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    internal class GatherItem : Goal
    {
        private LocatableComponent entityToGater = null;
        public string ZoneType = "Stockpile";

        public GatherItem(GOAP agent, LocatableComponent entity)
        {
            entityToGater = entity;
            Name = "Gather Entity: " + entityToGater.Name + " " + entity.GlobalID;
            Priority = 0.1f;
            entityToGater = entity;
            Reset(agent);
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            return new GatherItemAct(creature, entityToGater);
        }

        public override void Reset(GOAP agent)
        {
            Item item = new Item(entityToGater.Name + " " + entityToGater.GlobalID, null, entityToGater);
            if(agent != null)
            {
                agent.Items.Add(item);
            }

            State[GOAPStrings.TargetType] = GOAP.TargetType.None;
            State[GOAPStrings.TargetEntity] = item;
            State[GOAPStrings.TargetEntityInZone] = true;
            State[GOAPStrings.HeldObject] = null;
            State[GOAPStrings.HandState] = GOAP.HandState.Empty;
            State[GOAPStrings.AtTarget] = true;
            State[GOAPStrings.TargetDead] = false;
            State[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;
            State[GOAPStrings.TargetZoneType] = ZoneType;

            base.Reset(agent);
        }

        public override void ContextReweight(CreatureAIComponent creature)
        {
            if(entityToGater == null)
            {
                Priority = 0.0f;
                Cost = 999;
            }
            else
            {
                Priority = 0.1f / ((creature.Physics.GlobalTransform.Translation - entityToGater.GlobalTransform.Translation).LengthSquared() + 0.01f) + (float) PlayState.Random.NextDouble() * 0.1f;
                Cost = (creature.Physics.GlobalTransform.Translation - entityToGater.GlobalTransform.Translation).LengthSquared();
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            if(entityToGater == null
               || (this.ZoneType == "Stockpile")
               || entityToGater.IsDead
               || entityToGater.Parent != creature.Manager.RootComponent)
            {
                if(entityToGater == null)
                {
                    creature.Say("Gather is null");
                }

                if(entityToGater.IsDead)
                {
                    creature.Say("Gather is dead");
                }

                if(entityToGater.Parent != creature.Manager.RootComponent)
                {
                    creature.Say("Gather is attached");
                }

                if(this.ZoneType == "Stockpile")
                {
                    creature.Say("Gather is stocked");
                }

                return false;
            }
            else
            {
                return true;
            }
        }
    }

}