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
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class Body : GameComponent, IBoundedObject, IRenderableComponent, IUpdateableComponent
    {
        public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            if (Debugger.Switches.DrawBoundingBoxes)
            {
                Drawer3D.DrawBox(BoundingBox, Color.Blue, 0.02f, false);
                Drawer3D.DrawLine(GlobalTransform.Translation, GlobalTransform.Translation + Vector3.UnitX, Color.Red, 0.02f);
                Drawer3D.DrawLine(GlobalTransform.Translation, GlobalTransform.Translation + Vector3.UnitY, Color.Blue, 0.02f);
                Drawer3D.DrawLine(GlobalTransform.Translation, GlobalTransform.Translation + Vector3.UnitZ, Color.Green, 0.02f);
            }
        }

        public bool FrustumCull { get { return IsFlagSet(Flag.FrustumCull); } }
        public CollisionType CollisionType = CollisionType.None;
        public Vector3 BoundingBoxSize = Vector3.One;
        public Vector3 LocalBoundingBoxOffset = Vector3.Zero;
        [JsonIgnore]
        public Action OnDestroyed;
        public bool IsReserved = false;
        public GameComponent ReservedFor = null;
        private BoundingBox lastBounds = new BoundingBox();
        private OctTreeNode<Body> CachedOcttreeNode = null;
        [JsonIgnore]
        public Matrix GlobalTransform
        {
            get { return globalTransform; }
        }

        public Matrix LocalTransform
        {
            get { return localTransform; }
            set
            {
                HasMoved = true;
                localTransform = value;
            }
        }

        private float MaxDiff(BoundingBox a, BoundingBox b)
        {
            return (a.Min - b.Min).LengthSquared() + (a.Max - b.Max).LengthSquared();
        }

        /// <summary>
        /// Sets the global transform without any book-keeping or change detection mechanisms.
        /// !!DANGEROUS!!
        /// Failure to restore the transform when whatever operation called this is finished can break EVERYTHING!
        /// </summary>
        /// <param name="T"></param>
        public void RawSetGlobalTransform(Matrix T)
        {
            globalTransform = T;
        }

        [JsonIgnore]
        public Vector3 Position
        {
            get { return GlobalTransform.Translation; }
        }

        [JsonIgnore]
        public Vector3 LocalPosition
        {
            get { return LocalTransform.Translation; }
            set
            {
                var t = localTransform;
                t.Translation = value;
                LocalTransform = t;
            }
        }

        public BoundingBox BoundingBox = new BoundingBox();

        public List<MotionAnimation> AnimationQueue = new List<MotionAnimation>();

        public bool HasMoved
        {
            get { return hasMoved; }
            set
            {
                hasMoved = value;

               // if (value)
               //     foreach (var child in EnumerateChildren().OfType<Body>())
               //         child.HasMoved = true;
            }
        }

        protected Matrix localTransform = Matrix.Identity;
        protected Matrix globalTransform = Matrix.Identity;
        private bool hasMoved = true;

        public Body()
        {
        }

        public Body(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            this(manager, name, localTransform, boundingBoxExtents, boundingBoxPos, true)
        {
        }

        public Body(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, bool addToCollisionManager) :
            base(name, manager)
        {
            BoundingBoxSize = boundingBoxExtents;
            LocalBoundingBoxOffset = boundingBoxPos;

            //SetFlag(Flag.AddToCollisionManager, addToCollisionManager);
            LocalTransform = localTransform;
            globalTransform = localTransform;

            SetFlag(Flag.FrustumCull, true);
        }

        public virtual void OrientToWalls()
        {
            Orient(0);
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

        public virtual void Orient(float angle)
        {
            Matrix mat = Matrix.CreateRotationY(angle);
            mat.Translation = LocalTransform.Translation;
            LocalTransform = mat;
            PropogateTransforms();
        }

        public void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            if (AnimationQueue.Count > 0)
            {
                var anim = AnimationQueue[0];
                anim.Update(Time);

                LocalTransform = anim.GetTransform();

                if (anim.IsDone())
                    AnimationQueue.RemoveAt(0);
            }

            if (HasMoved)
            {
                UpdateTransform();
                for (var i = 0; i < Children.Count; ++i)
                    if (Children[i] is Body child) child.hasMoved = true;
            }
        }
        
        public virtual string GetMouseOverText()
        {
            return Name;
        }

        public void UpdateTransform()
        {
            PerformanceMonitor.PushFrame("Body.UpdateTransform");

            var newTransform = Matrix.Identity;

            if (Parent != null)
                newTransform = LocalTransform * (Parent as Body).GlobalTransform;
            else
                newTransform = LocalTransform;

            globalTransform = newTransform;

            UpdateBoundingBox();

            if (CachedOcttreeNode == null || MaxDiff(lastBounds, BoundingBox) > 0.1f)
            {
                if (CollisionType != CollisionType.None)
                {
                    if (CachedOcttreeNode == null || CachedOcttreeNode.Bounds.Contains(BoundingBox) != ContainmentType.Contains)
                    {
                        Manager.World.OctTree.RemoveItem(this, lastBounds);
                        if (!IsDead)
                            CachedOcttreeNode = Manager.World.OctTree.AddToTreeEx(Tuple.Create(this, BoundingBox));
                    }
                    else
                    {
                        CachedOcttreeNode.RemoveItem(this, lastBounds);
                        if (!IsDead)
                            CachedOcttreeNode = CachedOcttreeNode.AddToTreeEx(Tuple.Create(this, BoundingBox));
                    }
                }

                lastBounds = BoundingBox;
            }

            hasMoved = false;

            PerformanceMonitor.PopFrame();
        }

        public void PropogateTransforms()
        {
            PerformanceMonitor.PushFrame("Propogate Transforms");

            UpdateTransform();
            for (var i = 0; i < Children.Count; ++i)
                if (Children[i] is Body child) child.PropogateTransforms();

            PerformanceMonitor.PopFrame();
        }

        public BoundingBox GetBoundingBox()
        {
            return BoundingBox;
        }

        public BoundingBox GetRotatedBoundingBox()
        {
            var min = LocalBoundingBoxOffset - (BoundingBoxSize * 0.5f);
            var max = LocalBoundingBoxOffset + (BoundingBoxSize * 0.5f);
            min = Vector3.Transform(min, GlobalTransform);
            max = Vector3.Transform(max, GlobalTransform);
            return new BoundingBox(MathFunctions.Min(min, max), MathFunctions.Max(min, max));
        }

        public void UpdateBoundingBox()
        {
            if (IsFlagSet(Flag.RotateBoundingBox))
                BoundingBox = GetRotatedBoundingBox();
            else
            {
                BoundingBox.Min = GlobalTransform.Translation + LocalBoundingBoxOffset - (BoundingBoxSize * 0.5f);
                BoundingBox.Max = GlobalTransform.Translation + LocalBoundingBoxOffset + (BoundingBoxSize * 0.5f);
            }
        }

        public override void Delete()
        {
            if (CollisionType != CollisionType.None)
                Manager.World.OctTree.RemoveItem(this, lastBounds);
            base.Delete();
        }

        public override void Die()
        {
            if (Manager != null && CollisionType != CollisionType.None)
                Manager.World.OctTree.RemoveItem(this, lastBounds);

            if (OnDestroyed != null) OnDestroyed();

            base.Die();
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            PropogateTransforms();
            base.CreateCosmeticChildren(Manager);
        }

        public void Face(Vector3 target)
        {
            Vector3 diff = target - GlobalTransform.Translation;
            Matrix newTransform = Matrix.CreateRotationY((float)Math.Atan2(diff.X, -diff.Z));
            newTransform.Translation = LocalTransform.Translation;
            LocalTransform = newTransform;
        }
    }
}
