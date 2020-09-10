using System.Collections.Generic;

namespace DwarfCorp
{
    public class BirdAI : CreatureAI
    {
        public BirdAI()
        {
            
        }

        public BirdAI(ComponentManager Manager, string name, EnemySensor sensor) 
            :  base(Manager, name, sensor)
        {
            
        }

        IEnumerable<Act.Status> ChirpRandomly()
        {
            Timer chirpTimer = new Timer(MathFunctions.Rand(6f, 10f), false, Timer.TimerMode.Real);

            while (true)
            {
                chirpTimer.Update(FrameDeltaTime);

                if (chirpTimer.HasTriggered)
                    Creature.NoiseMaker.MakeNoise("chirp", Creature.AI.Position, true, 0.5f);

                yield return Act.Status.Running;
            }
        }

        public override Task ActOnIdle()
        {
            return new ActWrapperTask(
                new Parallel(
                    new FlyWanderAct(this, 10.0f + MathFunctions.Rand() * 2.0f, 2.0f + MathFunctions.Rand() * 0.5f, 20.0f, 8.0f, MathFunctions.Rand() * 10.0f), 
                    new Wrap(ChirpRandomly)
                )
                {
                    ReturnOnAllSucces = false,
                    Name = "Fly"
                });
        }
    }
}
