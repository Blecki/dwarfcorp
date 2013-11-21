using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    public class InteractiveItem : Item
    {
        public InteractiveComponent Component { get; set; }

        public WorldState UsePrecondition { get; set; }
        public WorldState UseEffects { get; set; }
        public float Cost { get; set; }

        public InteractiveItem(string id, Zone zone, LocatableComponent userData, InteractiveComponent component, float cost) :
            base(id, zone, userData)
        {
            UsePrecondition = new WorldState();
            UseEffects = new WorldState();
            this.Component = component;
            this.Cost = cost;
        }

        public virtual Action.PerformStatus Interact(CreatureAIComponent creature, GameTime time)
        {
            return Action.PerformStatus.Success;
        }
    }

}