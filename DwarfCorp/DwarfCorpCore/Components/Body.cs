﻿using System;
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
    public class Body : GameComponent, IBoundedObject
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
        
        public Matrix GlobalTransform
        {
            get { return globalTransform; }
            set
            {
                globalTransform = value;

                if(!AddToCollisionManager)
                {
                    return;
                }

                if(!IsActive || (!HasMoved && wasEverAddedToOctree) )
                {
                    return;
                }

                if (!ExceedsMovementThreshold && wasEverAddedToOctree)
                    return;

                Manager.CollisionManager.RemoveObject(this, lastBounds, CollisionType);
                Manager.CollisionManager.AddObject(this, CollisionType);

                lastBounds = GetBoundingBox();
                wasEverAddedToOctree = true;
                HasMoved = false;
                ExceedsMovementThreshold = false;
                thresholdPos = Position;
            }
        }


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

        [JsonIgnore]
        public Vector3 Position
        {
            get { return GlobalTransform.Translation; }
        }

        public BoundingBox BoundingBox = new BoundingBox();


        public bool DrawScreenRect { get; set; }
        public Vector3 BoundingBoxPos { get; set; }
        public bool DrawBoundingBox { get; set; }
        public bool DepthSort { get; set; }
        public bool FrustrumCull { get; set; }
        public bool DrawInFrontOfSiblings { get; set; }
        public bool IsAboveCullPlane { get; set; }

        public List<MotionAnimation> AnimationQueue { get; set; }

        public bool ExceedsMovementThreshold { get; set; }

        public bool HasMoved
        {
            get { return hasMoved; }
            set
            {
                hasMoved = value;

                foreach(Body child in Children.OfType<Body>())
                {
                    (child).HasMoved = value;
                }
            }
        }

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

        public Body(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
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

        public Body(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, bool addToCollisionManager) :
            base(name, parent)
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
            Voxel curr = new Voxel();
            Voxel[] neighbors = new Voxel[4];
            Vector3 pos = LocalTransform.Translation;
            if (PlayState.ChunkManager.ChunkData.GetVoxel(pos, ref curr))
            {
                
                curr.Chunk.Get2DManhattanNeighbors(neighbors, (int)curr.GridPosition.X, (int)curr.GridPosition.Y, (int)curr.GridPosition.Z);

                foreach (Voxel neighbor in neighbors)
                {
                    if (neighbor != null && !neighbor.IsEmpty)
                    {
                        Vector3 diff = neighbor.Position - curr.Position;
                        Matrix mat = Matrix.CreateRotationY((float)Math.Atan2(diff.X, diff.Z));
                        mat.Translation = pos;
                        LocalTransform = mat;
                        break;
                    }
                }
            }
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


        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            IsAboveCullPlane =  GlobalTransform.Translation.Y - GetBoundingBox().Extents().Y > (chunks.ChunkData.MaxViewingLevel + 5);
            if(DrawScreenRect)
            {
                Drawer2D.DrawRect(GetScreenRect(camera), Color.Transparent, Color.White, 1);
            }

            if(DrawBoundingBox)
            {
                Drawer3D.DrawBox(BoundingBox, Color.White, 0.02f);
                Drawer3D.DrawBox(GetRotatedBoundingBox(), Color.Red, 0.02f);
            }

            if (DrawReservation && IsReserved)
            {
                Drawer3D.DrawBox(BoundingBox, Color.White, 0.02f);
            }

            if(AnimationQueue.Count > 0)
            {
                MotionAnimation anim = AnimationQueue[0];
                anim.Update(gameTime);

                LocalTransform = anim.GetTransform();

                if(anim.IsDone())
                {
                    AnimationQueue.RemoveAt(0);
                }
            }

            base.Update(gameTime, chunks, camera);
        }


        public void UpdateTransformsRecursive()
        {
            if(!IsActive)
            {
                return;
            }

            if(Parent is Body)
            {
                Body locatable = (Body) Parent;

                //if(HasMoved)
                {
                    GlobalTransform = LocalTransform * locatable.GlobalTransform;
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


            lock(Children)
            {
                foreach(Body locatable in Children.OfType<Body>())
                {
                    locatable.UpdateTransformsRecursive();
                }
            }

            UpdateBoundingBox();
        }



        public BoundingBox GetBoundingBox()
        {
            return BoundingBox;
        }

        public BoundingBox GetRotatedBoundingBox()
        {
            Vector3 min = Vector3.Transform(BoundingBox.Min - GlobalTransform.Translation, GlobalTransform);
            Vector3 max = Vector3.Transform(BoundingBox.Max - GlobalTransform.Translation, GlobalTransform);
            return new BoundingBox(min, max);
        }


        public void UpdateBoundingBox()
        {
            Vector3 extents = BoundingBox.Max - BoundingBox.Min;
            BoundingBox.Min = GlobalTransform.Translation - extents / 2.0f + BoundingBoxPos;
            BoundingBox.Max = GlobalTransform.Translation + extents / 2.0f + BoundingBoxPos;
        }

        public override void Die()
        {
            UpdateBoundingBox();
            if(AddToCollisionManager)
            {
                Manager.CollisionManager.RemoveObject(this, GetBoundingBox(), CollisionType);
            }
            IsActive = false;
            IsVisible = false;
            HasMoved = false;
            OnDestroyed.Invoke();
            base.Die();
        }

        /*
        public static IEnumerable<Act.Status> UnReserve(Body closestItem)
        {
            closestItem.IsReserved = false;
            closestItem.ReservedFor = null;
            yield return Act.Status.Success;
        }
         */
    }

}