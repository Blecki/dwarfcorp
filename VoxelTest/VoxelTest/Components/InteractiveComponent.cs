using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class InteractiveComponent : GameComponent
    {
        public List<GameComponent> InteractingComponents { get; set; }
        public int MaxInteractions { get; set; }

        public InteractiveComponent(ComponentManager manager, string name, GameComponent parent, int maxInteractions) :
            base(manager, name, parent)
        {
            InteractingComponents = new List<GameComponent>();
            MaxInteractions = maxInteractions;
        }

        public virtual bool Interact(GameComponent interactor, GameTime time)
        {
            if(InteractingComponents.Contains(interactor))
            {
                return true;
            }

            if(InteractingComponents.Count < MaxInteractions)
            {
                InteractingComponents.Add(interactor);
                return true;
            }

            return false;
        }
    }

}