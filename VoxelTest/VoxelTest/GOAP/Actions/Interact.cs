using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    public class Interact : Action
    {
        public InteractiveItem item;

        public Interact(InteractiveItem i)
        {
            item = i;
            Name = "Interact(" + item.ID + ")";
            PreCondition = item.UsePrecondition;
            Effects = item.UseEffects;
            Cost = item.Cost;
        }

        public override PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            return item.Interact(creature, time);
        }
    }

}