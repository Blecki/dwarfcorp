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
        private static Point DefaultTopSprite = new Point(2, 6);
        private static Point DefaultLegsSprite = new Point(3, 6);
        public Point TopSprite;
        public Point LegsSprite;

        [EntityFactory("Chair")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chair(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null), "Wooden Chair", DefaultTopSprite, DefaultLegsSprite);
        }

        [EntityFactory("Wooden Chair")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chair(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null), "Wooden Chair", DefaultTopSprite, DefaultLegsSprite);
        }

        [EntityFactory("Stone Chair")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chair(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null), "Stone Chair", new Point(6, 6), new Point(7, 6));
        }

        [EntityFactory("Iron Chair")]
        private static GameComponent __factory3(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chair(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null), "Iron Chair", new Point(6, 7), new Point(7, 7));
        }

        private void Initialize(ComponentManager manager)
        {
            Tags.Add("Chair");
            CollisionType = CollisionType.Static;
        }

        public Chair()
        {
        }

        public Chair(ComponentManager manager, Vector3 position, List<ResourceAmount> resources, string craftType, Point topSprite, Point legsSprite) :
            base(manager, "Chair", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(manager, craftType, resources))
        {
            TopSprite = topSprite;
            LegsSprite = legsSprite;
            var matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            LocalTransform = matrix;

            Initialize(Manager);
            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32);

            AddChild(new SimpleSprite(Manager, "chair top", Matrix.CreateRotationX((float)Math.PI * 0.5f),
                spriteSheet, TopSprite)
            {
                OrientationType = SimpleSprite.OrientMode.Fixed,
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new SimpleSprite(Manager, "chair legs 1", Matrix.CreateTranslation(0, -0.05f, 0),
                spriteSheet, LegsSprite)
            {
                OrientationType = SimpleSprite.OrientMode.Fixed
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new SimpleSprite(Manager, "chair legs 2",
                Matrix.CreateTranslation(0, -0.05f, 0) * Matrix.CreateRotationY((float)Math.PI * 0.5f),
                spriteSheet, LegsSprite)
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
