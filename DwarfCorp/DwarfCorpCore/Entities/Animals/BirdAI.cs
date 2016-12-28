// BirdAI.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;

namespace DwarfCorp
{
    /// <summary>
    ///     Extends CreatureAI specifically for
    ///     bird behavior.
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

        private IEnumerable<Act.Status> ChirpRandomly()
        {
            var chirpTimer = new Timer(MathFunctions.Rand(6f, 10f), false);
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


        /// <summary> Tell the bird to fly around randomly.</summary>
        public override Task ActOnIdle()
        {
            return new ActWrapperTask(
                new Parallel(
                    new FlyWanderAct(this, 10.0f + MathFunctions.Rand()*2.0f, 2.0f + MathFunctions.Rand()*0.5f, 20.0f,
                        8.0f, 10.0f)
                    , new Wrap(ChirpRandomly)) {ReturnOnAllSucces = false, Name = "Fly"});
        }
    }
}