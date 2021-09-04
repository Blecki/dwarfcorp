using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Plant : GameComponent
    {
        private static List<Matrix> treeTransforms = new List<Matrix> { Matrix.Identity, Matrix.CreateRotationY((float)Math.PI / 2.0f) };
        private static List<Color> treeTints = new List<Color> { Color.White, Color.White };

        public bool IsGrown { get; set; }
        public string MeshAsset { get; set; }
        public float MeshScale { get; set; }
        public Vector3 BasePosition = Vector3.Zero;
        public float RandomAngle = 0.0f;
        public Farm Farm;
        public int LastGrowthHour = 0;

        public Plant()
        {
            //SetFlag(Flag.DontUpdate, true);
        }

        public Plant(ComponentManager Manager, string name, Vector3 Position, float RandomAngle, Vector3 bboxSize, string meshAsset, float meshScale) :
            base(Manager, name, Matrix.Identity, bboxSize, new Vector3(0.0f, bboxSize.Y / 2, 0.0f))
        {
            MeshAsset = meshAsset;
            MeshScale = meshScale;
            IsGrown = false;
            BasePosition = Position;
            this.RandomAngle = RandomAngle;

            // Needs this to ensure plants are initially placed correctly. Listener below only fires when voxels change.
            var under = new VoxelHandle(Manager.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(BasePosition - new Vector3(0.0f, 0.5f, 0.0f)));
            if (under.IsValid && under.RampType != RampType.None)
                LocalTransform = Matrix.CreateRotationY(RandomAngle) * Matrix.CreateTranslation(BasePosition - new Vector3(0.0f, 0.5f, 0.0f));
            else
                LocalTransform = Matrix.CreateRotationY(RandomAngle) * Matrix.CreateTranslation(BasePosition);

            CreateCosmeticChildren(Manager);

            //SetFlag(Flag.DontUpdate, true);
        }
        
        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            //PropogateTransforms();

            // Todo: Rather than passing the mesh name, create one in some kind of PrimitiveLibrary on the fly if it doesn't already exist.
            var mesh = AddChild(new InstanceMesh(
                Manager, 
                "Model",
                Matrix.CreateRotationY((float)(MathFunctions.Random.NextDouble() * Math.PI)) 
                    * Matrix.CreateScale(BoundingBoxSize.X, BoundingBoxSize.Y, BoundingBoxSize.Z) 
                    * Matrix.CreateTranslation(new Vector3(0, BoundingBoxSize.Y / 2, 0)), 
                MeshAsset,
                this.BoundingBoxSize, 
                Vector3.Zero));

            mesh.SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager,
                Matrix.Identity,
                new Vector3(0.25f, 0.25f, 0.25f), // Position just below surface.
                new Vector3(0.0f, -0.30f, 0.0f),
                (v) =>
                {
                    if (v.Type == VoxelEventType.VoxelTypeChanged)
                    {
                        if (v.NewVoxelType == 0)
                            Die();
                        if (Library.GetVoxelType(v.NewVoxelType).HasValue(out VoxelType soilType))
                            if (!soilType.IsSoil)
                                Die();
                    }
                    else if (v.Type == VoxelEventType.RampsChanged)
                    {
                        if (v.OldRamps != RampType.None && v.NewRamps == RampType.None)
                            LocalTransform = Matrix.CreateRotationY(RandomAngle) * Matrix.CreateTranslation(BasePosition);
                        else if (v.OldRamps == RampType.None && v.NewRamps != RampType.None)
                            LocalTransform = Matrix.CreateRotationY(RandomAngle) * Matrix.CreateTranslation(BasePosition - new Vector3(0.0f, 0.5f, 0.0f));
                        ProcessTransformChange();
                    }
                }))
                .SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(Manager);
        }

        public override void Die()
        {
            if (Farm != null && !(this is Seedling))
            {
                if (Farm.Voxel.IsValid && Farm.Voxel.Type.Name == "TilledSoil" && !String.IsNullOrEmpty(Farm.SeedType))
                {
                    var farmTile = new Farm
                    {
                        Voxel = Farm.Voxel,
                        SeedType = Farm.SeedType,
                    };

                    if (GameSettings.Current.AllowAutoFarming)
                    {
                        var task = new PlantTask(farmTile);
                        World.TaskManager.AddTask(task);
                    }
                }
            }
            base.Die();
        }

        public void ReScale(float Scale)
        {
            BoundingBoxSize = new Vector3(1.0f, Scale, 1.0f);
            LocalBoundingBoxOffset = new Vector3(0.0f, Scale / 2.0f, 0.0f);
            UpdateBoundingBox();

            if (GetComponent<InstanceMesh>().HasValue(out var mesh))
                mesh.LocalTransform = Matrix.CreateScale(1.0f, Scale, 1.0f) * Matrix.CreateTranslation(GetBoundingBox().Center() - Position);
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            //SetFlag(Flag.DontUpdate, true);
            //World.RemoveRootGameObject(this, BoundingBox);
            //return;

            base.Update(Time, Chunks, Camera);

            if (!Active)
                return;

            // Todo: Move this to an update system.
            //var currentHour = World.Time.CurrentDate.Hour;
            //if (currentHour != LastGrowthHour)
            //{
            //    LastGrowthHour = currentHour;
            //    if (!GetRoot().GetComponent<Seedling>().HasValue(out var seedling) && MathFunctions.RandEvent(0.01f))
            //        if (World.EnumerateIntersectingObjects(GetBoundingBox().Expand(2)).Count(b => b is Plant) < 10)
            //        {
            //            Vector3 randomPoint = MathFunctions.RandVector3Box(GetBoundingBox().Expand(4));
            //            randomPoint.Y = World.WorldSizeInVoxels.Y - 1;
               
            //            VoxelHandle under = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(randomPoint)));
            //            if (under.IsValid && under.Type.IsSoil)
            //                EntityFactory.CreateEntity<Seedling>(Name + " Sprout", under.GetBoundingBox().Center() + Vector3.Up);
            //        }
            //}
           
        }

        protected void CreateCrossPrimitive(String Asset)
        {
            if (!Manager.World.Renderer.InstanceRenderer.DoesGroupExist(Asset))
                Manager.World.Renderer.InstanceRenderer.AddInstanceGroup(new PrimitiveInstanceGroup
                {
                    RenderData = new InstanceRenderData
                    {
                        EnableGhostClipping = true,
                        EnableWind = true,
                        RenderInSelectionBuffer = true,
                        Model = CreateCrossPrimitive(new NamedImageFrame(Asset))
                    },
                    Name = Asset
                });
        }

        protected void CreateQuadPrimitive(String Asset)
        {
            if (!Manager.World.Renderer.InstanceRenderer.DoesGroupExist(Asset))
                Manager.World.Renderer.InstanceRenderer.AddInstanceGroup(new PrimitiveInstanceGroup
                {
                    RenderData = new InstanceRenderData
                    {
                        EnableGhostClipping = true,
                        EnableWind = true,
                        RenderInSelectionBuffer = true,
                        Model = CreateQuadPrimitive(new NamedImageFrame(Asset))
                    },
                    Name = Asset
                });
        }

        private static GeometricPrimitive CreateCrossPrimitive(NamedImageFrame spriteSheet)
        {
            int width = spriteSheet.SafeGetImage().Width;
            int height = spriteSheet.SafeGetImage().Height;

            return new BatchBillboardPrimitive(spriteSheet, width, height,
                new Point(0, 0), 1.0f, 1.0f, false, treeTransforms, treeTints, treeTints);
        }

        private static GeometricPrimitive CreateQuadPrimitive(NamedImageFrame spriteSheet)
        {
            int width = spriteSheet.SafeGetImage().Width;
            int height = spriteSheet.SafeGetImage().Height;

            return new BatchBillboardPrimitive(spriteSheet, width, height,
                new Point(0, 0), 1.0f, 1.0f, false, treeTransforms.Take(1).ToList(), treeTints, treeTints);
        }
    }
}
