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
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// This is a special kind of component which has a position and orientation. The global position of an object
    /// is computed from its local position relative to its parent. This is known as a "scene graph". Locatable components
    /// also live inside an octree for faster access to colliding or nearby objects.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Body : GameComponent, IBoundedObject, IUpdateableComponent
    {
        public bool WasAddedToOctree
        {
            get { return wasEverAddedToOctree; }
            set { wasEverAddedToOctree = value; }
        }

        private bool wasEverAddedToOctree = false;

        public CollisionManager.CollisionType CollisionType { get; set; }

        public delegate void BodyDestroyed();
        public event BodyDestroyed OnDestroyed;


        public bool IsReserved = false;
        public GameComponent ReservedFor = null;
        private BoundingBox lastBounds = new BoundingBox();
        private Vector3 thresholdPos = new Vector3();
        
        [JsonIgnore]public Matrix GlobalTransform
        {
            get { return globalTransform; }
            set
            {
                globalTransform = value;
                UpdateBoundingBox();
                if(!AddToCollisionManager)
                {
                    return;
                }

                if(!Active)
                {
                    return;
                }

                if (!ExceedsMovementThreshold && wasEverAddedToOctree)
                    return;

                Manager.World.CollisionManager.RemoveObject(this, lastBounds, CollisionType);
                Manager.World.CollisionManager.AddObject(this, CollisionType);

                lastBounds = GetBoundingBox();
                wasEverAddedToOctree = true;
                ExceedsMovementThreshold = false;
                thresholdPos = Position;
            }
        }

        private bool propogateTransforms = false;
        public Matrix LocalTransform
        {
            get { return localTransform; }
            set
            {
                localTransform = value;
                HasMoved = true;

                if ((Position - thresholdPos).LengthSquared() > 1.0)
                    ExceedsMovementThreshold = true;

                propogateTransforms = true;
            }
        }

        private bool firstIter = true;

        [JsonIgnore]
        public Vector3 Position
        {
            get { return GlobalTransform.Translation; }
        }

        [JsonIgnore]
        public Vector3 LocalPosition
        {
            get { return LocalTransform.Translation; }
            set { localTransform.Translation = value; }
        }

        public BoundingBox BoundingBox = new BoundingBox();


        public bool DrawScreenRect { get; set; }
        public Vector3 BoundingBoxPos { get; set; }
        public bool DrawBoundingBox { get; set; }
        public bool DepthSort { get; set; }
        public bool FrustrumCull { get; set; }
        public bool DrawInFrontOfSiblings { get; set; }

        public bool IsAboveCullPlane(ChunkManager Chunks)
        {
            return GlobalTransform.Translation.Y - GetBoundingBox().Extents().Y > (Chunks.ChunkData.MaxViewingLevel + 5);
        }

        public List<MotionAnimation> AnimationQueue { get; set; }

        public bool ExceedsMovementThreshold { get; set; }

        public bool HasMoved
        {
            get { return hasMoved; }
            set
            {
                hasMoved = value;

                if (value)
                    foreach (Body child in Children.OfType<Body>())
                        child.ParentMoved = true;
            }
        }

        public bool ParentMoved = false;

        public uint GetID()
        {
            return GlobalID;
        }


        protected Matrix localTransform = Matrix.Identity;
        protected Matrix globalTransform = Matrix.Identity;
        private bool hasMoved = true;

        public bool AddToCollisionManager { get; set; }
        public bool DrawReservation { get; set; }
        public Body()
        {
            if(OnDestroyed == null)
                OnDestroyed +=Body_OnDestroyed;
        }

        void Body_OnDestroyed()
        {

        }


        public Body(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            this(manager, name, localTransform, boundingBoxExtents, boundingBoxPos, true)
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

        public Body(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, bool addToCollisionManager) :
            base(name, manager)
        {
            DrawReservation = false;
            AnimationQueue = new List<MotionAnimation>();
            AddToCollisionManager = addToCollisionManager;
            BoundingBoxPos = boundingBoxPos;
            DrawBoundingBox = false;
            BoundingBox = new BoundingBox(localTransform.Translation - boundingBoxExtents / 2.0f + boundingBoxPos, localTransform.Translation + boundingBoxExtents / 2.0f + boundingBoxPos);

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

        public void OrientToWalls()
        {
            var curr = new VoxelHandle(Manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(LocalTransform.Translation));
            if (curr.IsValid)
            {
                foreach (var n in VoxelHelpers.EnumerateManhattanNeighbors2D(curr.Coordinate))
                {
                    var v = new VoxelHandle(World.ChunkManager.ChunkData, n);
                    if (v.IsValid && !v.IsEmpty)
                    { 
                        Vector3 diff = n.ToVector3() - curr.WorldPosition;
                        Orient((float)Math.Atan2(diff.X, diff.Z));
                        break;
                    }
                }
            }
        }

        public void Orient(float angle)
        {
            Matrix mat = Matrix.CreateRotationY(angle);
            mat.Translation = LocalTransform.Translation;
            LocalTransform = mat;
        }
 

        public Rectangle GetScreenRect(Camera camera)
        {
            BoundingBox box = GetBoundingBox();

            
            Vector3 ext = (box.Max - box.Min);
            Vector3 center = box.Center();


            Vector3 p1 = camera.Project(box.Min);
            Vector3 p2 = camera.Project(box.Max);
            Vector3 p3 = camera.Project(box.Min + new Vector3(ext.X, 0, 0));
            Vector3 p4 = camera.Project(box.Min + new Vector3(0, ext.Y, 0));
            Vector3 p5 = camera.Project(box.Min + new Vector3(0, 0, ext.Z));
            Vector3 p6 = camera.Project(box.Min + new Vector3(ext.X, ext.Y, 0));


            Vector3 min = MathFunctions.Min(p1, p2, p3, p4, p5, p6);
            Vector3 max = MathFunctions.Max(p1, p2, p3, p4, p5, p6);

            return new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));

        }


        public bool Intersects(BoundingSphere sphere)
        {
            return (sphere.Intersects(BoundingBox));
        }


        public bool Intersects(BoundingFrustum fr)
        {
            return (fr.Intersects(BoundingBox));
        }

        public bool Intersects(BoundingBox box)
        {
            return (box.Intersects(BoundingBox));
        }

        public bool Intersects(Ray ray)
        {
            return (ray.Intersects(BoundingBox) != null);
        }


        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (ParentMoved) HasMoved = true;
            ParentMoved = false;

            if(AnimationQueue.Count > 0)
            {
                HasMoved = true;
                MotionAnimation anim = AnimationQueue[0];
                anim.Update(gameTime);

                LocalTransform = anim.GetTransform();

                if(anim.IsDone())
                {
                    AnimationQueue.RemoveAt(0);
                }
            }

            if (firstIter || propogateTransforms || HasMoved)
            {
                PropogateTransforms();
                firstIter = false;
                propogateTransforms = false;
            }
        }

        ///// <summary>
        ///// Renders the component.
        ///// </summary>
        ///// <param name="gameTime">The game time.</param>
        ///// <param name="chunks">The chunk manager.</param>
        ///// <param name="camera">The camera.</param>
        ///// <param name="spriteBatch">The sprite batch.</param>
        ///// <param name="graphicsDevice">The graphics device.</param>
        ///// <param name="effect">The shader to use.</param>
        ///// <param name="renderingForWater">if set to <c>true</c> rendering for water reflections.</param>
        //public virtual void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        //{
        //}


        public void UpdateTransform()
        {
            if (Parent != Manager.RootComponent && Parent != null)
            {
                GlobalTransform = LocalTransform * (Parent as Body).GlobalTransform;
                hasMoved = false;
            }
            else
            {
                GlobalTransform = LocalTransform;
                hasMoved = false;
            }
        }


        public void PropogateTransforms()
        {
            UpdateTransform();
            foreach (var child in Children.OfType<Body>())
                child.PropogateTransforms();
        }


        public BoundingBox GetBoundingBox()
        {
            return BoundingBox;
        }

        public BoundingBox GetRotatedBoundingBox()
        {
            Vector3 min = Vector3.Transform(BoundingBox.Min - GlobalTransform.Translation, GlobalTransform);
            Vector3 max = Vector3.Transform(BoundingBox.Max - GlobalTransform.Translation, GlobalTransform);
            return new BoundingBox(MathFunctions.Min(min, max), MathFunctions.Max(min, max));
        }


        public void UpdateBoundingBox()
        {
            Vector3 extents = (BoundingBox.Max - BoundingBox.Min) * 0.5f;
            Vector3 translation = GlobalTransform.Translation;
            BoundingBox.Min = translation - extents + BoundingBoxPos;
            BoundingBox.Max = translation + extents + BoundingBoxPos;
        }

        public override void Die()
        {
            UpdateBoundingBox();
            if(AddToCollisionManager)
            {
                Manager.World.CollisionManager.RemoveObject(this, GetBoundingBox(), CollisionType);
            }
            Active = false;
            IsVisible = false;
            OnDestroyed.Invoke();
            base.Die();
        }


    }

}
