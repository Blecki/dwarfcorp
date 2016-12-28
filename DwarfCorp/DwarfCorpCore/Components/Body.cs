// Body.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     This is a special kind of component which has a position and orientation. The global position of an object
    ///     is computed from its local position relative to its parent. This is known as a "scene graph". Bodies
    ///     also live inside an octree for faster access to colliding or nearby objects. Basically anything that has a
    ///     position in the real world should be a body.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Body : GameComponent, IBoundedObject
    {
        #region properties

        /// <summary>
        ///     Delegate called whenever the body dies or is otherwise destroyed.
        /// </summary>
        public delegate void BodyDestroyed();

        /// <summary>
        ///     The global 3D bounding box of the object.
        /// </summary>
        public BoundingBox BoundingBox = new BoundingBox();

        /// <summary>
        ///     This is a lock on the body placed by creatures that want to pick it up.
        ///     If IsReserved is true, that means some creature has marked the object for pickup.
        ///     No other creature may pick up a reserved object.
        /// </summary>
        public bool IsReserved = false;

        /// <summary>
        ///     This is the creature that has marked the object for pickup.
        /// </summary>
        public GameComponent ReservedFor = null;

        /// <summary>
        ///     The global transform relative to the root object.
        /// </summary>
        protected Matrix globalTransform = Matrix.Identity;

        /// <summary>
        ///     If true, the object has moved this frame.
        /// </summary>
        private bool hasMoved = true;

        /// <summary>
        ///     The world bounding box of the object most recently computed.
        /// </summary>
        private BoundingBox lastBounds;

        /// <summary>
        ///     The local transform relative to the parent.
        /// </summary>
        protected Matrix localTransform = Matrix.Identity;

        /// <summary>
        ///     Check the current position against this one to see if the object has moved recently
        ///     (for book keeping)
        /// </summary>
        private Vector3 thresholdPos;

        private bool wasEverAddedToOctree;

        /// <summary>
        ///     True whenever this body exists in the octree.
        /// </summary>
        public bool WasAddedToOctree
        {
            get { return wasEverAddedToOctree; }
            set { wasEverAddedToOctree = value; }
        }

        /// <summary>
        ///     Type of the object, whether static or dynamic.
        /// </summary>
        public CollisionManager.CollisionType CollisionType { get; set; }

        /// <summary>
        ///     This is the position/orientation of the object in the global frame, computed by recursively
        ///     multiplying the local transform.
        /// </summary>
        public Matrix GlobalTransform
        {
            get { return globalTransform; }
            set
            {
                globalTransform = value;

                // If we are setting the global transform, make sure
                // to add it to the collision manager.
                if (!AddToCollisionManager)
                {
                    return;
                }

                // If the object has not moved, don't bother updating the octree.
                if (!IsActive || (!HasMoved && wasEverAddedToOctree))
                {
                    return;
                }

                // The object has moved, so update its bounding box.
                UpdateBoundingBox();

                // If the object has not moved beyond a limit, don't add it to the octree.
                if (!ExceedsMovementThreshold && wasEverAddedToOctree)
                    return;

                // Remove the object from the octree and add it again to update the
                // octree's state.
                Manager.CollisionManager.RemoveObject(this, lastBounds, CollisionType);
                Manager.CollisionManager.AddObject(this, CollisionType);

                // Book keeping to keep track of the object's movement over time.
                lastBounds = GetBoundingBox();
                wasEverAddedToOctree = true;
                HasMoved = false;
                ExceedsMovementThreshold = false;
                thresholdPos = Position;
            }
        }

        /// <summary>
        ///     The local transform of the body is the position/orientation relative to its parent.
        ///     The global transform is computed by recursively chaining together local transforms.
        /// </summary>
        public Matrix LocalTransform
        {
            get { return localTransform; }

            set
            {
                localTransform = value;
                HasMoved = true;

                if ((Position - thresholdPos).LengthSquared() > 1.0)
                {
                    ExceedsMovementThreshold = true;
                }
            }
        }

        /// <summary>
        ///     Convenient accessor to the body's position globally.
        ///     Cannot be set.
        /// </summary>
        [JsonIgnore]
        public Vector3 Position
        {
            get { return GlobalTransform.Translation; }
        }

        /// <summary>
        ///     Convenient getter/setter for the local translation.
        /// </summary>
        [JsonIgnore]
        public Vector3 LocalPosition
        {
            get { return LocalTransform.Translation; }
            set
            {
                localTransform.Translation = value;
                LocalTransform = localTransform;
            }
        }


        /// <summary>
        ///     If true, draws a rectangle encasing the object on the screen.
        /// </summary>
        public bool DrawScreenRect { get; set; }

        /// <summary>
        ///     Relative position of the center of the bounding box of the object.
        /// </summary>
        public Vector3 BoundingBoxPos { get; set; }

        /// <summary>
        ///     If true, draws a 3D box around the object.
        /// </summary>
        public bool DrawBoundingBox { get; set; }

        /// <summary>
        ///     If true, sorts the object based on its distance to the camera before
        ///     drawing it.
        /// </summary>
        public bool DepthSort { get; set; }

        /// <summary>
        ///     If true, intersects the object's bounds with the camera to determine
        ///     whether or not to draw it.
        /// </summary>
        public bool FrustrumCull { get; set; }

        /// <summary>
        ///     If true, the object will be drawn after all of its siblings.
        /// </summary>
        public bool DrawInFrontOfSiblings { get; set; }

        /// <summary>
        ///     If true, the object is above the global slice plane, which means tht it should
        ///     not be drawn.
        /// </summary>
        public bool IsAboveCullPlane { get; set; }

        /// <summary>
        ///     Objects may have a list of animations to undergo. This is treated as a FIFO queue.
        /// </summary>
        public List<MotionAnimation> AnimationQueue { get; set; }

        /// <summary>
        ///     If true, the object has moved more than some arbitrary amount.
        /// </summary>
        public bool ExceedsMovementThreshold { get; set; }

        /// <summary>
        ///     If true, the object has moved this frame.
        /// </summary>
        public bool HasMoved
        {
            get { return hasMoved; }
            set
            {
                hasMoved = value;

                foreach (Body child in Children.OfType<Body>())
                {
                    (child).HasMoved = value;
                }
            }
        }

        /// <summary>
        ///     If true, the object will be added to the octree to test
        ///     for collisions.
        /// </summary>
        public bool AddToCollisionManager { get; set; }

        /// <summary>
        ///     If true, debug lines are drawn between the reserving creature and this object.
        /// </summary>
        public bool DrawReservation { get; set; }

        /// <summary>
        ///     All objects have a unique ID.
        /// </summary>
        /// <returns>The object's unique ID.</returns>
        public uint GetID()
        {
            return GlobalID;
        }

        /// <summary>
        ///     Event triggered whenever the body dies.
        /// </summary>
        public event BodyDestroyed OnDestroyed;

        #endregion

        public Body()
        {
            if (OnDestroyed == null)
                OnDestroyed += Body_OnDestroyed;
        }


        /// <summary>
        ///     Construct a new Body.
        /// </summary>
        /// <param name="name">Debug name identifying the object.</param>
        /// <param name="parent">Parent of the object.</param>
        /// <param name="localTransform">Transform relative to the parent.</param>
        /// <param name="boundingBoxExtents">Size of the object's bounding box (in voxels).</param>
        /// <param name="boundingBoxPos">Relative position of the object's bounding box (in voxels)</param>
        public Body(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents,
            Vector3 boundingBoxPos) :
                this(name, parent, localTransform, boundingBoxExtents, boundingBoxPos, true)
        {
            DrawReservation = false;
            AnimationQueue = new List<MotionAnimation>();
            DrawInFrontOfSiblings = false;
            CollisionType = CollisionManager.CollisionType.None;
            DrawScreenRect = false;

            if (OnDestroyed == null)
                OnDestroyed += Body_OnDestroyed;

            lastBounds = GetBoundingBox();
        }

        /// <summary>
        ///     Construct a new body.
        /// </summary>
        /// <param name="name">Debug name of the body.</param>
        /// <param name="parent">Parent of the body.</param>
        /// <param name="localTransform">Transform of the body relative to its parent.</param>
        /// <param name="boundingBoxExtents">Size of the body's bounding box (in voxels)</param>
        /// <param name="boundingBoxPos">Relative position of the body's bounding box center (in voxels)</param>
        /// <param name="addToCollisionManager">If true, the object will be added to the octree.</param>
        public Body(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents,
            Vector3 boundingBoxPos, bool addToCollisionManager) :
                base(name, parent)
        {
            DrawReservation = false;
            AnimationQueue = new List<MotionAnimation>();
            AddToCollisionManager = addToCollisionManager;
            BoundingBoxPos = boundingBoxPos;
            DrawBoundingBox = false;
            BoundingBox = new BoundingBox(localTransform.Translation - boundingBoxExtents/2.0f + boundingBoxPos,
                localTransform.Translation + boundingBoxExtents/2.0f + boundingBoxPos);

            LocalTransform = localTransform;
            HasMoved = true;
            DepthSort = true;
            FrustrumCull = true;
            DrawInFrontOfSiblings = false;
            CollisionType = CollisionManager.CollisionType.None;
            DrawScreenRect = false;

            if (OnDestroyed == null)
                OnDestroyed += Body_OnDestroyed;

            lastBounds = GetBoundingBox();
        }

        /// <summary>
        ///     Get the global bounding box.
        /// </summary>
        /// <returns>An axis-aligned box encasing the object.</returns>
        public BoundingBox GetBoundingBox()
        {
            return BoundingBox;
        }

        private void Body_OnDestroyed()
        {
            // Intentionally left blank. When bodies are destroyed, this delegate is called.
        }

        /// <summary>
        ///     This checks the walls around the body, and orients it such that its X axis is pointing away from the first wall it
        ///     detects.
        /// </summary>
        public void OrientToWalls()
        {
            var curr = new Voxel();
            var neighbors = new Voxel[4];
            Vector3 pos = LocalTransform.Translation;
            if (PlayState.ChunkManager.ChunkData.GetVoxel(pos, ref curr))
            {
                curr.Chunk.Get2DManhattanNeighbors(neighbors, (int) curr.GridPosition.X, (int) curr.GridPosition.Y,
                    (int) curr.GridPosition.Z);

                foreach (Voxel neighbor in neighbors)
                {
                    if (neighbor != null && !neighbor.IsEmpty)
                    {
                        Vector3 diff = neighbor.Position - curr.Position;
                        Matrix mat = Matrix.CreateRotationY((float) Math.Atan2(diff.X, diff.Z));
                        mat.Translation = pos;
                        LocalTransform = mat;
                        break;
                    }
                }
            }
        }


        /// <summary>
        ///     Returns a bounding box in screen coordinates encasing the object. This is used for
        ///     screen selections and other miscellanious tests.
        /// </summary>
        /// <param name="camera">The camera drawing the object.</param>
        /// <returns>A rectangle (in pixels) around the object on the screen.</returns>
        public Rectangle GetScreenRect(Camera camera)
        {
            BoundingBox box = GetBoundingBox();


            Vector3 ext = (box.Max - box.Min);

            // Project all the bounding box points onto the screen.
            Vector3 p1 = camera.Project(box.Min);
            Vector3 p2 = camera.Project(box.Max);
            Vector3 p3 = camera.Project(box.Min + new Vector3(ext.X, 0, 0));
            Vector3 p4 = camera.Project(box.Min + new Vector3(0, ext.Y, 0));
            Vector3 p5 = camera.Project(box.Min + new Vector3(0, 0, ext.Z));
            Vector3 p6 = camera.Project(box.Min + new Vector3(ext.X, ext.Y, 0));


            // Find the min and max of those points.
            Vector3 min = MathFunctions.Min(p1, p2, p3, p4, p5, p6);
            Vector3 max = MathFunctions.Max(p1, p2, p3, p4, p5, p6);

            return new Rectangle((int) min.X, (int) min.Y, (int) (max.X - min.X), (int) (max.Y - min.Y));
        }


        /// <summary>
        ///     Intersection test between the body and a sphere.
        /// </summary>
        /// <param name="sphere">A sphere to test the body against.</param>
        /// <returns>Whether or not the bounding box intersects the sphere.</returns>
        public bool Intersects(BoundingSphere sphere)
        {
            return (sphere.Intersects(BoundingBox));
        }

        /// <summary>
        ///     Intersection test between the body and a frustum (truncated pyramid).
        /// </summary>
        /// <param name="fr">The frustum to test against.</param>
        /// <returns>Whether or not the bounding box intersects the frustum.</returns>
        public bool Intersects(BoundingFrustum fr)
        {
            return (fr.Intersects(BoundingBox));
        }

        /// <summary>
        ///     Intersection test between the body and an axis aligned bounding box.
        /// </summary>
        /// <param name="box">The box to test against.</param>
        /// <returns>Whether or not the bounding box intersects the object's bounding box.</returns>
        public bool Intersects(BoundingBox box)
        {
            return (box.Intersects(BoundingBox));
        }

        /// <summary>
        ///     Intersection test between the body and a Ray
        /// </summary>
        /// <param name="ray">The ray to test against.</param>
        /// <returns>Whether or not the object's bounding box intersects the ray.</returns>
        public bool Intersects(Ray ray)
        {
            return (ray.Intersects(BoundingBox) != null);
        }


        /// <summary>
        ///     Update the body. Updates its transforms and does book keeping.
        /// </summary>
        /// <param name="gameTime">The current time.</param>
        /// <param name="chunks">The chunk manager.</param>
        /// <param name="camera">The camera drawing the object.</param>
        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            // Allow some wiggle room for culling the object using the slice plane
            IsAboveCullPlane = GlobalTransform.Translation.Y - GetBoundingBox().Extents().Y >
                               (chunks.ChunkData.MaxViewingLevel + 5);

            if (DrawScreenRect)
            {
                Drawer2D.DrawRect(GetScreenRect(camera), Color.Transparent, Color.White, 1);
            }

            if (DrawBoundingBox)
            {
                Drawer3D.DrawBox(BoundingBox, Color.White, 0.02f);
                Drawer3D.DrawBox(GetRotatedBoundingBox(), Color.Red, 0.02f);
            }

            if (DrawReservation && IsReserved)
            {
                Drawer3D.DrawBox(BoundingBox, Color.White, 0.02f);
            }

            // Apply the animations in the animation queue.
            if (AnimationQueue.Count > 0)
            {
                MotionAnimation anim = AnimationQueue[0];
                anim.Update(gameTime);

                LocalTransform = anim.GetTransform();

                if (anim.IsDone())
                {
                    AnimationQueue.RemoveAt(0);
                }
            }

            base.Update(gameTime, chunks, camera);
        }


        /// <summary>
        ///     Recursively applies the local transforms of this body to all its children.
        /// </summary>
        public void UpdateTransformsRecursive()
        {
            if (!IsActive)
            {
                return;
            }

            if (Parent is Body)
            {
                var locatable = (Body) Parent;

                //if(HasMoved)
                {
                    GlobalTransform = LocalTransform*locatable.GlobalTransform;
                    hasMoved = false;
                }
            }
            else
            {
                //if(HasMoved)
                {
                    GlobalTransform = LocalTransform;
                    hasMoved = false;
                }
            }


            lock (Children)
            {
                foreach (Body locatable in Children.OfType<Body>())
                {
                    locatable.UpdateTransformsRecursive();
                }
            }

            UpdateBoundingBox();
        }


        /// <summary>
        ///     This actually transforms the object's bounding box into the global frame and finds
        ///     a new axis-aligned bounding box encasing it.
        /// </summary>
        /// <returns>A transformed bounding box</returns>
        public BoundingBox GetRotatedBoundingBox()
        {
            Vector3 min = Vector3.Transform(BoundingBox.Min - GlobalTransform.Translation, GlobalTransform);
            Vector3 max = Vector3.Transform(BoundingBox.Max - GlobalTransform.Translation, GlobalTransform);
            return new BoundingBox(min, max);
        }


        /// <summary>
        ///     Moves the bounding box along with the object.
        /// </summary>
        public void UpdateBoundingBox()
        {
            Vector3 extents = BoundingBox.Max - BoundingBox.Min;
            BoundingBox.Min = GlobalTransform.Translation - extents/2.0f + BoundingBoxPos;
            BoundingBox.Max = GlobalTransform.Translation + extents/2.0f + BoundingBoxPos;
        }

        /// <summary>
        ///     Called whenever the body has died by normal means (for instance, if its health becomes zero)
        /// </summary>
        public override void Die()
        {
            UpdateBoundingBox();
            if (AddToCollisionManager)
            {
                Manager.CollisionManager.RemoveObject(this, GetBoundingBox(), CollisionType);
            }
            IsActive = false;
            IsVisible = false;
            HasMoved = false;
            OnDestroyed.Invoke();
            base.Die();
        }
    }
}