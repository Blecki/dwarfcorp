using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class BedComponent : InteractiveComponent
    {
        public BedComponent(ComponentManager manager, string name, GameComponent parent, int maxInteractions) :
            base(manager, name, parent, maxInteractions)
        {

        }

        public override bool Interact(GameComponent interactor, GameTime time)
        {
            if (!base.Interact(interactor, time))
            {
                return false;
            }
            else if (interactor is CreatureAIComponent)
            {
                CreatureAIComponent creature = (CreatureAIComponent)interactor;
                creature.Status.Energy += creature.Stats.EnergyRechargeBed * (float)time.ElapsedGameTime.TotalSeconds;
                creature.Status.Energy = Math.Min(creature.Status.Energy, 1.1f);
                return true;
            }
            else
            {
                return false;
            }


        }
    }
}
