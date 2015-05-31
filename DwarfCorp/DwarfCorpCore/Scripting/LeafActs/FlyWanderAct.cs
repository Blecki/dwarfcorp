// FlyWanderAct.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature randomly applies force at intervals to itself.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class FlyWanderAct : CreatureAct
    {
        public Timer WanderTime { get; set; }
        public Timer TurnTime { get; set; }
        public float Radius { get; set; }
        public float Altitude { get; set; }
        public float YRadius { get; set; }
        public float GravityCompensation { get; set; }
        public float Damping { get; set; }
        public float TurnThreshold { get; set; }
        public Vector3 LocalTarget { get; set; }
        public FlyWanderAct()
        {

        }

        public FlyWanderAct(CreatureAI creature, float seconds, float turnTime, float radius, float altitude) :
            base(creature)
        {
            Altitude = altitude;
            Name = "FlyWander " + seconds;
            WanderTime = new Timer(seconds, false);
            TurnTime = new Timer(turnTime, false);
            Radius = radius;
            YRadius = 2.0f;
            GravityCompensation = 0.1f;
            Damping = 0.25f;
            TurnThreshold = 2.0f;
        }

        public override void Initialize()
        {
            WanderTime.Reset(WanderTime.TargetTimeSeconds);
            TurnTime.Reset(TurnTime.TargetTimeSeconds);
            base.Initialize();
        }


        public override IEnumerable<Status> Run()
        {
            // Store the last position of the bird to sample from
            Vector3 oldPosition = Agent.Position;

            // Get the height of the terrain beneath the bird.
            float surfaceHeight = Agent.Chunks.ChunkData.GetFilledVoxelGridHeightAt(oldPosition.X, oldPosition.Y, oldPosition.Z);
            
            // Immediately start flying.
            Agent.Creature.CurrentCharacterMode = Creature.CharacterMode.Flying;
            
            // Use this to determine when to start turning.
            float currentDistance = 999;

            {
                // Pick a target within a box floating some distance above the surface.
                float randomX = MathFunctions.Rand()*Radius - Radius/2.0f;
                float randomZ = MathFunctions.Rand()*Radius - Radius/2.0f;
                float randomY = (float) PlayState.Random.NextDouble()*YRadius + Altitude + surfaceHeight;

                // Set the target to that random location.
                LocalTarget = new Vector3(randomX + oldPosition.X, randomY, randomZ + oldPosition.Z);
            }

            // Keep flying until a timer has trigerred.
            while (!WanderTime.HasTriggered)
            {
                // If we hit the ground, switch to walking, otherwise switch to flying.
                Agent.Creature.CurrentCharacterMode = Creature.IsOnGround ? Creature.CharacterMode.Walking : Creature.CharacterMode.Flying;

                WanderTime.Update(DwarfTime.LastTime);

                // If we're near a target, or a timeout occured, pick a new ranodm target.
                if (TurnTime.Update(DwarfTime.LastTime) || TurnTime.HasTriggered || currentDistance < TurnThreshold)
                {
                    // Pick a target within a box floating some distance above the surface.
                    float randomX = MathFunctions.Rand() * Radius - Radius / 2.0f;
                    float randomZ = MathFunctions.Rand() * Radius - Radius / 2.0f;
                    float randomY = (float)PlayState.Random.NextDouble() * YRadius + Altitude + surfaceHeight;

                    // Set the target to that random location.
                    LocalTarget = new Vector3(randomX + oldPosition.X, randomY, randomZ + oldPosition.Z);
                }

                // Set the current distance to the target so we know when to go to a new target.
                currentDistance = (Agent.Position - LocalTarget).Length();

                // Output from the force controller.
                Vector3 output = Creature.Controller.GetOutput((float)DwarfTime.LastTime.ElapsedGameTime.TotalSeconds, LocalTarget, Creature.Physics.GlobalTransform.Translation);
                
                // Feed forward term to cancel gravity.
                Vector3 feedForward = -Agent.Physics.Gravity;

                // We apply a linear combination of the force controller and the 
                // feed forward force to the bird to make it lazily turn around and fly.
                Creature.Physics.ApplyForce(output * Damping + feedForward * GravityCompensation, (float)DwarfTime.LastTime.ElapsedGameTime.TotalSeconds);
                yield return Status.Running;
            }

            // When we're done flying, go back to walking and just fall.
            Agent.Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
            yield return Status.Success;
        }
    }

}
