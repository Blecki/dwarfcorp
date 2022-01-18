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
        public const float FixedDT = 1.0f / 60.0f;
        public const int MaxTimesteps = 5; // The maximum number of timesteps to try and calculate in a single frame.

        public Vector3 Velocity = Vector3.Zero;
        public float Mass { get; set; }
        public float LinearDamping { get; set; }
        public float Friction = 0.99f;
        public Vector3 Gravity { get; set; }
        
        private bool isSleeping = false;
        public bool IsSleeping { get { return AllowPhysicsSleep && isSleeping;  } set { isSleeping = value; } }
        public bool IsInLiquid = false;
        public OrientMode Orientation { get; set; }
        public CollisionMode CollideMode = CollisionMode.All;
        public VoxelHandle CurrentVoxel = VoxelHandle.InvalidHandle;
        public bool AllowPhysicsSleep = true;
        public Timer SleepTimer = new Timer(5.0f, true, Timer.TimerMode.Real);
        public Timer WakeTimer = new Timer(0.01f, true, Timer.TimerMode.Real);

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

        private VoxelHandle[] NeighborhoodVoxels = new VoxelHandle[7];

        public virtual void OnTerrainCollision(VoxelHandle vox) { }

        public Physics()
        {

        }

        public Physics(ComponentManager Manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, float mass, float i, float linearDamping, float angularDamping, Vector3 gravity, OrientMode orientation = OrientMode.Fixed) :
            base(Manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Mass = mass;
            LinearDamping = linearDamping;
            Gravity = gravity;
            IsSleeping = false;
            CollisionType = CollisionType.Dynamic;
            Orientation = orientation;
        }

        public void ApplyForce(Vector3 force, float dt)
        {
            Velocity += (force / Mass) * dt;
            IsSleeping = false;
        }

        private void Move(float dt)
        {
            Vector3 newPos = new Vector3(LocalTransform.Translation.X, LocalTransform.Translation.Y, LocalTransform.Translation.Z) + Velocity * dt;
            Matrix transform = LocalTransform;
            transform.Translation = newPos;
            LocalTransform = transform;
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
                var worldBounds = chunks.Bounds;
                worldBounds.Max.Y += 50;
                // For each timestep, move and collide.
                for (int n = 0; n < numTimesteps * velocityLength; n++)
                {
                    // Move by a fixed amount.
                    Move(FixedDT / velocityLength);

                    var previousVoxel = CurrentVoxel;
                    // Get the current voxel.
                    CurrentVoxel = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(Position));

                    // Collide with the world.
                    if (CollisionType != CollisionType.None)
                    {
                        if (CurrentVoxel != previousVoxel)
                            UpdateNeighborhoodVoxels();

                        DetectWorldCollisions();
                    }

                    var transform = LocalTransform;
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
                    transform.Translation = MathFunctions.Clamp(transform.Translation, worldBounds);
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
                    if (numTimesteps * velocityLength > 1)
                    {
                        if (Parent != null)
                            globalTransform = LocalTransform * Parent.GlobalTransform;
                        else
                            globalTransform = LocalTransform;
                        UpdateBoundingBox();
                    }
                }

                if (Orientation != OrientMode.Fixed)
                {
                    if (Velocity.Length() > 0.4f)
                    {
                        if (Orientation == OrientMode.LookAt)
                        {
                            if (Math.Abs(Vector3.Dot(Vector3.Down, Velocity)) < 0.99 * Velocity.Length())
                            {
                                var newTransform = Matrix.Invert(Matrix.CreateLookAt(LocalPosition, LocalPosition + Velocity, Vector3.Down));
                                newTransform.Translation = LocalTransform.Translation;
                                LocalTransform = newTransform;
                            }
                            else
                            {
                                var newTransform = Matrix.Invert(Matrix.CreateLookAt(LocalPosition, LocalPosition + Velocity, Vector3.Right));
                                newTransform.Translation = LocalTransform.Translation;
                                LocalTransform = newTransform;
                            }
                        }
                        else if (Orientation == OrientMode.RotateY)
                        {
                            Rotation = (float)Math.Atan2(Velocity.X, -Velocity.Z);
                            var newRotation =  Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationY(Rotation));
                            var oldRotation = Quaternion.CreateFromRotationMatrix(LocalTransform);
                            var finalRot = Quaternion.Slerp(oldRotation, newRotation, 0.1f);
                            finalRot.Normalize();
                            var newTransform = Matrix.CreateFromQuaternion(finalRot);
                            newTransform.Translation = LocalTransform.Translation;
                            newTransform.Right.Normalize();
                            newTransform.Up.Normalize();
                            newTransform.Forward.Normalize();
                            newTransform.M14 = 0;
                            newTransform.M24 = 0;
                            newTransform.M34 = 0;
                            newTransform.M44 = 1;
                            LocalTransform = newTransform;
                        }
                    }
                }
            }

            CheckLiquids((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        public void CheckLiquids(float dt)
        {
            if (CurrentVoxel.IsValid && LiquidCellHelpers.CountCellsWithWater(CurrentVoxel) > WaterManager.inWaterThreshold)
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

        public bool Collide(BoundingBox box, float dt)
        {
            if (!BoundingBox.Intersects(box))
                return false;

            var contact = new Collision.Contact();

            if (!Collision.TestStaticAABBAABB(BoundingBox, box, ref contact))
                return false;

            var m = LocalTransform;

            m.Translation += contact.NEnter * (contact.Penetration) * 0.5f;

            var impulse = (Vector3.Dot(Velocity, -contact.NEnter) * contact.NEnter) * 60.0f;
            Velocity += impulse * dt;
            Velocity = new Vector3(Velocity.X * Friction, Velocity.Y, Velocity.Z * Friction);

            LocalTransform = m;
            UpdateBoundingBox();
            return true;
        }

        private void UpdateNeighborhoodVoxels()
        {
            var i = 0;
            foreach (var v in VoxelHelpers.EnumerateManhattanCube(CurrentVoxel.Coordinate).Select(c => World.ChunkManager.CreateVoxelHandle(c)))
            {
                NeighborhoodVoxels[i] = v;
                i += 1;
            }
        }

        private void DetectWorldCollisions()
        {
            var y = (int)Position.Y;

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

            foreach (var v in NeighborhoodVoxels)
            {
                if (!v.IsValid || v.IsEmpty)
                    continue;

                if (CollideMode == CollisionMode.UpDown && (int)v.Coordinate.Y == y)
                    continue;

                if (CollideMode == CollisionMode.Sides && (int)v.Coordinate.Y != y)
                    continue;

                if (Collide(v.GetBoundingBox(), FixedDT))
                    OnTerrainCollision(v);
            }
        }

        /// <summary>
        /// Intelligently push the object out of a solid voxel.
        /// </summary>
        private void ResolveTerrainCollisionGradientMethod()
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
            var m = LocalTransform;
            m.Translation = p;
            LocalTransform = m;
            Velocity += localGradient*0.01f;
            PropogateTransforms();
            UpdateBoundingBox();
        }
    }
}