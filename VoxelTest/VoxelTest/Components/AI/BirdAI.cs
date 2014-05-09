using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;

namespace DwarfCorp
{
    /// <summary>
    /// Extends CreatureAIComponent specifically for
    /// bird behavior.
    /// </summary>
    public class BirdAI : CreatureAIComponent
    {
        public BirdAI()
        {
            
        }

        public BirdAI(Creature creature, string name, EnemySensor sensor, PlanService planService) :
            base(creature, name, sensor, planService)
        {
            
        }

        // Overrides the default ActOnIdle so we can
        // have the bird act in any way we wish.
        public override Task ActOnIdle()
        {
            return new ActWrapperTask(new FlyWanderAct(this, 10.0f + MathFunctions.Rand() * 2.0f, 2.0f + MathFunctions.Rand() * 0.5f, 20.0f, 8.0f) 
                & new WanderAct(this, 10.0f, 3.0f + MathFunctions.Rand() * 0.5f, 1.0f));
        }
    }
}
