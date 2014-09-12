using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Extends CreatureAI specifically for
    /// bird behavior.
    /// </summary>
    public class SnakeAI : CreatureAI
    {
        public SnakeAI()
        {
            
        }

        public SnakeAI(Creature creature, string name, EnemySensor sensor, PlanService planService) :
            base(creature, name, sensor, planService)
        {
            
        }

        // Overrides the default ActOnIdle so we can
        // have the bird act in any way we wish.
        public override Task ActOnIdle()
        {
            return new ActWrapperTask(new Wrap(Gogogo));
        }

        public IEnumerable<Act.Status> Gogogo()
        {
            while (true)
            {
                
               Creature.Physics.ApplyForce(0.1f *(PlayState.CursorLightPos - this.Creature.AI.Position) - 0.1f * Creature.Physics.Velocity, 1);
               yield return Act.Status.Running;
            }
        }
    }
}
