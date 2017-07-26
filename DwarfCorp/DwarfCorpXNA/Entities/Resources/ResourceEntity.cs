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

        public ResourceEntity(ComponentManager manager, ResourceLibrary.ResourceType resourceType, Vector3 position) :
            base(manager, ResourceLibrary.Resources[resourceType].ResourceName, Matrix.CreateTranslation(position), new Vector3(0.25f, 0.25f, 0.25f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0))
        {
            Restitution = 0.1f;
            Friction = 0.1f;
            Resource type = ResourceLibrary.Resources[resourceType];


            var sprite =
                AddChild(new Sprite(Manager, "Sprite", Matrix.CreateTranslation(Vector3.UnitY*0.25f), null,
                    false)
                {
                    OrientationType = Sprite.OrientMode.Spherical,
                    LightsWithVoxels = !type.SelfIlluminating
                }) as Sprite;

            if (type.CompositeLayers == null || type.CompositeLayers.Count == 0)
            {
                int frameX = type.Image.SourceRect.X/32;
                int frameY = type.Image.SourceRect.Y/32;

                List<Point> frames = new List<Point>
                {
                    new Point(frameX, frameY)
                };
                Animation animation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(type.Image.AssetName),
                    "Animation", 32, 32, frames, false, type.Tint, 0.01f, 0.75f, 0.75f, false);;
                sprite.AddAnimation(animation);


                animation.Play();
            }
            else
            {
                List<Composite.Frame> frames = new List<Composite.Frame>();
                var frame = new Composite.Frame();
                frame.Layers = new List<SpriteSheet>();
                foreach (var layer in type.CompositeLayers)
                {
                    frame.Layers.Add(new SpriteSheet(layer.Value, 32));
                    frame.Positions.Add(layer.Key);
                    frame.Tints.Add(type.Tint);
                }
                frames.Add(frame);
                CompositeAnimation compositeAnimation = new CompositeAnimation("resources", frames)
                {
                    CompositeName = "resources",
                    CurrentFrame = 0,
                    CurrentOffset = new Point(0, 0),
                    FrameWidth = 32,
                    FrameHeight = 32,
                    FrameHZ = 1,
                    Name = "Composite resource",
                    Tint = type.Tint
                };

                sprite.AddAnimation(compositeAnimation);
                compositeAnimation.Play();
            }

            Tags.Add(type.ResourceName);
            Tags.Add("Resource");
            var bobber = sprite.AddChild(new Bobber(Manager, 0.05f, 2.0f, MathFunctions.Rand() * 3.0f, sprite.LocalTransform.Translation.Y));


            if (type.IsFlammable)
            {
                AddChild(new Health(Manager, "health", 10.0f, 0.0f, 10.0f));
                AddChild(new Flammable(Manager, "Flames"));
            }
        }

    }
}
