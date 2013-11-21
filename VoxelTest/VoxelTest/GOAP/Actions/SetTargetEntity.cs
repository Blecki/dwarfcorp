using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class SetTargetEntity : Action
    {
        public Item item;

        public SetTargetEntity(Item i)
        {
            item = i;
            Name = "SetTargetEntity(" + item.ID + ")";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.None;
            PreCondition[GOAPStrings.AtTarget] = false;
            PreCondition[GOAPStrings.TargetEntity] = null;

            Effects = new WorldState();
            Effects[GOAPStrings.TargetType] = GOAP.TargetType.Entity;
            Effects[GOAPStrings.TargetEntity] = item;
            Effects[GOAPStrings.AtTarget] = false;
            Effects[GOAPStrings.TargetTags] = new TagList(item.UserData.Tags);


            Cost = 0.1f;
        }

        public override ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            if(creature == null || creature.TargetComponent != null)
            {
                return ValidationStatus.Invalid;
            }
            else if(item == null || item.UserData == null || item.UserData.IsDead)
            {
                return ValidationStatus.Invalid;
            }
            else
            {
                return base.ContextValidate(creature);
            }
        }

        public override PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            creature.TargetComponent = item.UserData;
            item.ReservedFor = creature;
            return PerformStatus.Success;
        }
    }

}