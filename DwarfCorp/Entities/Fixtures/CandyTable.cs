using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class CandyTable : CraftedBody
    {
        private static Point DefaultTopFrame = new Point(5, 4);
        private static Point DefaultLegsFrame = new Point(5, 3);

        [EntityFactory("Candy Table")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new CandyTable("Wooden Table", Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null), DefaultTopFrame, DefaultLegsFrame);
        }

        public SpriteSheet fixtureAsset;
        public Point fixtureFrame;

        public Point TopFrame = new Point(0, 6);
        public Point LegsFrame = new Point(1, 6);

        public CandyTable()
        {
            
        }

        public CandyTable(string craftType, ComponentManager componentManager, Vector3 position, List<ResourceAmount> resources, Point topFrame, Point legsFrame) :
            this(craftType, componentManager, position, null, Point.Zero, resources, topFrame, legsFrame)
        {
            
        }

        public CandyTable(string craftType, ComponentManager manager, Vector3 position, string asset, List<ResourceAmount> resources, Point topFrame, Point legsFrame) :
            this(craftType, manager, position, new SpriteSheet(asset), Point.Zero, resources, topFrame, legsFrame)
        {

        }

        public CandyTable(string craftType, ComponentManager manager, Vector3 position, SpriteSheet fixtureAsset, Point fixtureFrame, List<ResourceAmount> resources, Point topFrame, Point legsFrame) :
            base(manager, craftType, Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new DwarfCorp.CraftDetails(manager, craftType, resources))
        {
            TopFrame = topFrame;
            LegsFrame = legsFrame;
            this.fixtureAsset = fixtureAsset;
            this.fixtureFrame = fixtureFrame;

            var matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            LocalTransform = matrix;

            Tags.Add("Table");
            CollisionType = CollisionType.Static;

            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32);

            AddChild(new SimpleSprite(Manager, "chair top", Matrix.CreateRotationX((float)Math.PI * 0.5f),
                spriteSheet, TopFrame)
            {
                OrientationType = SimpleSprite.OrientMode.Fixed,
            }).SetFlagRecursive(Flag.ShouldSerialize, false);

            AddChild(new SimpleSprite(Manager, "chair legs 1", Matrix.CreateTranslation(0, -0.05f, 0),
                spriteSheet, LegsFrame)
            {
                OrientationType = SimpleSprite.OrientMode.Fixed
            }).SetFlagRecursive(Flag.ShouldSerialize, false);

            AddChild(new SimpleSprite(Manager, "chair legs 2",
                Matrix.CreateTranslation(0, -0.05f, 0) * Matrix.CreateRotationY((float)Math.PI * 0.5f),
                spriteSheet, LegsFrame)
            {
                OrientationType = SimpleSprite.OrientMode.Fixed
            }).SetFlagRecursive(Flag.ShouldSerialize, false);

            if (fixtureAsset != null)
                AddChild(new SimpleSprite(Manager, "", Matrix.CreateTranslation(new Vector3(0, 0.3f, 0)), fixtureAsset, fixtureFrame)).SetFlagRecursive(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
