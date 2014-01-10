using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class LocatableComponent : GameComponent, IBoundedObject
    {
        public bool WasAddedToOctree
        {
            get { return wasEverAddedToOctree; }
            set { wasEverAddedToOctree = value; }
        }

        private bool wasEverAddedToOctree = false;

        public CollisionManager.CollisionType CollisionType { get; set; }


        public Matrix GlobalTransform
        {
            get { return globalTransform; }
            set
            {
                globalTransform = value;

                if(!AddToOctree)
                {
                    return;
                }

                if(!IsActive || (!HasMoved && wasEverAddedToOctree) || (!Manager.CollisionManager.NeedsUpdate(this, CollisionType)))
                {
                    return;
                }

                Manager.CollisionManager.UpdateObject(this, CollisionType);
                wasEverAddedToOctree = true;
            }
        }


        public Matrix LocalTransform
        {
            get { return localTransform; }

            set
            {
                localTransform = value;
                HasMoved = true;
            }
        }

        public BoundingBox BoundingBox = new BoundingBox();

        
        public Vector3 BoundingBoxPos { get; set; }
        public bool DrawBoundingBox { get; set; }
        public bool DepthSort { get; set; }
        public bool FrustrumCull { get; set; }
        public bool DrawInFrontOfSiblings { get; set; }

        public bool HasMoved
        {
            get { return hasMoved; }
            set
            {
                hasMoved = value;

                if(!AddToOctree)
                {
                    return;
                }

                foreach(LocatableComponent child in Children.OfType<LocatableComponent>())
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

        public bool AddToOctree { get; set; }

        public LocatableComponent()
        {
            
        }

        public LocatableComponent(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            this(manager, name, parent, localTransform, boundingBoxExtents, boundingBoxPos, true)
        {
            DrawInFrontOfSiblings = false;
            CollisionType = CollisionManager.CollisionType.None;
        }

        public LocatableComponent(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, bool addToOctree) :
            base(manager, name, parent)
        {
            AddToOctree = addToOctree;
            BoundingBoxPos = boundingBoxPos;
            DrawBoundingBox = false;
            BoundingBox = new BoundingBox(localTransform.Translation - boundingBoxExtents / 2.0f + boundingBoxPos, localTransform.Translation + boundingBoxExtents / 2.0f + boundingBoxPos);

            LocalTransform = localTransform;
            HasMoved = true;
            DepthSort = true;
            FrustrumCull = true;
            DrawInFrontOfSiblings = false;
            CollisionType = CollisionManager.CollisionType.None;
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


        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            /*
            if ( IsVisible)
            {
                VoxelChunk myChunk = chunks.GetVoxelChunkAtWorldLocation(GlobalTransform.Translation);

                if (myChunk == null || !myChunk.IsVisible )
                {
                    IsVisible = false;
                }
                else
                {
                    IsVisible = true;
                }
            }
            */

            if(DrawBoundingBox)
            {
                Drawer3D.DrawBox(BoundingBox, Color.White, 0.02f);
            }

            base.Update(gameTime, chunks, camera);
        }

        public void UpdateTransformsRecursive()
        {
            if(!IsActive)
            {
                return;
            }

            if(Parent is LocatableComponent)
            {
                LocatableComponent locatable = (LocatableComponent) Parent;

                if(HasMoved)
                {
                    GlobalTransform = LocalTransform * locatable.GlobalTransform;
                    hasMoved = false;
                }
            }
            else
            {
                if(HasMoved)
                {
                    GlobalTransform = LocalTransform;
                    hasMoved = false;
                }
            }


            lock(Children)
            {
                foreach(LocatableComponent locatable in Children.OfType<LocatableComponent>().Where(locatable => locatable.HasMoved))
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


        public void UpdateBoundingBox()
        {
            Vector3 extents = BoundingBox.Max - BoundingBox.Min;
            float m = Math.Max(Math.Max(extents.X, extents.Y), extents.Z) * 0.5f;
            BoundingBox.Min = GlobalTransform.Translation - extents / 2.0f + BoundingBoxPos;
            BoundingBox.Max = GlobalTransform.Translation + extents / 2.0f + BoundingBoxPos;
        }

        public override void Die()
        {
            UpdateBoundingBox();
            if(AddToOctree)
            {
                Manager.CollisionManager.RemoveObject(this, CollisionType);
            }
            IsActive = false;
            IsVisible = false;
            HasMoved = false;

            base.Die();
        }
    }

}