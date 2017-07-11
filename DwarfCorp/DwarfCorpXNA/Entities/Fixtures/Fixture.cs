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
    [JsonObject(IsReference = true)]
    public class Fixture : Body
    {
        public SpriteSheet Asset { get; set; }
        public Point Frame { get; set; }

        public Fixture()
        {
            
        }

        public Fixture(ComponentManager Manager, Vector3 position, SpriteSheet asset, Point frame) :
            base(Manager, "Fixture", Matrix.CreateTranslation(position), new Vector3(asset.FrameWidth * (1.0f / 32.0f), asset.Height * (1.0f / 32.0f), asset.FrameWidth * (1.0f / 32.0f)), Vector3.Zero, true)
        {
            Asset = asset;
            Frame = frame;
            var sprite = AddChild(new Sprite(Manager, "Sprite", Matrix.Identity, asset, false)) as Sprite;
            sprite.AddAnimation(new Animation(asset.GenerateFrame(frame)));
            sprite.SetFlag(Flag.ShouldSerialize, false);
            AddToCollisionManager = false;
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            var sprite = AddChild(new Sprite(manager, "Sprite", Matrix.Identity, Asset, false)) as Sprite;
            sprite.AddAnimation(new Animation(Asset.GenerateFrame(Frame)));
            sprite.SetFlag(Flag.ShouldSerialize, false);
            base.CreateCosmeticChildren(manager);
        }
    }
}
