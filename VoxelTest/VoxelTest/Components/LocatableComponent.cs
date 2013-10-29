using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
namespace DwarfCorp
{
    public class LocatableComponent : GameComponent, BoundedObject
    {
        [JsonIgnore]
        public bool WasAddedToOctree { get { return m_wasEverAddedToOctree; } }
        private bool m_wasEverAddedToOctree = false;

        [JsonIgnore]
        public Matrix GlobalTransform
        { 
            get
            {
                return m_globalTransform;
            }
            set
            {
                m_globalTransform = value;

                if (AddToOctree)
                {
                    if (IsActive 
                        && (HasMoved || !m_wasEverAddedToOctree) 
                        && (m_octree.NeedsUpdate(this)))
                    {
                        m_octree.AddUpdate(this);
                        m_wasEverAddedToOctree = true;
                    }
                }
            }
        }

        [JsonIgnore]
        public Matrix LocalTransform 
        { 
            get 
            { 
                return m_localTransform; 
            }
            
            set 
            { 
                m_localTransform = value;
                HasMoved = true;
            } 
        }

        public BoundingBox BoundingBox = new BoundingBox();
        public BoundingSphere BoundingSphere = new BoundingSphere();
        public Vector3 BoundingBoxPos { get; set; }
        public bool DrawBoundingBox { get; set; }
        public bool DepthSort { get; set; }
        public bool FrustrumCull { get; set; }
        public bool DrawInFrontOfSiblings { get; set; }
        
        [JsonIgnore]
        public static Octree m_octree = null;
        public bool IsStatic { get; set; }
        
        [JsonIgnore]
        public bool HasMoved 
        {
            get
            {
                return m_hasMoved;
            }
            set
            {
                m_hasMoved = value;

                if (AddToOctree)
                {
                    foreach (GameComponent child in Children.Values)
                    {
                        if (child is LocatableComponent)
                        {
                            ((LocatableComponent)child).HasMoved = value;
                        }
                    }
                }
            }
        }

        public uint GetID() { return GlobalID; }
        
        protected Matrix m_localTransform = Matrix.Identity;
        

        protected Matrix m_globalTransform = Matrix.Identity;
        private bool m_hasMoved = true;
        
        public bool AddToOctree { get; set; }


        public LocatableComponent(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            this(manager, name, parent, localTransform, boundingBoxExtents, boundingBoxPos, true)
        {
            DrawInFrontOfSiblings = false;
            IsStatic = false;
        }
        public LocatableComponent(ComponentManager manager, string name, GameComponent parent, Matrix localTransform,  Vector3 boundingBoxExtents, Vector3 boundingBoxPos, bool addToOctree) :
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

            if (DrawBoundingBox)
            {
                SimpleDrawing.DrawBox(BoundingBox, Color.White, 0.02f);
            }


            base.Update(gameTime, chunks, camera);
        }

        public void UpdateTransformsRecursive()
        {
            if (Parent is LocatableComponent)
            {
                LocatableComponent locatable = (LocatableComponent)Parent;

                if (HasMoved)
                {
                    GlobalTransform = LocalTransform * locatable.GlobalTransform;
                    m_hasMoved = false;
                }

            }
            else
            {
                if (HasMoved)
                {
                    GlobalTransform = LocalTransform;
                    m_hasMoved = false;
                }
            }

          
            foreach(KeyValuePair<uint, GameComponent> pair in Children)
            {
                if (pair.Value is LocatableComponent)
                {
                    LocatableComponent locatable = (LocatableComponent)pair.Value;

                    if (locatable.HasMoved)
                    {
                        locatable.UpdateTransformsRecursive();
                    }
                }
            }

            UpdateBoundingBox();

        }

        public BoundingSphere GetBoundingSphere()
        {
            return BoundingSphere;
        }

        public BoundingBox GetBoundingBox()
        {
            return BoundingBox;
        }

        public override void Render(GameTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
            
             
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }

        public void UpdateBoundingBox()
        {
            Vector3 extents = BoundingBox.Max - BoundingBox.Min;
            float m = Math.Max(Math.Max(extents.X, extents.Y), extents.Z) * 0.5f;
            BoundingBox.Min = GlobalTransform.Translation - extents / 2.0f + BoundingBoxPos;
            BoundingBox.Max = GlobalTransform.Translation + extents / 2.0f + BoundingBoxPos;
            BoundingSphere.Center = GlobalTransform.Translation;
            BoundingSphere.Radius = (float)Math.Sqrt(3 * m * m);
        }

        public override void Die()
        {
            UpdateBoundingBox();
            if (AddToOctree)
            {
                m_octree.RemoveObject(this);
            }
            IsActive = false;
            IsVisible = false;
            HasMoved = false;
           
            base.Die();
        }
    }
}
