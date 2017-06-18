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
        public Timer PerchTime { get; set; }
        public float Radius { get; set; }
        public float Altitude { get; set; }
        public float YRadius { get; set; }
        public float GravityCompensation { get; set; }
        public float Damping { get; set; }
        public float TurnThreshold { get; set; }
        public Vector3 LocalTarget { get; set; }
        public Vector3 OriginalGravity { get; set; }

        public bool CanPerchOnWalls { get; set; }
        public bool CanPerchOnGround { get; set; }
        public bool CanPerchOnObjects { get; set; }

        public FlyState State { get; set; }

        public enum FlyState
        {
            Wandering,
            SearchingForPerch,
            Perching
        }

        public FlyWanderAct()
        {

        }

        public FlyWanderAct(CreatureAI creature, float seconds, float turnTime, float radius, float altitude, float perchTime) :
            base(creature)
        {
            Altitude = altitude;
            Name = "FlyWander " + seconds;
            WanderTime = new Timer(seconds, false);
            TurnTime = new Timer(turnTime, false);
            PerchTime = new Timer(perchTime, false);
            Radius = radius;
            YRadius = 2.0f;
            GravityCompensation = 0.1f;
            Damping = 0.25f;
            TurnThreshold = 2.0f;
            OriginalGravity = creature.Physics.Gravity;
            CanPerchOnWalls = false;
            CanPerchOnGround = true;
            CanPerchOnObjects = true;
            State = FlyState.Wandering;
            
        }

        public override void Initialize()
        {
            WanderTime.Reset(WanderTime.TargetTimeSeconds);
            TurnTime.Reset(TurnTime.TargetTimeSeconds);
            base.Initialize();
        }

        public override void OnCanceled()
        {
            Agent.Creature.Physics.Gravity = OriginalGravity;
            Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            PerchTime.Reset();
            WanderTime.Reset();
            TurnTime.Reset();
            while (true)
            {
                if (State == FlyState.Perching)
                {
                    PerchTime.Reset();
                    while (!PerchTime.HasTriggered)
                    {
                        Agent.Creature.Physics.Velocity = Vector3.Zero;
                        Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
                        PerchTime.Update(DwarfTime.LastTime);
                        yield return Act.Status.Running;
                    }
                    // When we're done flying, go back to walking and just fall.
                    Agent.Creature.CurrentCharacterMode = CharacterMode.Walking;
                    Agent.Creature.Physics.Gravity = OriginalGravity;
                    yield return Act.Status.Success;
                }

                Agent.Creature.Physics.Gravity = Vector3.Zero;
                // Store the last position of the bird to sample from
                Vector3 oldPosition = Agent.Position;

                // Get the height of the terrain beneath the bird.
                float surfaceHeight = Agent.Chunks.ChunkData.GetFilledVoxelGridHeightAt(oldPosition.X, oldPosition.Y,
                    oldPosition.Z);

                // Immediately start flying.
                Agent.Creature.CurrentCharacterMode = CharacterMode.Flying;

                // Use this to determine when to start turning.
                float currentDistance = 999;

                {
                    // Pick a target within a box floating some distance above the surface.
                    float randomX = MathFunctions.Rand()*Radius - Radius/2.0f;
                    float randomZ = MathFunctions.Rand()*Radius - Radius/2.0f;
                    float randomY = (float) MathFunctions.Random.NextDouble()*YRadius + Altitude + surfaceHeight;

                    // Set the target to that random location.
                    LocalTarget = new Vector3(randomX + oldPosition.X, randomY, randomZ + oldPosition.Z);
                }

                
                // Keep flying until a timer has trigerred.
                while ((!WanderTime.HasTriggered && State == FlyState.Wandering) || (State == FlyState.SearchingForPerch))
                {
                    // If we hit the ground, switch to walking, otherwise switch to flying.
                    Agent.Creature.CurrentCharacterMode = CharacterMode.Flying;

                    WanderTime.Update(DwarfTime.LastTime);

                    // If we're near a target, or a timeout occured, pick a new ranodm target.
                    if (TurnTime.Update(DwarfTime.LastTime) || TurnTime.HasTriggered || currentDistance < TurnThreshold)
                    {
                        // Pick a target within a box floating some distance above the surface.
                        float randomX = MathFunctions.Rand()*Radius - Radius/2.0f;
                        float randomZ = MathFunctions.Rand()*Radius - Radius/2.0f;
                        float randomY = (float) MathFunctions.Random.NextDouble()*YRadius + Altitude + surfaceHeight;

                        // Set the target to that random location.
                        LocalTarget = new Vector3(randomX + oldPosition.X, randomY, randomZ + oldPosition.Z);
                    }

                    // Set the current distance to the target so we know when to go to a new target.
                    currentDistance = (Agent.Position - LocalTarget).Length();

                    // Output from the force controller.
                    Vector3 output =
                        Creature.Controller.GetOutput((float) DwarfTime.LastTime.ElapsedGameTime.TotalSeconds,
                            LocalTarget, Creature.Physics.GlobalTransform.Translation);

                    // We apply a linear combination of the force controller and the 
                    // feed forward force to the bird to make it lazily turn around and fly.
                    Creature.Physics.ApplyForce(output*Damping*GravityCompensation,
                        (float) DwarfTime.LastTime.ElapsedGameTime.TotalSeconds);


                    if (State == FlyState.Wandering && WanderTime.HasTriggered)
                    {
                        State = FlyState.SearchingForPerch;
                    }

                    if (State == FlyState.SearchingForPerch)
                    {
                        Voxel vox = Creature.Physics.CurrentVoxel;

                        if (vox.WaterLevel > 0)
                        {
                            yield return Act.Status.Running;
                            continue;
                        }

                        if (CanPerchOnGround)
                        {
                            Creature.Physics.ApplyForce(OriginalGravity, (float)DwarfTime.LastTime.ElapsedGameTime.TotalSeconds);
                            Voxel below = vox.GetVoxelBelow();

                            if (below != null && !below.IsEmpty && below.WaterLevel == 0)
                            {
                                State = FlyState.Perching;
                                continue;
                            }
                        }

                        if (CanPerchOnWalls)
                        {
                            foreach (Voxel n in Creature.Physics.Neighbors)
                            {
                                if (n != null && n.GridPosition.Y >= vox.GridPosition.Y && !n.IsEmpty)
                                {
                                    State = FlyState.Perching;
                                    continue;
                                }
                            }
                        }

                        /*
                        if (CanPerchOnObjects)
                        {
                            List<Body> objetcs = new List<Body>();
                            PlayState.ComponentManager.GetBodiesIntersecting(Creature.Physics.BoundingBox, objetcs, CollisionManager.CollisionType.Static);

                            if (objetcs.Count > 0)
                            {
                                State = FlyState.Perching;
                                continue;
                            }
                        }
                         */
                        
                    }

                    yield return Status.Running;

                }

                yield return Status.Running;
            }
        }
    }

}
