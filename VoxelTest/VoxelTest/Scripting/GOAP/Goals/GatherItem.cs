using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GatherItem : Goal
    {
        public LocatableComponent EntityToGather = null;
        public string ZoneType = "Stockpile";

        public GatherItem()
        {

        }

        public GatherItem(GOAP agent, LocatableComponent entity)
        {
            EntityToGather = entity;
            Name = "Gather Entity: " + EntityToGather.Name + " " + entity.GlobalID;
            Priority = 0.1f;
            EntityToGather = entity;
            Reset(agent);
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            return new GatherItemAct(creature, EntityToGather);
        }

        public override void Reset(GOAP agent)
        {
            Item item = new Item(EntityToGather.Name + " " + EntityToGather.GlobalID, null, EntityToGather);
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
            if(EntityToGather == null)
            {
                Priority = 0.0f;
                Cost = 999;
            }
            else
            {
                Priority = 0.1f / ((creature.Physics.GlobalTransform.Translation - EntityToGather.GlobalTransform.Translation).LengthSquared() + 0.01f) + (float) PlayState.Random.NextDouble() * 0.1f;
                Cost = (creature.Physics.GlobalTransform.Translation - EntityToGather.GlobalTransform.Translation).LengthSquared();
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            if(EntityToGather == null
               || (this.ZoneType == "Stockpile")
               || EntityToGather.IsDead
               || EntityToGather.Parent != creature.Manager.RootComponent)
            {
                if(EntityToGather == null)
                {
                    creature.Say("Gather is null");
                }

                if(EntityToGather.IsDead)
                {
                    creature.Say("Gather is dead");
                }

                if(EntityToGather.Parent != creature.Manager.RootComponent)
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