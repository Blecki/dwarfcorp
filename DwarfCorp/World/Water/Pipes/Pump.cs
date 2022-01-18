using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class Pump : CraftedBody
    {
        public class PumpPipeObject : PipeNetworkObject
        {
            public PumpPipeObject() : base() { }

            public PumpPipeObject(ComponentManager Manager) : base(Manager) { }

            public override void OnPipeNetworkUpdate(WorldManager World, PipeSystem System)
            {
                var voxel = World.ChunkManager.CreateVoxelHandle(this.Coordinate);
                if (voxel.IsValid)
                {
                    var filledCells = LiquidCellHelpers.EnumerateFilledCellsInVovel(voxel);
                    if (filledCells.Count() > 0)
                    {
                        var l = filledCells.First();

                        var destinationCell = System.SearchNetwork(World, this, (n) => n.LiquidType == 0);
                        if (destinationCell != null)
                        {
                            destinationCell.LiquidType = l.LiquidType;
                            l.LiquidType = 0;
                            foreach (var cell in LiquidCellHelpers.EnumerateAllNeighbors(l.Coordinate).Select(c => World.ChunkManager.CreateLiquidCellHandle(c)))
                                if (cell.IsValid) World.ChunkManager.Water.EnqueueDirtyCell(cell);
                        }
                    }
                }
            }
        }

        [EntityFactory("Pump")]
        private static GameComponent __factory6(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Pump(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public Pump()
        {

        }

         public Pump(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, "Pump", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(manager, Resource))
        {
            var matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            LocalTransform = matrix;

            Tags.Add("Steam");
            CollisionType = CollisionType.Static;

            AddChild(new PumpPipeObject(manager) {  });

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
