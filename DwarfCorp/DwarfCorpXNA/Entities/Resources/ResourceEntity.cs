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
        public ResourceAmount Resource { get; set; }

        public ResourceEntity()
        {
            
        }

        public ResourceEntity(ComponentManager manager, ResourceAmount resourceType, Vector3 position) :
            base(manager, ResourceLibrary.Resources[resourceType.ResourceType].ResourceName, 
                Matrix.CreateTranslation(position), new Vector3(0.25f, 0.25f, 0.25f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0))
        {
            Resource = resourceType;
            if (Resource.NumResources > 1)
            {
                Name = String.Format("Pile of {0} {1}", Resource.NumResources, Resource.ResourceType);
            }
            Restitution = 0.1f;
            Friction = 0.1f;
            Resource type = ResourceLibrary.Resources[resourceType.ResourceType];
            
            Tags.Add(type.ResourceName);
            Tags.Add("Resource");
            
            if (type.IsFlammable)
            {
                AddChild(new Health(Manager, "health", 10.0f, 0.0f, 10.0f));
                AddChild(new Flammable(Manager, "Flames"));
            }

            PropogateTransforms();
            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            Resource type = ResourceLibrary.GetResourceByName(Resource.ResourceType);

            Tinter sprite = null;

            if (type.CompositeLayers == null || type.CompositeLayers.Count == 0)
            {
                sprite = AddChild(new SimpleSprite(Manager, "Sprite",
                    Matrix.CreateTranslation(Vector3.UnitY * 0.25f),
                    false,
                    new SpriteSheet(type.Image.AssetName, 32),
                    new Point(type.Image.SourceRect.X / 32, type.Image.SourceRect.Y / 32))
                {
                    OrientationType = SimpleSprite.OrientMode.Spherical,
                    WorldHeight = 0.5f,
                    WorldWidth = 0.5f,
                }) as Tinter;
            }
            else
            {
                var layers = new List<LayeredSimpleSprite.Layer>();

                foreach (var layer in type.CompositeLayers)
                {
                    layers.Add(new LayeredSimpleSprite.Layer
                    {
                        Sheet = new SpriteSheet(layer.Value, 32),
                        Frame = layer.Key
                    });
                }

                sprite = AddChild(new LayeredSimpleSprite(Manager, "Sprite",
                    Matrix.CreateTranslation(Vector3.UnitY * 0.25f),
                    false,
                    layers)
                {
                    OrientationType = LayeredSimpleSprite.OrientMode.Spherical,
                    WorldHeight = 0.5f,
                    WorldWidth = 0.5f,
                }) as Tinter;
            }

            sprite.Tint = type.Tint;
            sprite.SetFlag(Flag.ShouldSerialize, false);

            sprite.AddChild(new Bobber(Manager, 0.05f, 2.0f, MathFunctions.Rand() * 3.0f, sprite.LocalTransform.Translation.Y)).SetFlag(Flag.ShouldSerialize, false);
        }

    }
}
