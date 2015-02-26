﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;

namespace DwarfCorp
{
    /// <summary>
    /// Extends CreatureAI specifically for
    /// bird behavior.
    /// </summary>
    public class BirdAI : CreatureAI
    {
        public BirdAI()
        {
            
        }

        public BirdAI(Creature creature, string name, EnemySensor sensor, PlanService planService) :
            base(creature, name, sensor, planService)
        {
            
        }

        IEnumerable<Act.Status> ChirpRandomly()
        {
            Timer chirpTimer = new Timer(MathFunctions.Rand(6f, 10f), false);
            while (true)
            {
                chirpTimer.Update(DwarfTime.LastTime);
                if (chirpTimer.HasTriggered)
                {
                    Creature.NoiseMaker.MakeNoise("chirp", Creature.AI.Position, true, 0.5f);
                }
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }


        // Overrides the default ActOnIdle so we can
        // have the bird act in any way we wish.
        public override Task ActOnIdle()
        {
            return new ActWrapperTask(
                new Parallel(new FlyWanderAct(this, 10.0f + MathFunctions.Rand() * 2.0f, 2.0f + MathFunctions.Rand() * 0.5f, 20.0f, 8.0f)
                & new WanderAct(this, 10.0f, 3.0f + MathFunctions.Rand() * 0.5f, 1.0f), new Wrap(ChirpRandomly)) {ReturnOnAllSucces = false, Name = "Fly"});
        }
    }
}
