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
        public Color DebugColor = Color.Blue;

        public virtual void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            if (Debugger.Switches.DrawBoundingBoxes)
            {
                Drawer3D.DrawBox(BoundingBox, DebugColor, 0.02f, false);
                Drawer3D.DrawLine(GlobalTransform.Translation, GlobalTransform.Translation + Vector3.UnitX, Color.Red, 0.02f);
                Drawer3D.DrawLine(GlobalTransform.Translation, GlobalTransform.Translation + Vector3.UnitY, Color.Blue, 0.02f);
                Drawer3D.DrawLine(GlobalTransform.Translation, GlobalTransform.Translation + Vector3.UnitZ, Color.Green, 0.02f);
            }
        }

        public CollisionType CollisionType = CollisionType.Static;
        public Vector3 BoundingBoxSize = Vector3.One;
        public Vector3 LocalBoundingBoxOffset = Vector3.Zero;
        [JsonIgnore] public Action OnDestroyed;
        [JsonIgnore] public bool IsReserved => ReservedFor != null;
        [JsonIgnore] public GameComponent ReservedFor = null;
        private BoundingBox LastBounds = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(0, 0, 0)); // This is to make sure objects that spawn in chunk 0,0,0 actually get added to the octtree.

        [JsonIgnore] public Matrix GlobalTransform => globalTransform;

        public Matrix LocalTransform
        {
            get { return localTransform; }
            set
            {
                HasMoved = true;
                localTransform = value;
            }
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
            //PropogateTransforms();
        }

        public virtual void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            if (IsDead)
                return;

            if (AnimationQueue.Count > 0)
            {
                var anim = AnimationQueue[0];
                anim.Update(Time);

                LocalTransform = anim.GetTransform();

                if (anim.IsDone())
                    AnimationQueue.RemoveAt(0);
            }

            for (var i = 0; i < Children.Count; ++i)
                if (!Children[i].IsFlagSet(Flag.DontUpdate))
                    Children[i].Update(Time, Chunks, Camera);
        }

        public void ProcessTransformChange()
        {
            if (IsDead)
                return;

            if (HasMoved)
            {
                UpdateTransform();
                for (var i = 0; i < Children.Count; ++i)
                {
                    Children[i].HasMoved = true;
                    Children[i].ProcessTransformChange();
                }
            }
        }

        public virtual string GetMouseOverText()
        {
            return Name;
        }

        private bool NeedsSpacialStorageUpdate(BoundingBox LastBounds, BoundingBox NewBounds)
        {
            var lastMinChunkID = GlobalVoxelCoordinate.FromVector3(LastBounds.Min).GetGlobalChunkCoordinate();
            var lastMaxChunkID = GlobalVoxelCoordinate.FromVector3(LastBounds.Max).GetGlobalChunkCoordinate();
            var newMinChunkID = GlobalVoxelCoordinate.FromVector3(NewBounds.Min).GetGlobalChunkCoordinate();
            var newMaxChunkID = GlobalVoxelCoordinate.FromVector3(NewBounds.Max).GetGlobalChunkCoordinate();
            return lastMinChunkID != newMinChunkID || lastMaxChunkID != newMaxChunkID;
        }

        public virtual void OnOutsideWorld()
        {
            if (!this.IsFlagSet(Flag.PreserveOutsideWorld))
                this.Die();
        }

        public void UpdateTransform()
        {
            HasMoved = false;

            if (Parent.HasValue(out var parent))
                globalTransform = LocalTransform * parent.GlobalTransform;
            else
                globalTransform = LocalTransform;

            UpdateBoundingBox();

            if (NeedsSpacialStorageUpdate(LastBounds, BoundingBox) || IsFlagSet(Flag.ForceSpacialUpdate))
            {
                SetFlag(Flag.ForceSpacialUpdate, false);
                if (IsRoot() && !IsFlagSet(Flag.DontUpdate))
                {
                    Manager.World.RemoveRootGameObject(this, LastBounds);
                    if (Manager.World.AddRootGameObject(this, BoundingBox) == 0)
                        this.OnOutsideWorld();
                }

                this.OnSpacialStorageUpdate(LastBounds, BoundingBox);
            }

            LastBounds = BoundingBox;
        }

        public virtual void OnSpacialStorageUpdate(BoundingBox LastBounds, BoundingBox NewBounds)
        {

        }

        public void PropogateTransforms()
        {
            UpdateTransform();
            for (var i = 0; i < Children.Count; ++i)
                Children[i].PropogateTransforms();
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
        
        public virtual void CreateCosmeticChildren(ComponentManager Manager)
        {
            //PropogateTransforms();
            //base.CreateCosmeticChildren(Manager);
        }

        public void Face(Vector3 target)
        {
            Vector3 diff = target - GlobalTransform.Translation;
            Matrix newTransform = Matrix.CreateRotationY((float)Math.Atan2(diff.X, -diff.Z));
            newTransform.Translation = LocalTransform.Translation;
            LocalTransform = newTransform;
        }

        public virtual void RemoveFromOctTree()
        {
            if (Manager != null)
            {
                //Manager.World.RemoveGameObject(this, LastBounds);
                if (IsRoot() && !IsFlagSet(Flag.DontUpdate))
                    Manager.World.RemoveRootGameObject(this, LastBounds);
            }
        }
    }
}
