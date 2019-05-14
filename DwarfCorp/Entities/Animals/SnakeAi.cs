using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class SnakeAI : CreatureAI
    {
        public SnakeAI()
        {
            
        }

        public SnakeAI(ComponentManager Manager, string name, EnemySensor sensor) :
            base(Manager, name, sensor)
        {
            
        }

        public override Task ActOnIdle()
        {
            return new ActWrapperTask(new Wrap(Gogogo));
        }

        public IEnumerable<Act.Status> Gogogo()
        {
            while (true)
            {
               Creature.Physics.ApplyForce(0.1f *(Manager.World.Renderer.CursorLightPos - this.Creature.AI.Position) - 0.1f * Creature.Physics.Velocity, 1);
               yield return Act.Status.Running;
            }
        }
    }
}
