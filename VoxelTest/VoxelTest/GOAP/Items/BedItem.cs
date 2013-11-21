using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    internal class BedItem : InteractiveItem
    {
        public BedItem(string id, Zone zone, LocatableComponent userData, InteractiveComponent component, float cost) :
            base(id, zone, userData, component, cost)
        {
            UseEffects[GOAPStrings.IsSleepy] = false;
            UsePrecondition[GOAPStrings.IsSleepy] = true;
            UsePrecondition[GOAPStrings.AtTarget] = true;
            UsePrecondition[GOAPStrings.TargetEntity] = this;
        }

        public override Action.PerformStatus Interact(CreatureAIComponent creature, GameTime time)
        {
            if(Component.Interact(creature, time))
            {
                creature.InteractingWith = Component;
                creature.TargetComponent = UserData;

                if(creature.Status.Energy > 1.0)
                {
                    Component.InteractingComponents.Remove(creature);
                    return Action.PerformStatus.Success;
                }
                else
                {
                    return Action.PerformStatus.InProgress;
                }
            }
            else
            {
                return Action.PerformStatus.Failure;
            }
        }
    }

}