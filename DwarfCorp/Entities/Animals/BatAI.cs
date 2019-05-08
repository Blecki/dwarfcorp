using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class BatAI : CreatureAI
    {
        public BatAI()
        {

        }

        public BatAI(ComponentManager Manager, string name, EnemySensor sensor) :
            base(Manager, name, sensor)
        {

        }

        private IEnumerable<Act.Status> ChirpRandomly()
        {
            var chirpTimer = new Timer(MathFunctions.Rand(6f, 10f), false);
            while (true)
            {
                chirpTimer.Update(DwarfTime.LastTime);
                if (chirpTimer.HasTriggered)
                    Creature.NoiseMaker.MakeNoise("Chirp", Creature.AI.Position, true, 0.01f);
                yield return Act.Status.Running;
            }
        }

        public override Task ActOnIdle()
        {
            return new ActWrapperTask(
                new Parallel(
                    new FlyWanderAct(this, 10.0f + MathFunctions.Rand() * 2.0f, 2.0f + MathFunctions.Rand() * 0.5f, 20.0f, 4.0f + MathFunctions.Rand() * 2, 10.0f)
                    {
                        CanPerchOnGround = false,
                        CanPerchOnWalls = true
                    },
                    new Wrap(ChirpRandomly))
                {
                    ReturnOnAllSucces = false,
                    Name = "Fly"
                });
        }
    }
}
