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
        public bool IsGrown { get; set; }
        public string MeshAsset { get; set; }
        public float MeshScale { get; set; }
        public Vector3 BasePosition = Vector3.Zero;
        public float RandomAngle = 0.0f;
        public Farm Farm;
        public int LastGrowthHour = 0;
        public Plant()
        {
        }

        public Plant(ComponentManager Manager, string name, Vector3 Position, float RandomAngle, Vector3 bboxSize,
           string meshAsset, float meshScale) :
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
        }
        
        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            PropogateTransforms();

            // Todo: Rather than passing the mesh name, create one in some kind of PrimitiveLibrary on the fly if it doesn't already exist.
            var mesh = AddChild(new InstanceMesh(Manager, "Model",
                Matrix.CreateRotationY((float)(MathFunctions.Random.NextDouble() * Math.PI)) * Matrix.CreateScale(MeshScale, MeshScale, MeshScale) * Matrix.CreateTranslation(GetBoundingBox().Center() - Position), MeshAsset,
                this.BoundingBoxSize, Vector3.Zero));

            mesh.SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager,
                Matrix.Identity,
                new Vector3(0.25f, 0.25f, 0.25f), // Position just below surface.
                new Vector3(0.0f, -0.30f, 0.0f),
                (v) =>
                {
                    if (v.Type == VoxelChangeEventType.VoxelTypeChanged
                        && (v.NewVoxelType == 0 || !VoxelLibrary.GetVoxelType(v.NewVoxelType).IsSoil))
                    {
                        Die();
                    }
                    else if (v.Type == VoxelChangeEventType.RampsChanged)
                    {
                        if (v.OldRamps != RampType.None && v.NewRamps == RampType.None)
                            LocalTransform = Matrix.CreateRotationY(RandomAngle) * Matrix.CreateTranslation(BasePosition);
                        else if (v.OldRamps == RampType.None && v.NewRamps != RampType.None)
                            LocalTransform = Matrix.CreateRotationY(RandomAngle) * Matrix.CreateTranslation(BasePosition - new Vector3(0.0f, 0.5f, 0.0f));
                    }
                }))
                .SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(Manager);
        }

        public override void Die()
        {
            if (Farm != null && !(this is Seedling))
            {
                if (Farm.Voxel.IsValid && Farm.Voxel.Type.Name == "TilledSoil" && !String.IsNullOrEmpty(Farm.SeedString))
                {
                    var farmTile = new Farm
                    {
                        Voxel = Farm.Voxel,
                        SeedString = Farm.SeedString,
                        RequiredResources = Farm.RequiredResources
                    };

                    if (GameSettings.Default.AllowAutoFarming)
                    {
                        var task = new PlantTask(farmTile)
                        {
                            Plant = Farm.SeedString,
                            RequiredResources = Farm.RequiredResources
                        };
                        World.Master.TaskManager.AddTask(task);
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

            var mesh = GetComponent<InstanceMesh>();
            if (mesh != null)
                mesh.LocalTransform = Matrix.CreateScale(1.0f, Scale, 1.0f) * Matrix.CreateTranslation(GetBoundingBox().Center() - Position);
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            base.Update(Time, Chunks, Camera);

            if (!Active)
                return;

            var currentHour = World.Time.CurrentDate.Hour;
            if (currentHour != LastGrowthHour)
            {
                LastGrowthHour = currentHour;
                var isSeedling = GetRoot().GetComponent<Seedling>() != null;
               
                if (!isSeedling && MathFunctions.RandEvent(0.01f))
                {
                    var bodies = World.EnumerateIntersectingObjects(GetBoundingBox().Expand(2));

                    int numPlants = bodies.Count(b => b is Plant);
                    if (numPlants < 10)
                    {
                        Vector3 randomPoint = MathFunctions.RandVector3Box(GetBoundingBox().Expand(4));
                        randomPoint.Y = World.WorldSizeInVoxels.Y - 1;
               
                        VoxelHandle under = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(randomPoint)));
                        if (under.IsValid && under.Type.IsSoil)
                        {
                            EntityFactory.CreateEntity<Seedling>(Name + " Sprout", under.GetBoundingBox().Center() + Vector3.Up);
                        }
                    }
                }
            }
           
        }

        protected void CreateCrossPrimitive(String Asset)
        {
            if (!Manager.World.InstanceRenderer.DoesGroupExist(Asset))
                Manager.World.InstanceRenderer.AddInstanceGroup(new PrimitiveInstanceGroup
                {
                    RenderData = new InstanceRenderData
                    {
                        EnableGhostClipping = true,
                        EnableWind = true,
                        RenderInSelectionBuffer = true,
                        Model = PrimitiveLibrary.CreateCrossPrimitive(new NamedImageFrame(Asset))
                    },
                    Name = Asset
                });
        }
    }
}
