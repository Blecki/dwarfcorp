using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class Spigot : CraftedBody
    {
        public class SpigotPipeObject : PipeNetworkObject
        {
            public SpigotPipeObject(ComponentManager Manager) : base(Manager) { }

            public override void OnPipeNetworkUpdate()
            {
                var voxel = World.ChunkManager.CreateVoxelHandle(this.Coordinate);
                if (voxel.IsValid && this.Pressure > 0.1f && voxel.LiquidLevel < WaterManager.maxWaterLevel && Library.GetLiquid("Water").HasValue(out var water))
                {

                    this.Pressure = 0.0f;
                    voxel.LiquidType = water.ID;
                    voxel.LiquidLevel += 1;
                }
            }
        }

        [EntityFactory("Spigot")]
        private static GameComponent __factory6(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Spigot(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public Spigot()
        {

        }

         public Spigot(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, "Spigot", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(manager, Resource))
        {
            var matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            LocalTransform = matrix;

            Tags.Add("Steam");
            CollisionType = CollisionType.Static;

            AddChild(new SpigotPipeObject(manager) {  });

            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32);

            AddChild(new SimpleSprite(Manager, "pipe", Matrix.CreateRotationX((float)Math.PI * 0.5f),
                spriteSheet, new Point(0, 6))
            {
                OrientationType = SimpleSprite.OrientMode.Fixed,
            }).SetFlagRecursive(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            base.Update(Time, Chunks, Camera);
        }
        
    }
}
