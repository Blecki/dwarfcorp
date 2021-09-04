﻿using System;
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
    public class Table : CraftedBody
    {
        private static Point DefaultTopFrame = new Point(0, 6);
        private static Point DefaultLegsFrame = new Point(1, 6);

        [EntityFactory("Table")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Wooden Table", Manager, Position, Data.GetData<Resource>("Resource", null), DefaultTopFrame, DefaultLegsFrame);
        }

        [EntityFactory("Cutting Board")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Cutting Board", Manager, Position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(0, 7), Data.GetData<Resource>("Resource", null), DefaultTopFrame, DefaultLegsFrame)
            {
                Tags = new List<string>() { "Cutting Board" }
            };
        }

        [EntityFactory("Apothecary")]
        private static GameComponent __factory3(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Apothecary", Manager, Position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(1, 4), Data.GetData<Resource>("Resource", null), DefaultTopFrame, DefaultLegsFrame)
            {
                Tags = new List<string>() { "Research", "Apothecary" }
            };
        }

        [EntityFactory("Wooden Table")]
        private static GameComponent __factory4(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Wooden Table", Manager, Position, Data.GetData<Resource>("Resource", null), DefaultTopFrame, DefaultLegsFrame);
        }


        [EntityFactory("Stone Table")]
        private static GameComponent __factory5(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Stone Table", Manager, Position, Data.GetData<Resource>("Resource", null), new Point(4, 6), new Point(5, 6));
        }


        [EntityFactory("Iron Table")]
        private static GameComponent __factory6(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Iron Table", Manager, Position, Data.GetData<Resource>("Resource", null), new Point(4, 7), new Point(5, 7));
        }

        public SpriteSheet fixtureAsset;
        public Point fixtureFrame;

        public Point TopFrame = new Point(0, 6);
        public Point LegsFrame = new Point(1, 6);

        public Table()
        {
            
        }

        // Todo: Why so many constructors?
        public Table(string craftType, ComponentManager componentManager, Vector3 position, Resource Resource, Point topFrame, Point legsFrame) :
            this(craftType, componentManager, position, null, Point.Zero, Resource, topFrame, legsFrame)
        {
            
        }

        public Table(string craftType, ComponentManager manager, Vector3 position, string asset, Resource Resource, Point topFrame, Point legsFrame) :
            this(craftType, manager, position, new SpriteSheet(asset), Point.Zero, Resource, topFrame, legsFrame)
        {

        }

        public Table(string craftType, ComponentManager manager, Vector3 position, SpriteSheet fixtureAsset, Point fixtureFrame, Resource Resource, Point topFrame, Point legsFrame) :
            base(manager, craftType, Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(manager, Resource))
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
                if (changeEvent.Type == VoxelEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
