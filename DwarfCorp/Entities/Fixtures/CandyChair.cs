using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class CandyChair : CraftedBody
    {
        [EntityFactory("Candy Chair")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new CandyChair(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public CandyChair()
        {
        }

        public CandyChair(ComponentManager Manager, Vector3 Position, Resource Resource) :
            base(Manager, "Candy Chair", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(Manager, Resource))
        {
            var matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = Position;
            LocalTransform = matrix;

            Tags.Add("Chair");
            CollisionType = CollisionType.Static;
            CreateCosmeticChildren(base.Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32);

            AddChild(new SimpleSprite(Manager, "chair top", Matrix.CreateRotationX((float)Math.PI * 0.5f), spriteSheet, new Point(4, 4))
            {
                OrientationType = SimpleSprite.OrientMode.Fixed,
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new SimpleSprite(Manager, "chair legs 1", Matrix.CreateTranslation(0, -0.05f, 0), spriteSheet, new Point(4, 3))
            {
                OrientationType = SimpleSprite.OrientMode.Fixed
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new SimpleSprite(Manager, "chair legs 2",
                Matrix.CreateTranslation(0, -0.05f, 0) * Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, new Point(4, 3))
            {
                OrientationType = SimpleSprite.OrientMode.Fixed
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
