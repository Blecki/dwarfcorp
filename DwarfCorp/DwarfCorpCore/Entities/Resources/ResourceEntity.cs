// ResourceEntity.cs
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
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class ResourceEntity : Physics
    {
        public ResourceEntity()
        {
            
        }

        public ResourceEntity(ResourceLibrary.ResourceType resourceType, Vector3 position) :
            base(ResourceLibrary.Resources[resourceType].ResourceName, World.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.25f, 0.25f, 0.25f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0))
        {
            Restitution = 0.1f;
            Friction = 0.1f;
            Resource type = ResourceLibrary.Resources[resourceType];
            SpriteSheet spriteSheet = new SpriteSheet(type.Image.AssetName);

            int frameX = type.Image.SourceRect.X / 32;
            int frameY = type.Image.SourceRect.Y / 32;

            List<Point> frames = new List<Point>
            {
                new Point(frameX, frameY)
            };
            Animation animation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(type.Image.AssetName), "Animation", 32, 32, frames, false, type.Tint, 0.01f, 0.75f, 0.75f, false);

            Sprite sprite = new Sprite(World.ComponentManager, "Sprite", this, Matrix.CreateTranslation(Vector3.UnitY * 0.25f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Spherical,
                LightsWithVoxels = !type.SelfIlluminating
            };
            sprite.AddAnimation(animation);


            animation.Play();

            Tags.Add(type.ResourceName);
            Tags.Add("Resource");
            Bobber bobber = new Bobber(0.05f, 2.0f, MathFunctions.Rand() * 3.0f, sprite);


            if (type.IsFlammable)
            {
                Health health = new Health(World.ComponentManager, "health", this, 10.0f, 0.0f, 10.0f);
                new Flammable(World.ComponentManager, "Flames", this, health);

            }
        }
    }
}
