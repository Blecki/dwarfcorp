﻿// Flag.cs
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
    [JsonObject(IsReference = true)]
    public class Flag : Body
    {
        public Flag()
        {

        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);
        }

        public Flag(Vector3 position) :
            base("Flag", WorldManager.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            SpriteSheet spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture);
            List<Point> frames = new List<Point>
            {
                new Point(0, 2),
                new Point(1, 2),
                new Point(2, 2)
            };
            Animation lampAnimation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), "Flag", 32, 32, frames, true, Color.White, 5.0f + MathFunctions.Rand(), 1f, 1.0f, false);

            Sprite sprite = new Sprite(WorldManager.ComponentManager, "sprite", this, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.YAxis
            };
            sprite.AddAnimation(lampAnimation);



            Voxel voxelUnder = new Voxel();

            if (WorldManager.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(WorldManager.ComponentManager, this, WorldManager.ChunkManager, voxelUnder);
            }

            lampAnimation.Play();
            Tags.Add("Flag");

            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
