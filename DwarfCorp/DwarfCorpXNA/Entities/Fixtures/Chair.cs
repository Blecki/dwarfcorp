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
    public class Chair : CraftedBody
    {
        [EntityFactory("Chair")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chair(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
        }

        private void Initialize(ComponentManager manager)
        {
            Tags.Add("Chair");
            CollisionType = CollisionType.Static;
        }

        public Chair()
        {
        }

        public Chair(ComponentManager manager, Vector3 position, List<ResourceAmount> resources = null) :
            base(manager, "Chair", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(manager, "Chair", resources))
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

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32);

            AddChild(new SimpleSprite(Manager, "chair top", Matrix.CreateRotationX((float)Math.PI * 0.5f),
                spriteSheet, new Point(2, 6))
            {
                OrientationType = SimpleSprite.OrientMode.Fixed,
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new SimpleSprite(Manager, "chair legs 1", Matrix.CreateTranslation(0, -0.05f, 0),
                spriteSheet, new Point(3, 6))
            {
                OrientationType = SimpleSprite.OrientMode.Fixed
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new SimpleSprite(Manager, "chair legs 2",
                Matrix.CreateTranslation(0, -0.05f, 0) * Matrix.CreateRotationY((float)Math.PI * 0.5f),
                spriteSheet, new Point(3, 6))
            {
                OrientationType = SimpleSprite.OrientMode.Fixed
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
