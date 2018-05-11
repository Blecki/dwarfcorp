// Forge.cs
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
    public class Forge : CraftedBody
    {
        [EntityFactory("Forge")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Forge(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
        }

        public Forge()
        {

        }

        public Forge(ComponentManager manager, Vector3 position, List<ResourceAmount> resources) :
            base(manager, "Forge", Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new DwarfCorp.CraftDetails(manager, "Forge", resources))
        {
            Tags.Add("Forge");

            CollisionType = CollisionType.Static;

            CreateCosmeticChildren(manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32);

            var frames = new List<Point>
            {
                new Point(1, 3),
                new Point(3, 3),
                new Point(2, 3),
                new Point(3, 3)
            };

            var forgeAnimation = AnimationLibrary.CreateAnimation(spriteSheet, frames, "ForgeLightAnimation");
            forgeAnimation.Loops = true;

            var sprite = AddChild(new AnimatedSprite(Manager, "sprite", Matrix.Identity, false)
            {
                LightsWithVoxels = false
            }) as AnimatedSprite;

            sprite.AddAnimation(forgeAnimation);
            sprite.AnimPlayer.Play(forgeAnimation);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            AddChild(new LightEmitter(Manager, "light", Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 50, 4)
            {
                HasMoved = true
            }).SetFlag(Flag.ShouldSerialize, false);

            // This is a hack to make the animation update at least once even when the object is created inactive by the craftbuilder.
            sprite.AnimPlayer.Update(new DwarfTime(), false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
