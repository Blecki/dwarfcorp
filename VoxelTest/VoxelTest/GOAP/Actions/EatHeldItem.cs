using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class EatHeldItem : Action
    {
        public Timer EatTimer { get; set; }
        public Timer ChewTimer { get; set; }

        public EatHeldItem()
        {
            Name = "EatHeldItem";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.HandState] = GOAP.HandState.Full;
            PreCondition[GOAPStrings.IsHungry] = true;
            PreCondition[GOAPStrings.HeldItemTags] = new TagList("Food");

            Effects = new WorldState();
            Effects[GOAPStrings.HandState] = GOAP.HandState.Empty;
            Effects[GOAPStrings.HeldObject] = null;
            Effects[GOAPStrings.HeldItemTags] = null;
            Effects[GOAPStrings.IsHungry] = false;
            Cost = 100f;
            EatTimer = new Timer(3.0f, false);
            ChewTimer = new Timer(5.0f, false);
        }


        public override Action.PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            Item item = (Item) creature.Goap.Belief[GOAPStrings.HeldObject];

            if(item == null)
            {
                return Action.PerformStatus.Failure;
            }

            if(EatTimer.HasTriggered)
            {
                LocatableComponent grabbed = creature.Hands.GetFirstGrab();
                creature.Hands.UnGrab(grabbed);

                List<FoodComponent> foods = grabbed.GetChildrenOfTypeRecursive<FoodComponent>();

                if(foods.Count > 0)
                {
                    grabbed.HasMoved = true;
                    grabbed.IsActive = true;

                    creature.Status.Hunger = Math.Max(creature.Status.Hunger - foods[0].FoodAmount, 0);
                    grabbed.Die();

                    creature.Goap.Belief[GOAPStrings.HandState] = GOAP.HandState.Empty;

                    EatTimer.Reset(EatTimer.TargetTimeSeconds);
                    return PerformStatus.Success;
                }
                else
                {
                    return PerformStatus.Invalid;
                }
            }
            else
            {
                if(ChewTimer.HasTriggered)
                {
                    SoundManager.PlaySound("ouch", creature.Physics.GlobalTransform.Translation);
                }
                else
                {
                    ChewTimer.Update(time);
                }

                EatTimer.Update(time);
                return PerformStatus.InProgress;
            }
        }

        public override ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            Item item = (Item) creature.Goap.Belief[GOAPStrings.HeldObject];

            if(item == null)
            {
                return ValidationStatus.Replan;
            }

            return ValidationStatus.Ok;
        }
    }

}