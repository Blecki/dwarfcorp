// Physics.cs
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
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// Basic physics object. When attached to an entity, it causes it to obey gravity, and collide with stuff.
    /// All objects are just axis-aligned boxes that are treated as point masses.
    /// </summary>
    public class Physics : Body, IUpdateableComponent
    {
        /// <summary>
        /// Gets or sets the angular velocity in radians per second.
        /// </summary>
        /// <value>
        /// The angular velocity.
        /// </value>
        public Vector3 AngularVelocity { get; set; }
        /// <summary>
        /// Gets or sets the linear velocity in voxels per second.
        /// </summary>
        /// <value>
        /// The velocity.
        /// </value>
        public Vector3 Velocity { get; set; }
        /// <summary>
        /// Gets or sets the mass in voxel weights.
        /// </summary>
        /// <value>
        /// The mass.
        /// </value>
        public float Mass { get; set; }
        /// <summary>
        /// Gets or sets the moment of inertia.
        /// </summary>
        /// <value>
        /// The i.
        /// </value>
        public float I { get; set; }
        /// <summary>
        /// Gets or sets the linear viscous damping force. 1.0 means no damping. 0.0 means full damping.
        /// </summary>
        /// <value>
        /// The linear damping.
        /// </value>
        public float LinearDamping { get; set; }
        /// <summary>
        /// Gets or sets the viscous angular damping force. 1.0 means no damping. 0.0 means full damping.
        /// </summary>
        /// <value>
        /// The angular damping.
        /// </value>
        public float AngularDamping { get; set; }
        /// <summary>
        /// Gets or sets the restitution in proportion. 
        /// A colliding body with 1.0 restitution bounces back with 100% of its velocity.
        /// </summary>
        /// <value>
        /// The restitution.
        /// </value>
        public float Restitution { get; set; }
        /// <summary>
        /// Gets or sets the friction when colliding with voxels.
        /// </summary>
        /// <value>
        /// The friction.
        /// </value>
        public float Friction { get; set; }
        /// <summary>
        /// Gets or sets the gravity in voxels per second squared.
        /// </summary>
        /// <value>
        /// The gravity.
        /// </value>
        public Vector3 Gravity { get; set; }
        /// <summary>
        /// Gets or sets the previous position of this body on the last update.
        /// </summary>
        /// <value>
        /// The previous position.
        /// </value>
        public Vector3 PreviousPosition { get; set; }
        /// <summary>
        /// Book keeping for applying gravity. Avoids applying gravity when it would otherwise mess up physics.
        /// </summary>
        private bool applyGravityThisFrame = true;

        private bool isSleeping = false;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is sleeping. A sleeping instance does not update
        /// unless acted upon by an outside force. (Take that, newton)
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is sleeping; otherwise, <c>false</c>.
        /// </value>
        public bool IsSleeping { get { return AllowPhysicsSleep && isSleeping;  } set { isSleeping = value; } }
        /// <summary>
        /// Book-keeping. If we are sleeping, and this is set to true, we check physics for one more frame
        /// before going back to sleep.
        /// </summary>
        private bool overrideSleepThisFrame = true;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is in liquid (like water or lava).
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is in liquid; otherwise, <c>false</c>.
        /// </value>
        public bool IsInLiquid { get; set; }
        /// <summary>
        /// Gets or sets the previous velocity. On the last update.
        /// </summary>
        /// <value>
        /// The previous velocity.
        /// </value>
        public Vector3 PreviousVelocity { get; set; }
        /// <summary>
        /// Gets or sets the orientation mode.
        /// </summary>
        /// <value>
        /// The orientation.
        /// </value>
        public OrientMode Orientation { get; set; }
        /// <summary>
        /// When in RotateY mode, this is the angle around Y in radians in which to rotate.
        /// </summary>
        private float Rotation = 0.0f;
        /// <summary>
        /// Gets or sets the collision mode. 
        /// </summary>
        /// <value>
        /// The collide mode.
        /// </value>
        public CollisionMode CollideMode { get; set; }
        /// <summary>
        /// The current voxel
        /// </summary>
        public VoxelHandle CurrentVoxel = VoxelHandle.InvalidHandle;
        /// <summary>
        /// Fixed time to update physics at. This is to prevent instabilty on very slow
        /// or very fast machines, and to protect stability during fast forward.
        /// </summary>
        public const float FixedDT = 1.0f / 60.0f;
        public const int MaxTimesteps = 5; // The maximum number of timesteps to try and calculate in a single frame.

        /// <summary>
        /// Does this physics object collide on all sides, none,
        /// just the top and bottom, or just the 2D sides?
        /// </summary>
        public enum CollisionMode
        {
            All,
            None,
            UpDown,
            Sides
        }

        /// <summary>
        /// Does this physics object rotate in accordance with
        /// physics, or is its orientation fixed? It may also look
        /// at the direction it is traveling, or may just rotate about the
        /// Y axis.
        /// </summary>
        public enum OrientMode
        {
            Physics,
            Fixed,
            LookAt,
            RotateY
        }

        // If true, the physics object will sleep when it has low velocity.
        public bool AllowPhysicsSleep = true;

        public Physics()
        {

        }

        public Physics(ComponentManager Manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, float mass, float i, float linearDamping, float angularDamping, Vector3 gravity, OrientMode orientation = OrientMode.Fixed) :
            base(Manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Mass = mass;
            Velocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
            I = i;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
            Gravity = gravity;
            Restitution = 0.01f;
            Friction = 0.99f;
            IsSleeping = false;
            PreviousPosition = LocalTransform.Translation;
            PreviousVelocity = Vector3.Zero;
            IsInLiquid = false;
            CollisionType = CollisionManager.CollisionType.Dynamic;
            CollideMode = CollisionMode.All;
            Orientation = orientation;
            SleepTimer = new Timer(5.0f, true);
            WakeTimer = new Timer(0.01f, true);
        }

        public void Move(float dt)
        {
            Vector3 newPos = new Vector3(LocalTransform.Translation.X, LocalTransform.Translation.Y, LocalTransform.Translation.Z) + Velocity * dt;
            Matrix transform = LocalTransform;
            transform.Translation = newPos;
            LocalTransform = transform;
        }
 
        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!Active) return;

            // Never apply physics when animating!
            if (AnimationQueue.Count > 0)
            {
                PropogateTransforms();
                base.Update(gameTime, chunks, camera);
                return;
            }

            if (gameTime.Speed < 0.01)
            {
                base.Update(gameTime, chunks, camera);
                return;
            }

            // How would this get a NaN anyway?
            if (MathFunctions.HasNan(Velocity))
                throw new InvalidOperationException(string.Format("Physics went haywire for object {0} : {1}", GlobalID, Name));

            if (IsSleeping)
            {
                applyGravityThisFrame = false;
            }

            bool goingSlow = Velocity.LengthSquared() < 0.05f;
            // If we're not sleeping and moving very slowly, go to sleep.
            if (AllowPhysicsSleep && !IsSleeping && goingSlow)
            {
                SleepTimer.Update(gameTime);
                if (SleepTimer.HasTriggered)
                {
                    WakeTimer.Reset();
                    Velocity = Vector3.Zero;
                    IsSleeping = true;
                }
            }
            else if (AllowPhysicsSleep && IsSleeping && !goingSlow)
            {
                WakeTimer.Update(gameTime);
                SleepTimer.Reset();
                if (WakeTimer.HasTriggered)
                {
                    IsSleeping = false;
                }
            }

            // If not sleeping, update physics.
            if (!IsSleeping || overrideSleepThisFrame)
            {
                overrideSleepThisFrame = false;

                float dt = (float)(gameTime.ElapsedGameTime.TotalSeconds);

                // Calculate the number of timesteps to apply.
                int numTimesteps = Math.Min(MaxTimesteps, Math.Max((int)(dt / FixedDT), 1));

                float velocityLength = Math.Max(Velocity.Length(), 1.0f);

                // Prepare expanded world bounds.
                BoundingBox worldBounds = chunks.Bounds;
                worldBounds.Max.Y += 50;
                // For each timestep, move and collide.
                for (int n = 0; n < numTimesteps * velocityLength; n++)
                {
                    // Move by a fixed amount.
                    Move(FixedDT / velocityLength);

                    // Get the current voxel.
                    CurrentVoxel = new VoxelHandle(chunks.ChunkData,
                        GlobalVoxelCoordinate.FromVector3(Position));

                    // Collide with the world.
                    HandleCollisions(chunks, FixedDT);

                    Matrix transform = LocalTransform;
                    // Avoid leaving the world.
                    if (worldBounds.Contains(LocalTransform.Translation + Velocity * dt) != ContainmentType.Contains)
                    {
                        transform.Translation = LocalTransform.Translation - 0.1f * Velocity * dt;
                        Velocity = new Vector3(Velocity.X * -0.9f, Velocity.Y, Velocity.Z * -0.9f);
                    }


                    // If we're outside the world, die
                    if (LocalTransform.Translation.Y < -10 ||
                        worldBounds.Contains(GetBoundingBox()) == ContainmentType.Disjoint)
                    {
                        Die();
                    }


                    // Orientation logic.
                    if (Orientation == OrientMode.Physics)
                    {
                        Matrix dA = Matrix.Identity;
                        dA *= Matrix.CreateRotationX(AngularVelocity.X * FixedDT);
                        dA *= Matrix.CreateRotationY(AngularVelocity.Y * FixedDT);
                        dA *= Matrix.CreateRotationZ(AngularVelocity.Z * FixedDT);

                        transform = dA * transform;
                    }
                    else if (Orientation != OrientMode.Fixed)
                    {
                        if (Velocity.Length() > 0.4f)
                        {
                            if (Orientation == OrientMode.LookAt)
                            {
                                Matrix newTransform =
                                    Matrix.Invert(Matrix.CreateLookAt(Position, Position + Velocity, Vector3.Down));
                                newTransform.Translation = transform.Translation;
                                transform = newTransform;
                            }
                            else if (Orientation == OrientMode.RotateY)
                            {

                                Rotation = (float)Math.Atan2(Velocity.X, -Velocity.Z);
                                Quaternion newRotation =
                                    Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationY(Rotation));
                                Quaternion oldRotation = Quaternion.CreateFromRotationMatrix(LocalTransform);
                                Quaternion finalRot = Quaternion.Slerp(oldRotation, newRotation, 0.1f);
                                finalRot.Normalize();
                                Matrix newTransform = Matrix.CreateFromQuaternion(finalRot);
                                newTransform.Translation = transform.Translation;
                                newTransform.Right.Normalize();
                                newTransform.Up.Normalize();
                                newTransform.Forward.Normalize();
                                newTransform.M14 = 0;
                                newTransform.M24 = 0;
                                newTransform.M34 = 0;
                                newTransform.M44 = 1;
                                transform = newTransform;
                            }
                        }
                    }

                    // Final check to ensure we're in the world.
                    transform.Translation = ClampToBounds(transform.Translation);
                    LocalTransform = transform;

                    // Assume that if velocity is small, we're standing on ground (lol bad assumption)
                    // Apply friction.
                    if (Math.Abs(Velocity.Y) < 0.1f)
                    {
                        Velocity = new Vector3(Velocity.X * Friction, Velocity.Y, Velocity.Z * Friction);
                    }

                    // Apply gravity.
                    if (applyGravityThisFrame)
                    {
                        ApplyForce(Gravity, FixedDT / velocityLength);
                    }

                    // Damp the velocity.
                    Vector3 dampingForce = -Velocity * (1.0f - LinearDamping);

                    Velocity += dampingForce * FixedDT;
                    AngularVelocity *= AngularDamping;

                    // These will get called next time around anyway... -@blecki
                    // No they won't @blecki, this broke everything!! -@mklingen
                    // Remove check so that it is ALWAYS called when an object moves. Call removed
                    //   from component update in ComponentManager. -@blecki
                    if (numTimesteps*velocityLength > 1)
                    {
                        // Assume all physics are attached to the root.
                        UpdateTransform();
                        UpdateBoundingBox();
                        //UpdateTransformsRecursive(Parent as Body);
                    }
                }

            } 

            applyGravityThisFrame = true;
            CheckLiquids(chunks, (float)gameTime.ElapsedGameTime.TotalSeconds);
            PreviousVelocity = Velocity;
            PreviousPosition = Position;
            base.Update(gameTime, chunks, camera);
        }

        public Timer SleepTimer { get; set; }
        public Timer WakeTimer { get; set; }

        public void Face(Vector3 target)
        {
            Vector3 diff = target - GlobalTransform.Translation;
            Matrix newTransform = Matrix.CreateRotationY((float)Math.Atan2(diff.X, -diff.Z));
            newTransform.Translation = LocalTransform.Translation;
            LocalTransform = newTransform;
        }

        public void SetPosition(Vector3 pos)
        {
            Matrix tf = LocalTransform;
            tf.Translation = pos;
            LocalTransform = tf;
        }

        public void CheckLiquids(ChunkManager chunks, float dt)
        {
            CurrentVoxel = new VoxelHandle(chunks.ChunkData,
                GlobalVoxelCoordinate.FromVector3(GlobalTransform.Translation + Vector3.Up * 0.5f));
            var below = new VoxelHandle(chunks.ChunkData,
                GlobalVoxelCoordinate.FromVector3(GlobalTransform.Translation + Vector3.Down * 0.25f));

            if (CurrentVoxel.IsValid && CurrentVoxel.WaterCell.WaterLevel > WaterManager.inWaterThreshold)
            {
                ApplyForce(new Vector3(0, 25, 0), dt);
                Velocity = new Vector3(Velocity.X * 0.9f, Velocity.Y * 0.5f, Velocity.Z * 0.9f);
            }

            if (IsInLiquid && Velocity.LengthSquared() > 0.5f)
            {
                Manager.World.ParticleManager.Trigger("splat", Position + MathFunctions.RandVector3Box(-0.5f, 0.5f, 0.1f, 0.25f, -0.5f, 0.5f), Color.White, MathFunctions.Random.Next(0, 2));
            }

            if (below.IsValid && below.WaterCell.WaterLevel > WaterManager.inWaterThreshold)
            {
                IsInLiquid = true;
            }
            else
            {
                IsInLiquid = false;
            }
        }

        public virtual void OnTerrainCollision(VoxelHandle vox)
        {
            //
        }

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch (messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    overrideSleepThisFrame = true;
                    IsSleeping = false;
                    SleepTimer.Reset();
                    HandleCollisions(World.ChunkManager, DwarfTime.Dt);
                    break;
            }


            base.ReceiveMessageRecursive(messageToReceive);
        }


        public bool Collide(BoundingBox box, float dt)
        {
            if (!BoundingBox.Intersects(box))
            {
                return false;
            }


            Contact contact = new Contact();

            if (!TestStaticAABBAABB(BoundingBox, box, ref contact))
            {
                return false;
            }

            Matrix m = LocalTransform;

            m.Translation += contact.NEnter * (contact.Penetration) * 0.5f;

            Vector3 impulse = (Vector3.Dot(Velocity, -contact.NEnter) * contact.NEnter) * 60.0f;
            Velocity += impulse * dt;
            Velocity = new Vector3(Velocity.X * Friction, Velocity.Y, Velocity.Z * Friction);

            LocalTransform = m;
            UpdateBoundingBox();
            return true;
        }

        public virtual void HandleCollisions(ChunkManager chunks, float dt)
        {
            if (CollideMode == CollisionMode.None) return;

            int y = (int)Position.Y;

            foreach (var v in VoxelHelpers.EnumerateManhattanCube(CurrentVoxel.Coordinate)
                .Select(c => new VoxelHandle(chunks.ChunkData, c)))
            {
                if (!v.IsValid || v.IsEmpty)
                    continue;

                if (CollideMode == CollisionMode.UpDown && (int)v.Coordinate.Y == y)
                {
                    continue;
                }

                if (CollideMode == CollisionMode.Sides && (int)v.Coordinate.Y != y)
                {
                    continue;
                }

                if (Collide(v.GetBoundingBox(), dt))
                {
                    OnTerrainCollision(v);
                }
            }
        }


        public struct Contact
        {
            public bool IsIntersecting;
            public Vector3 NEnter;
            public float Penetration;
        }

        public static bool TestStaticAABBAABB(BoundingBox s1, BoundingBox s2, ref Contact contact)
        {
            BoundingBox a = s1;
            BoundingBox b = s2;

            // [Minimum Translation Vector]
            float mtvDistance = float.MaxValue; // Set current minimum distance (max float value so next value is always less)
            Vector3 mtvAxis = new Vector3(); // Axis along which to travel with the minimum distance

            // [Axes of potential separation]
            // • Each shape must be projected on these axes to test for intersection:
            //          
            // (1, 0, 0)                    A0 (= B0) [X Axis]
            // (0, 1, 0)                    A1 (= B1) [Y Axis]
            // (0, 0, 1)                    A1 (= B2) [Z Axis]

            // [X Axis]
            if (!TestAxisStatic(Vector3.UnitX, a.Min.X, a.Max.X, b.Min.X, b.Max.X, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            // [Y Axis]
            if (!TestAxisStatic(Vector3.UnitY, a.Min.Y, a.Max.Y, b.Min.Y, b.Max.Y, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            // [Z Axis]
            if (!TestAxisStatic(Vector3.UnitZ, a.Min.Z, a.Max.Z, b.Min.Z, b.Max.Z, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            contact.IsIntersecting = true;

            // Calculate Minimum Translation Vector (MTV) [normal * penetration]
            contact.NEnter = Vector3.Normalize(mtvAxis);

            // Multiply the penetration depth by itself plus a small increment
            // When the penetration is resolved using the MTV, it will no longer intersect
            contact.Penetration = (float)Math.Sqrt(mtvDistance) * 1.001f;

            return true;
        }

        private static bool TestAxisStatic(Vector3 axis, float minA, float maxA, float minB, float maxB, ref Vector3 mtvAxis, ref float mtvDistance)
        {
            // [Separating Axis Theorem]
            // • Two convex shapes only overlap if they overlap on all axes of separation
            // • In order to create accurate responses we need to find the collision vector (Minimum Translation Vector)   
            // • Find if the two boxes intersect along a single axis 
            // • Compute the intersection interval for that axis
            // • Keep the smallest intersection/penetration value
            float axisLengthSquared = Vector3.Dot(axis, axis);

            // If the axis is degenerate then ignore
            if (axisLengthSquared < 1.0e-8f)
            {
                return true;
            }

            // Calculate the two possible overlap ranges
            // Either we overlap on the left or the right sides
            float d0 = (maxB - minA); // 'Left' side
            float d1 = (maxA - minB); // 'Right' side

            // Intervals do not overlap, so no intersection
            if (d0 <= 0.0f || d1 <= 0.0f)
            {
                return false;
            }

            // Find out if we overlap on the 'right' or 'left' of the object.
            float overlap = (d0 < d1) ? d0 : -d1;

            // The mtd vector for that axis
            Vector3 sep = axis * (overlap / axisLengthSquared);

            // The mtd vector length squared
            float sepLengthSquared = Vector3.Dot(sep, sep);

            // If that vector is smaller than our computed Minimum Translation Distance use that vector as our current MTV distance
            if (sepLengthSquared < mtvDistance)
            {
                mtvDistance = sepLengthSquared;
                mtvAxis = sep;
            }

            return true;
        }

        public void ApplyForce(Vector3 force, float dt)
        {
            Velocity += (force / Mass) * dt;
            IsSleeping = false;
        }

        public void ApplyTorque(Vector3 torque, float dt)
        {
            AngularVelocity += (torque / I) / dt;
            IsSleeping = false;
        }

        public Vector3 ClampToBounds(Vector3 vector3)
        {
            return MathFunctions.Clamp(vector3, Manager.World.ChunkManager.Bounds);
        }
    }

}