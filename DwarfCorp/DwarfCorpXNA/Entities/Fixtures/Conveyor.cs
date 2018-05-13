using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DwarfCorp
{
    public class Conveyor : CraftedBody
    {
        [EntityFactory("Conveyor")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Conveyor(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
        }

        private void Initialize(ComponentManager manager)
        {
            Tags.Add("Conveyor");
            CollisionType = CollisionType.Static;
        }

        public Conveyor()
        {
        }

        public Conveyor(ComponentManager manager, Vector3 position, List<ResourceAmount> resources = null) :
            base(manager, "Conveyor", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(manager, "Conveyor", resources))
        {
            var matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position - new Vector3(0, 0.22f, 0);
            LocalTransform = matrix;

            Initialize(Manager);
            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.conveyor, 32);

            AddChild(new SimpleSprite(Manager, "top", Matrix.CreateRotationX((float)Math.PI * 0.5f),
                spriteSheet, new Point(0, 0))
            {
                OrientationType = SimpleSprite.OrientMode.Fixed,
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
