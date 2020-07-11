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
    public class Physics : GameComponent
    {
        public Vector3 Velocity { get; set; }
        public float Mass { get; set; }
        public float LinearDamping { get; set; }
        public float Friction { get; set; }
        public Vector3 Gravity { get; set; }
        public Vector3 PreviousPosition { get; set; }
        
        private bool isSleeping = false;
        public bool IsSleeping { get { return AllowPhysicsSleep && isSleeping;  } set { isSleeping = value; } }
        public bool IsInLiquid { get; set; }
        public Vector3 PreviousVelocity { get; set; }
        public OrientMode Orientation { get; set; }
        public CollisionMode CollideMode { get; set; }
        public VoxelHandle CurrentVoxel = VoxelHandle.InvalidHandle;
        public const float FixedDT = 1.0f / 60.0f;
        public const int MaxTimesteps = 5; // The maximum number of timesteps to try and calculate in a single frame.
        public bool AllowPhysicsSleep = true;
        public Timer SleepTimer { get; set; }
        public Timer WakeTimer { get; set; }

        private float Rotation = 0.0f;
        private bool overrideSleepThisFrame = true;

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

        private VoxelHandle[] neighborHood = new VoxelHandle[7];
        private VoxelHandle prevVoxel = VoxelHandle.InvalidHandle;
        private bool queryNeighborhood = true;
        public Physics()
        {

        }

        public Physics(ComponentManager Manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, float mass, float i, float linearDamping, float angularDamping, Vector3 gravity, OrientMode orientation = OrientMode.Fixed) :
            base(Manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Mass = mass;
            Velocity = Vector3.Zero;
            LinearDamping = linearDamping;
            Gravity = gravity;
            Friction = 0.99f;
            IsSleeping = false;
            PreviousPosition = LocalTransform.Translation;
            PreviousVelocity = Vector3.Zero;
            IsInLiquid = false;
            CollisionType = CollisionType.Dynamic;
            CollideMode = CollisionMode.All;
            Orientation = orientation;
            SleepTimer = new Timer(5.0f, true, Timer.TimerMode.Real);
            WakeTimer = new Timer(0.01f, true, Timer.TimerMode.Real);
        }

        public void Move(float dt)
        {
            Vector3 newPos = new Vector3(LocalTransform.Translation.X, LocalTransform.Translation.Y, LocalTransform.Translation.Z) + Velocity * dt;
            Matrix transform = LocalTransform;
            transform.Translation = newPos;
            LocalTransform = transform;
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

        }

        public void PhysicsUpdate(DwarfTime gameTime, ChunkManager chunks)
        { 
            if (!Active)
                return;

            // Never apply physics when animating!
            if (AnimationQueue.Count > 0)
            {
                Velocity = Vector3.Zero;
                return;
            }

            if (gameTime.Speed < 0.01) // This a poor man's IsPaused? Does this even get called if paused?
                return;

            // How would this get a NaN anyway?
            if (MathFunctions.HasNan(Velocity))
                Velocity = Vector3.Zero;

            if (AllowPhysicsSleep)
            {
                bool goingSlow = Velocity.LengthSquared() < 0.05f;
                // If we're not sleeping and moving very slowly, go to sleep.

                if (!IsSleeping && goingSlow)
                {
                    WakeTimer.Reset();
                    SleepTimer.Update(gameTime);
                    if (SleepTimer.HasTriggered)
                    {
                        WakeTimer.Reset();
                        Velocity = Vector3.Zero;
                        IsSleeping = true;
                    }
                }
                else if (IsSleeping && !goingSlow)
                {
                    WakeTimer.Update(gameTime);
                    SleepTimer.Reset();
                    if (WakeTimer.HasTriggered)
                    {
                        IsSleeping = false;
                    }
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

                    prevVoxel = CurrentVoxel;
                    // Get the current voxel.
                    CurrentVoxel = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(Position));

                    if (CurrentVoxel != prevVoxel)
                        queryNeighborhood = true;

                    // Collide with the world.
                    if (CollisionType != CollisionType.None)
                        HandleCollisions(queryNeighborhood, neighborHood, chunks, FixedDT);
                    queryNeighborhood = false;
                    Matrix transform = LocalTransform;
                    // Avoid leaving the world.
                    if (worldBounds.Contains(LocalTransform.Translation + Velocity * dt) != ContainmentType.Contains)
                    {
                        transform.Translation = LocalTransform.Translation - 0.1f * Velocity * dt;
                        Velocity = new Vector3(Velocity.X * -0.9f, Velocity.Y, Velocity.Z * -0.9f);
                    }


                    // If we're outside the world, die
                    if (LocalTransform.Translation.Y < -10 || worldBounds.Contains(GetBoundingBox()) == ContainmentType.Disjoint)
                        Die();

                    // Final check to ensure we're in the world.
                    transform.Translation = ClampToBounds(transform.Translation);
                    LocalTransform = transform;

                    // Assume that if velocity is small, we're standing on ground (lol bad assumption)
                    // Apply friction.
                    if (Math.Abs(Velocity.Y) < 0.1f)
                        Velocity = new Vector3(Velocity.X * Friction, Velocity.Y, Velocity.Z * Friction);

                    // Apply gravity.
                    ApplyForce(Gravity, FixedDT / velocityLength);

                    // Damp the velocity.
                    var dampingForce = -Velocity * (1.0f - LinearDamping);

                    Velocity += dampingForce * FixedDT;

                    // These will get called next time around anyway... -@blecki
                    // No they won't @blecki, this broke everything!! -@mklingen
                    // Remove check so that it is ALWAYS called when an object moves. Call removed
                    //   from component update in ComponentManager. -@blecki
                    if (numTimesteps*velocityLength > 1)
                    {
                        // Assume all physics are attached to the root.
                        if (Parent != null)
                            globalTransform = LocalTransform * Parent.GlobalTransform;
                        else
                            globalTransform = LocalTransform;
                        UpdateBoundingBox();
                    }
                }



                if (Orientation != OrientMode.Fixed)
                {
                    Matrix transform = LocalTransform;
                    if (Velocity.Length() > 0.4f)
                    {
                        if (Orientation == OrientMode.LookAt)
                        {
                            if (Math.Abs(Vector3.Dot(Vector3.Down, Velocity)) < 0.99 * Velocity.Length())
                            {
                                Matrix newTransform =
                                    Matrix.Invert(Matrix.CreateLookAt(LocalPosition, LocalPosition + Velocity, Vector3.Down));
                                newTransform.Translation = transform.Translation;
                                transform = newTransform;
                            }
                            else
                            {
                                {
                                    Matrix newTransform =
                                        Matrix.Invert(Matrix.CreateLookAt(LocalPosition, LocalPosition + Velocity, Vector3.Right));
                                    newTransform.Translation = transform.Translation;
                                    transform = newTransform;
                                }
                            }
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
                    LocalTransform = transform;
                }

            }

            CheckLiquids(chunks, (float)gameTime.ElapsedGameTime.TotalSeconds);
            PreviousVelocity = Velocity;
            PreviousPosition = Position;
        }

        public void CheckLiquids(ChunkManager chunks, float dt)
        {
            var currentVoxel = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(GlobalTransform.Translation));

            if (currentVoxel.IsValid && currentVoxel.LiquidLevel > WaterManager.inWaterThreshold)
            {
                ApplyForce(new Vector3(0, 25, 0), dt);
                Velocity = new Vector3(Velocity.X * 0.9f, Velocity.Y * 0.5f, Velocity.Z * 0.9f);
                IsInLiquid = true;
            }
            else
                IsInLiquid = false;

            if (IsInLiquid && Velocity.LengthSquared() > 0.5f)
                Manager.World.ParticleManager.Trigger("splat", Position + MathFunctions.RandVector3Box(-0.5f, 0.5f, 0.1f, 0.25f, -0.5f, 0.5f), Color.White, MathFunctions.Random.Next(0, 2));
        }

        public virtual void OnTerrainCollision(VoxelHandle vox)
        {
            //
        }

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            //switch (messageToReceive.Type)
            //{
            //    case Message.MessageType.OnChunkModified:
            //        overrideSleepThisFrame = true;
            //        IsSleeping = false;
            //        SleepTimer.Reset();
            //        HandleCollisions(true, neighborHood, World.ChunkManager, DwarfTime.Dt);
            //        queryNeighborhood = false;
            //        break;
            //}


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

        public virtual void HandleCollisions(bool queryNeighborHood, VoxelHandle[] neighborHood, ChunkManager chunks, float dt)
        {
            if (CollideMode == CollisionMode.None) return;

            int y = (int)Position.Y;

            if (CurrentVoxel.IsValid && !CurrentVoxel.IsEmpty)
            {
                var currentBox = CurrentVoxel.GetBoundingBox();
                if (currentBox.Contains(Position) == ContainmentType.Contains)
                {
                    ResolveTerrainCollisionGradientMethod();
                    OnTerrainCollision(CurrentVoxel);
                    return;
                }
            }

            if (queryNeighborHood)
            {
                int i = 0;
                foreach(var v in VoxelHelpers.EnumerateManhattanCube(CurrentVoxel.Coordinate).Select(c => new VoxelHandle(chunks, c)))
                {
                    neighborHood[i] = v;
                    i++;
                }
            }

            foreach (var v in neighborHood)
            {
                if (!v.IsValid || v.IsEmpty)
                    continue;

                if (CollideMode == CollisionMode.UpDown && (int)v.Coordinate.Y == y)
                    continue;

                if (CollideMode == CollisionMode.Sides && (int)v.Coordinate.Y != y)
                    continue;

                if (Collide(v.GetBoundingBox(), dt))
                    OnTerrainCollision(v);
            }
        }

// This is a more expensive terrain collision method that has fewer problems than the box-collision method.
        // It works by stepping the physics object along the gradient of the terrain field until it is out of collision.
        // It will only work if the object is on the edge of the terrain (i.e exactly one voxel in or less).
        public void ResolveTerrainCollisionGradientMethod()
        {
            Vector3 localGradient = Vector3.Zero;

            var p = Position;
            localGradient = Position - CurrentVoxel.Center;
            localGradient += Velocity; // Prefer to push in the direction we're already going.
            foreach (var v in VoxelHelpers.EnumerateManhattanNeighbors(CurrentVoxel.Coordinate))
            {
                var handle = new VoxelHandle(World.ChunkManager, v);
                var sign = (handle.IsValid && handle.IsEmpty) ? -1 : 1;
                localGradient += (p - handle.Center) * sign;
            }

            p += localGradient*0.01f;
            Matrix m = LocalTransform;
            m.Translation = p;
            LocalTransform = m;
            Velocity += localGradient*0.01f;
            PropogateTransforms();
            UpdateBoundingBox();
        }

        public struct Contact
        {
            public bool IsIntersecting;
            public Vector3 NEnter;
            public float Penetration;
        }


        public static bool TestStaticAABBAABB(BoundingBox s1, BoundingBox s2, ref Contact contact, bool testX, bool testY, bool testZ)
        {
            if (!testX && !testY && !testZ)
            {
                return false;
            }

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
            if (testX && !TestAxisStatic(Vector3.UnitX, a.Min.X, a.Max.X, b.Min.X, b.Max.X, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            // [Y Axis]
            if (testY && !TestAxisStatic(Vector3.UnitY, a.Min.Y, a.Max.Y, b.Min.Y, b.Max.Y, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            // [Z Axis]
            if (testZ && !TestAxisStatic(Vector3.UnitZ, a.Min.Z, a.Max.Z, b.Min.Z, b.Max.Z, ref mtvAxis, ref mtvDistance))
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

        private static bool TestAxisStaticSigned(Vector3 axis, float minA, float maxA, float minB, float maxB, ref Vector3 mtvAxis, ref float mtvDistance, bool positive)
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

            ////
            // minA ----- max A
            //  |---------d0-------|
            //       |--d1--|
            //      minB -------- maxB
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

            if (!positive)
            {
                overlap = -d1;
            }

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

        public Vector3 ClampToBounds(Vector3 vector3)
        {
            return MathFunctions.Clamp(vector3, Manager.World.ChunkManager.Bounds);
        }
    }
}