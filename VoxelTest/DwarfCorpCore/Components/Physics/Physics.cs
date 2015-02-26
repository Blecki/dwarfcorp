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
    public class Physics : Body
    {
        public Vector3 AngularVelocity { get; set; }
        public Vector3 Velocity { get; set; }
        public float Mass { get; set; }
        public float I { get; set; }
        public float LinearDamping { get; set; }
        public float AngularDamping { get; set; }
        public float Restitution { get; set; }
        public float Friction { get; set; }
        public Vector3 Gravity { get; set; }
        public Vector3 PreviousPosition { get; set; }
        private bool applyGravityThisFrame = true;
        public bool IsSleeping { get; set; }
        private bool overrideSleepThisFrame = true;
        public bool IsInLiquid { get; set; }
        public Vector3 PreviousVelocity { get; set; }
        public OrientMode Orientation { get; set; }
        private float Rotation = 0.0f;
        public enum OrientMode
        {
            Physics,
            Fixed,
            LookAt,
            RotateY
        }

        public Physics()
        {
            
        }

        public Physics(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, float mass, float i, float linearDamping, float angularDamping, Vector3 gravity, OrientMode orientation = OrientMode.Fixed) :
            base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
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
            Orientation = orientation;
            SleepTimer = new Timer(5.0f, true);
        }

        public void MoveX(float dt)
        {
            Vector3 newPos = new Vector3(LocalTransform.Translation.X + Velocity.X * dt, LocalTransform.Translation.Y, LocalTransform.Translation.Z);
            Matrix transform = LocalTransform;
            transform.Translation = newPos;
            LocalTransform = transform;
        }

        public void MoveY(float dt)
        {
            Vector3 newPos = new Vector3(LocalTransform.Translation.X, LocalTransform.Translation.Y + Velocity.Y * dt, LocalTransform.Translation.Z);
            Matrix transform = LocalTransform;
            transform.Translation = newPos;
            LocalTransform = transform;
        }

        public void MoveZ(float dt)
        {
            Vector3 newPos = new Vector3(LocalTransform.Translation.X, LocalTransform.Translation.Y, LocalTransform.Translation.Z + Velocity.Z * dt);
            Matrix transform = LocalTransform;
            transform.Translation = newPos;
            LocalTransform = transform;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            BoundingBox bounds = chunks.Bounds;
            bounds.Max.Y += 50;
            if (!IsSleeping && (Velocity).Length() < 0.15f)
            {
                SleepTimer.Update(gameTime);
                if (SleepTimer.HasTriggered)
                {
                    applyGravityThisFrame = false;
                    Velocity *= 0.0f;
                    IsSleeping = true;
                }

            }
            else 
            {
                SleepTimer.Reset();
                IsSleeping = false;
            }

            if(!IsSleeping || overrideSleepThisFrame)
            {
                if(overrideSleepThisFrame)
                {
                    overrideSleepThisFrame = false;
                }

                float dt = (float)(gameTime.ElapsedGameTime.TotalSeconds);

                MoveY(dt);
                MoveX(dt);
                MoveZ(dt);
                HandleCollisions(chunks, dt);

                Matrix transform = LocalTransform;
                if (bounds.Contains(LocalTransform.Translation + Velocity*dt) != ContainmentType.Contains)
                {
                    transform.Translation = LocalTransform.Translation - 0.1f * Velocity * dt;
                    Velocity = new Vector3(Velocity.X * -0.9f, Velocity.Y, Velocity.Z * -0.9f);
                }


                if (LocalTransform.Translation.Z < -10 || bounds.Contains(GetBoundingBox()) == ContainmentType.Disjoint)
                {
                    Die();
                }


                if(Orientation == OrientMode.Physics)
                {
                    Matrix dA = Matrix.Identity;
                    dA *= Matrix.CreateRotationX(AngularVelocity.X * dt);
                    dA *= Matrix.CreateRotationY(AngularVelocity.Y * dt);
                    dA *= Matrix.CreateRotationZ(AngularVelocity.Z * dt);

                    transform = dA * transform;
                }
                else if(Orientation != OrientMode.Fixed)
                {
                    if(Velocity.Length() > 0.4f)
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
              
                            Rotation = (float) Math.Atan2(Velocity.X, -Velocity.Z);
                            Quaternion newRotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationY(Rotation));
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

                transform.Translation = ClampToBounds(transform.Translation);
                LocalTransform = transform;

                if(Math.Abs(Velocity.Y) < 0.1f)
                {
                    Velocity = new Vector3(Velocity.X * Friction, Velocity.Y, Velocity.Z * Friction);
                }

                if (applyGravityThisFrame)
                {
                    ApplyForce(Gravity, dt);
                }
                else
                {
                    applyGravityThisFrame = true;
                }

                Velocity *= LinearDamping;
                AngularVelocity *= AngularDamping;
                UpdateBoundingBox();
                CheckLiquids(chunks, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            Velocity = (PreviousVelocity * 0.1f + Velocity * 0.9f);
            PreviousVelocity = Velocity;
            PreviousPosition = Position;
            base.Update(gameTime, chunks, camera);
        }

        public Timer SleepTimer { get; set; }

        public void Face(Vector3 target)
        {
            Vector3 diff = target - GlobalTransform.Translation;
            Matrix newTransform = Matrix.CreateRotationY((float) Math.Atan2(diff.X, -diff.Z));
            newTransform.Translation = LocalTransform.Translation;
            LocalTransform = newTransform;
        }

        public void CheckLiquids(ChunkManager chunks, float dt)
        {
            Voxel currentVoxel = new Voxel();
            bool success = chunks.ChunkData.GetVoxel(GlobalTransform.Translation, ref currentVoxel);
            
            if(success && currentVoxel.Water.WaterLevel > 5)
            {
                IsInLiquid = true;
                ApplyForce(new Vector3(0, 25, 0), dt);
                Velocity = new Vector3(Velocity.X * 0.9f, Velocity.Y * 0.5f, Velocity.Z * 0.9f);
            }
            else
            {
                IsInLiquid = false;
            }
        }

        public virtual void OnTerrainCollision(Voxel vox)
        {
            //
        }

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch(messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    overrideSleepThisFrame = true;
                    break;
            }


            base.ReceiveMessageRecursive(messageToReceive);
        }


        public bool Collide(BoundingBox box)
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
            m.Translation += contact.NEnter * (contact.Penetration) * 1.01f;

            Vector3 impulse = (Vector3.Dot(Velocity, -contact.NEnter)*contact.NEnter);
            Velocity += impulse;
            //Vector3 newVelocity = (contact.NEnter * Vector3.Dot(Velocity, contact.NEnter));
            //Velocity = (Velocity - newVelocity) * Restitution;
            /*
            if (Math.Abs(contact.NEnter.Y) > 0.1f)
            {
                Velocity = new Vector3(Velocity.X, -Velocity.Y * Restitution, Velocity.Z);
            }
            else
            {
                Velocity = Vector3.Reflect(Velocity, -contact.NEnter);
                Velocity = new Vector3(Velocity.X * Friction, Velocity.Y, Velocity.Z * Friction);
            }
             */

            LocalTransform = m;
            UpdateBoundingBox();

            return true;
        }


        public virtual void HandleCollisions(ChunkManager chunks, float dt)
        {
            Voxel currentVoxel = new Voxel();
            bool success = chunks.ChunkData.GetVoxel(null, LocalTransform.Translation, ref currentVoxel);

            List<Voxel> vs = new List<Voxel>
            {
                currentVoxel
            };

            VoxelChunk chunk = chunks.ChunkData.GetVoxelChunkAtWorldLocation(LocalTransform.Translation);


            if (!success || currentVoxel == null || chunk == null)
            {
                return;
            }

            Vector3 grid = chunk.WorldToGrid(LocalTransform.Translation);
            
            List<Voxel> adjacencies = chunk.GetNeighborsEuclidean((int) grid.X, (int) grid.Y, (int) grid.Z);
            vs.AddRange(adjacencies);
            Vector3 half = Vector3.One*0.5f;
            vs.Sort((a, b) => (MathFunctions.L1(LocalTransform.Translation, a.Position + half).CompareTo(MathFunctions.L1(LocalTransform.Translation, b.Position + half))));
            foreach(Voxel v in vs)
            {
                if(v == null || v.IsEmpty)
                {
                    continue;
                }

                BoundingBox voxAABB = v.GetBoundingBox();
                if (Collide(voxAABB))
                {
                    OnTerrainCollision(v);
                }
            }
        }


        public class Contact
        {
            public bool IsIntersecting = false;
            public Vector3 NEnter = Vector3.Zero;
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
            if(!TestAxisStatic(Vector3.UnitX, a.Min.X, a.Max.X, b.Min.X, b.Max.X, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            // [Y Axis]
            if(!TestAxisStatic(Vector3.UnitY, a.Min.Y, a.Max.Y, b.Min.Y, b.Max.Y, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            // [Z Axis]
            if(!TestAxisStatic(Vector3.UnitZ, a.Min.Z, a.Max.Z, b.Min.Z, b.Max.Z, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            contact.IsIntersecting = true;

            // Calculate Minimum Translation Vector (MTV) [normal * penetration]
            contact.NEnter = Vector3.Normalize(mtvAxis);

            // Multiply the penetration depth by itself plus a small increment
            // When the penetration is resolved using the MTV, it will no longer intersect
            contact.Penetration = (float) Math.Sqrt(mtvDistance) * 1.001f;

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
            if(axisLengthSquared < 1.0e-8f)
            {
                return true;
            }

            // Calculate the two possible overlap ranges
            // Either we overlap on the left or the right sides
            float d0 = (maxB - minA); // 'Left' side
            float d1 = (maxA - minB); // 'Right' side

            // Intervals do not overlap, so no intersection
            if(d0 <= 0.0f || d1 <= 0.0f)
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
            if(sepLengthSquared < mtvDistance)
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
            return MathFunctions.Clamp(vector3, PlayState.ChunkManager.Bounds);
        }
    }

}