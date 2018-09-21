// Table.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Table : CraftedBody
    {
        private static Point DefaultTopFrame = new Point(0, 6);
        private static Point DefaultLegsFrame = new Point(1, 6);

        [EntityFactory("Table")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Wooden Table", Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null), DefaultTopFrame, DefaultLegsFrame);
        }

        [EntityFactory("Cutting Board")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Cutting Board", Manager, Position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(0, 7), Data.GetData<List<ResourceAmount>>("Resources", null), DefaultTopFrame, DefaultLegsFrame)
            {
                Tags = new List<string>() { "Cutting Board" }
            };
        }

        [EntityFactory("Apocethary")]
        private static GameComponent __factory3(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Apocethary", Manager, Position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(1, 4), Data.GetData<List<ResourceAmount>>("Resources", null), DefaultTopFrame, DefaultLegsFrame)
            {
                Tags = new List<string>() { "Research", "Apocethary" }
            };
        }

        [EntityFactory("Wooden Table")]
        private static GameComponent __factory4(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Wooden Table", Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null), DefaultTopFrame, DefaultLegsFrame);
        }


        [EntityFactory("Stone Table")]
        private static GameComponent __factory5(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Stone Table", Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null), new Point(4, 6), new Point(5, 6));
        }


        [EntityFactory("Metal Table")]
        private static GameComponent __factory6(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Table("Metal Table", Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null), new Point(4, 7), new Point(5, 7));
        }

        public SpriteSheet fixtureAsset;
        public Point fixtureFrame;

        public Point TopFrame = new Point(0, 6);
        public Point LegsFrame = new Point(1, 6);

        public Table()
        {
            
        }

        public Table(string craftType, ComponentManager componentManager, Vector3 position, List<ResourceAmount> resources, Point topFrame, Point legsFrame) :
            this(craftType, componentManager, position, null, Point.Zero, resources, topFrame, legsFrame)
        {
            
        }

        public Table(string craftType, ComponentManager manager, Vector3 position, string asset, List<ResourceAmount> resources, Point topFrame, Point legsFrame) :
            this(craftType, manager, position, new SpriteSheet(asset), Point.Zero, resources, topFrame, legsFrame)
        {

        }

        public Table(string craftType, ComponentManager manager, Vector3 position, SpriteSheet fixtureAsset, Point fixtureFrame, List<ResourceAmount> resources, Point topFrame, Point legsFrame) :
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
