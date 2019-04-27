using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public partial class GameComponent
    {
        public virtual void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            if (Debugger.Switches.DrawBoundingBoxes)
            {
                Drawer3D.DrawBox(BoundingBox, Color.Blue, 0.02f, false);
                Drawer3D.DrawLine(GlobalTransform.Translation, GlobalTransform.Translation + Vector3.UnitX, Color.Red, 0.02f);
                Drawer3D.DrawLine(GlobalTransform.Translation, GlobalTransform.Translation + Vector3.UnitY, Color.Blue, 0.02f);
                Drawer3D.DrawLine(GlobalTransform.Translation, GlobalTransform.Translation + Vector3.UnitZ, Color.Green, 0.02f);
            }
        }

        public CollisionType CollisionType = CollisionType.Static;
        public Vector3 BoundingBoxSize = Vector3.One;
        public Vector3 LocalBoundingBoxOffset = Vector3.Zero;
        [JsonIgnore]
        public Action OnDestroyed;
        [JsonIgnore]
        public bool IsReserved
        {
            get { return ReservedFor != null; }
        }
        [JsonIgnore]
        public GameComponent ReservedFor = null;
        private BoundingBox LastBounds = new BoundingBox();
        private OctTreeNode<GameComponent> CachedOcttreeNode = null;
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
                HasMoved = true;
            }
        }

        [JsonIgnore]
        public BoundingBox BoundingBox = new BoundingBox();

        public List<MotionAnimation> AnimationQueue = new List<MotionAnimation>();

        public bool HasMoved = true;

        protected Matrix localTransform = Matrix.Identity;
        protected Matrix globalTransform = Matrix.Identity;

        // Todo: Remove unused argument addToCollisionManager
        public GameComponent(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            this(name, manager)
        {
            BoundingBoxSize = boundingBoxExtents;
            LocalBoundingBoxOffset = boundingBoxPos;

            LocalTransform = localTransform;
            globalTransform = localTransform;
        }

        public virtual void OrientToWalls()
        {
            Orient(0);
            var curr = new VoxelHandle(Manager.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(LocalTransform.Translation));
            if (curr.IsValid)
            {
                foreach (var n in VoxelHelpers.EnumerateManhattanNeighbors2D(curr.Coordinate))
                {
                    var v = new VoxelHandle(World.ChunkManager, n);
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

        public virtual void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            if (AnimationQueue.Count > 0)
            {
                var anim = AnimationQueue[0];
                anim.Update(Time);

                LocalTransform = anim.GetTransform();

                if (anim.IsDone())
                    AnimationQueue.RemoveAt(0);
            }

            //if (HasMoved)
            //{
            //    UpdateTransform();
            //    for (var i = 0; i < Children.Count; ++i)
            //        if (Children[i] is Body child) child.HasMoved = true;
            //}
        }

        public void ProcessTransformChange()
        {
            if (HasMoved)
            {
                UpdateTransform();
                for (var i = 0; i < Children.Count; ++i)
                    if (Children[i] is GameComponent child) child.HasMoved = true;
            }
        }

        public virtual string GetMouseOverText()
        {
            return Name;
        }

        public void UpdateTransform()
        {
            HasMoved = false;

            PerformanceMonitor.PushFrame("Body.UpdateTransform");

            var newTransform = Matrix.Identity;

            if ((Parent as GameComponent) != null)
                newTransform = LocalTransform * (Parent as GameComponent).GlobalTransform;
            else
                newTransform = LocalTransform;

            globalTransform = newTransform;

            UpdateBoundingBox();

            Manager.World.RemoveGameObject(this, LastBounds);
            Manager.World.AddGameObject(this, BoundingBox);
            LastBounds = BoundingBox;

            /*
            if (CollisionType != CollisionType.None && (CachedOcttreeNode == null || MaxDiff(LastBounds, BoundingBox) > 0.1f))
            {
                {
                    if (CachedOcttreeNode == null || CachedOcttreeNode.Contains(BoundingBox) == ContainmentType.Disjoint)
                    {
                        RemoveFromOctTree();
                        if (!IsDead)
                            CachedOcttreeNode = Manager.World.AddGameObject(this, BoundingBox);
                    }
                    else // Drill down to the lowest level of the tree possible,
                    {
                        CachedOcttreeNode.Remove(this, LastBounds);
                        if (!IsDead)
                            CachedOcttreeNode = CachedOcttreeNode.Add(this, BoundingBox);
                    }
                }

                LastBounds = BoundingBox;
            }
            */

            PerformanceMonitor.PopFrame();
        }

        public void PropogateTransforms()
        {
            PerformanceMonitor.PushFrame("Propogate Transforms");

            UpdateTransform();
            for (var i = 0; i < Children.Count; ++i)
                if (Children[i] is GameComponent child) child.PropogateTransforms();

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

        //public override void Delete()
        //{
        //    RemoveFromOctTree();
        //    base.Delete();
        //}

        //public override void Die()
        //{
        //    RemoveFromOctTree();
        //    if (OnDestroyed != null) OnDestroyed();

        //    base.Die();
        //}

        public virtual void CreateCosmeticChildren(ComponentManager Manager)
        {
            PropogateTransforms();
            //base.CreateCosmeticChildren(Manager);
        }

        public void Face(Vector3 target)
        {
            Vector3 diff = target - GlobalTransform.Translation;
            Matrix newTransform = Matrix.CreateRotationY((float)Math.Atan2(diff.X, -diff.Z));
            newTransform.Translation = LocalTransform.Translation;
            LocalTransform = newTransform;
        }

        private void RemoveFromOctTree()
        {
            if (Manager != null)
                Manager.World.RemoveGameObject(this, LastBounds);
            CachedOcttreeNode = null;
        }
    }
}
