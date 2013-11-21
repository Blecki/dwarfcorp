using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class PickupTargetEntity : Action
    {
        public PickupTargetEntity(GOAP g)
        {
            Name = "PickupTargetEntity";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.HandState] = GOAP.HandState.Empty;
            PreCondition[GOAPStrings.HeldItemTags] = null;
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.Entity;
            PreCondition[GOAPStrings.AtTarget] = true;
            PreCondition[GOAPStrings.TargetDead] = false;
            PreCondition[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            Effects = new WorldState();
            Effects[GOAPStrings.HandState] = GOAP.HandState.Full;
            Effects[GOAPStrings.TargetType] = GOAP.TargetType.None;
            Effects[GOAPStrings.TargetTags] = null;
            Cost = 0.1f;
        }

        public override Action.ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            if(creature.TargetComponent == null || creature.Hands.IsFull() || creature.TargetComponent.IsDead)
            {
                return ValidationStatus.Invalid;
            }


            return ValidationStatus.Ok;
        }

        public override Action.PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            Item item = (Item) creature.Goap.Belief[GOAPStrings.TargetEntity];


            if(item == null)
            {
                return PerformStatus.Failure;
            }


            Zone zone = item.Zone;

            if(zone is Stockpile)
            {
                Stockpile s = (Stockpile) zone;
                LocatableComponent component = item.UserData;
                bool removed = s.RemoveItem(component);
                if(removed && component != null && !creature.Hands.IsFull())
                {
                    creature.Hands.Grab(component);
                    return PerformStatus.Success;
                }
                else
                {
                    return PerformStatus.Invalid;
                }
            }
            else if(zone is Room)
            {
                Room r = (Room) zone;
                List<LocatableComponent> components = r.GetComponentsInRoomContainingTag(item.ID);

                if(components.Count > 0)
                {
                    LocatableComponent component = r.GetComponentsInRoomContainingTag(item.ID)[0];

                    if(component != null && !creature.Hands.IsFull())
                    {
                        creature.Hands.Grab(component);
                        return PerformStatus.Success;
                    }
                    else
                    {
                        return PerformStatus.Invalid;
                    }
                }
            }
            else
            {
                LocatableComponent component = item.UserData;
                if(component != null && !creature.Hands.IsFull() && component.Parent == creature.Manager.RootComponent)
                {
                    creature.Hands.Grab(component);

                    if(creature.Master.GatherDesignations.Contains(component))
                    {
                        creature.Master.GatherDesignations.Remove(component);


                        component.DrawBoundingBox = false;
                    }
                }
                else
                {
                    return PerformStatus.Invalid;
                }
            }

            return PerformStatus.Success;
        }

        public override void Apply(WorldState state)
        {
            state[GOAPStrings.HeldObject] = state[GOAPStrings.TargetEntity];
            state[GOAPStrings.HeldItemTags] = state[GOAPStrings.TargetTags];
            Item item = (Item) (state[GOAPStrings.HeldObject]);
            if(item != null)
            {
                item.Zone = null;
            }
            base.Apply(state);
        }

        public override void UndoEffects(WorldState state)
        {
            state[GOAPStrings.HeldObject] = null;
            state[GOAPStrings.HeldItemTags] = null;
            base.UndoEffects(state);
        }

        public override void UnApply(WorldState state)
        {
            state[GOAPStrings.HeldObject] = null;
            state[GOAPStrings.HeldItemTags] = null;
            base.UnApply(state);
        }
    }

}