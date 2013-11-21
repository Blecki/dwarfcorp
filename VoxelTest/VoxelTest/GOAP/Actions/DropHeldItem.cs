using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    internal class DropHeldItem : Action
    {
        public DropHeldItem()
        {
            Name = "DropHeldItem";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.HandState] = GOAP.HandState.Full;

            Effects = new WorldState();
            Effects[GOAPStrings.HandState] = GOAP.HandState.Empty;
            Effects[GOAPStrings.HeldObject] = null;
            Effects[GOAPStrings.HeldItemTags] = null;

            Cost = 100f;
        }

        public override void Apply(WorldState state)
        {
            Item item = (Item) (state[GOAPStrings.HeldObject]);

            if(item != null)
            {
                state[GOAPStrings.TargetEntity] = new Item(item.ID, item.Zone, item.UserData);
            }
            else
            {
                state[GOAPStrings.TargetEntity] = null;
            }

            base.Apply(state);
        }

        public override void UnApply(WorldState state)
        {
            Item item = (Item) (state[GOAPStrings.TargetEntity]);

            if(item != null)
            {
                state[GOAPStrings.HeldObject] = new Item(item.ID, null, item.UserData);
                state[GOAPStrings.HeldItemTags] = new TagList(item.UserData.Tags);
            }
            else
            {
                state[GOAPStrings.HeldObject] = null;
                state[GOAPStrings.HeldItemTags] = null;
            }

            base.UnApply(state);
        }

        public override Action.PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            Item item = (Item) creature.Goap.Belief[GOAPStrings.HeldObject];

            if(item == null)
            {
                return Action.PerformStatus.Failure;
            }


            LocatableComponent grabbed = creature.Hands.GetFirstGrab();
            creature.Hands.UnGrab(grabbed);
            Matrix m = Matrix.Identity;
            m.Translation = creature.Physics.GlobalTransform.Translation;

            grabbed.LocalTransform = m;
            grabbed.HasMoved = true;
            grabbed.IsActive = true;


            creature.Goap.Belief[GOAPStrings.HandState] = GOAP.HandState.Empty;

            return PerformStatus.Success;
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