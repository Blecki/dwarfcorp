﻿// Mushroom.cs
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
    public class Mushroom : Plant
    {
        public Mushroom()
        {

        }

        public Mushroom(Vector3 position, 
                        string asset, 
                        ResourceLibrary.ResourceType resource, 
                        int numRelease, bool selfIlluminate) :
            base("Mushroom", WorldManager.ComponentManager.RootComponent, Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            Seedlingsheet = new SpriteSheet(ContentPaths.Entities.Plants.deadbush, 32, 32);
            SeedlingFrame = new Point(0, 0);
            Matrix matrix = Matrix.CreateRotationY(MathFunctions.Rand(-0.1f, 0.1f));
            matrix.Translation = position + new Vector3(0.5f, -0.25f, 0.5f);
            LocalTransform = matrix;

            SpriteSheet spriteSheet = new SpriteSheet(asset);

            List<Point> frames = new List<Point>
            {
                new Point(0, 0)
            };
            Animation animation = new Animation(GameState.Game.GraphicsDevice, spriteSheet, "Mushroom", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite sprite = new Sprite(WorldManager.ComponentManager, "sprite", this, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed,
                LightsWithVoxels = !selfIlluminate
            };
            sprite.AddAnimation(animation);

            Sprite sprite2 = new Sprite(WorldManager.ComponentManager, "sprite2", this, Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed,
                LightsWithVoxels = !selfIlluminate
            };
            sprite2.AddAnimation(animation);

            Voxel voxelUnder = new Voxel();
            bool success = WorldManager.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder);

            if (success)
            {
                VoxelListener listener = new VoxelListener(WorldManager.ComponentManager, this, WorldManager.ChunkManager, voxelUnder);
            }

            Inventory inventory = new Inventory("Inventory", this)
            {
                Resources = new ResourceContainer()
                {
                    MaxResources = 2,
                    Resources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>()
                    {
                        {
                            resource,
                            new ResourceAmount(resource, numRelease)
                        }
                    }
                }
            };
            
            Health health = new Health(WorldManager.ComponentManager, "HP", this, 30, 0.0f, 30);
            new Flammable(WorldManager.ComponentManager, "Flames", this, health);

            animation.Play();
            Tags.Add("Mushroom");
            Tags.Add("Vegetation");
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
