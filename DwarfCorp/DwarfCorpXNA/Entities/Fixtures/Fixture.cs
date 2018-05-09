// Fixture.cs
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
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Fixture : Body
    {
        public SpriteSheet Asset;
        public Point Frame;
        public SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical;

        public Fixture()
        {
            
        }

        public Fixture(
            ComponentManager Manager, 
            Vector3 position, 
            SpriteSheet asset, 
            Point frame,
            SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical) :
            base(
                Manager, 
                "Fixture", 
                Matrix.CreateTranslation(position), 
                new Vector3(asset.FrameWidth / 32.0f, asset.FrameHeight / 32.0f, asset.FrameWidth / 32.0f) * 0.9f, 
                Vector3.Zero, true)
        {
            Asset = asset;
            Frame = frame;
            CollisionType = CollisionType.Static;
            this.OrientMode = OrientMode;

            AddChild(new Health(Manager, "Hp", 100, 0, 100));

            PropogateTransforms();
            CreateCosmeticChildren(Manager);
        }

        public Fixture(
            String Name,
            IEnumerable<String> Tags,
            ComponentManager Manager,
            Vector3 position,
            SpriteSheet asset,
            Point frame,
            SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical) :
            this(Manager, position, asset, frame, OrientMode)
        { 
            this.Name = Name;
            this.Tags.AddRange(Tags);
        }
        
        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            AddChild(new SimpleSprite(manager, "Sprite", Matrix.Identity, Asset, Frame)
            {
                OrientationType = OrientMode
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }            
    }
}
