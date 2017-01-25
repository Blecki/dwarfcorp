// Bed.cs
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
    public class Bed : Body
    {
        private Box bedModel;
        public Bed()
        {
            
        }

        public Bed(Vector3 position) :
            base("Bed", WorldManager.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(1.5f, 0.5f, 0.75f), new Vector3(-0.5f + 1.5f * 0.5f, -0.5f + 0.25f, -0.5f + 0.75f * 0.5f))
        {
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bedtex);
            bedModel = new Box(WorldManager.ComponentManager, "bedbox", this, Matrix.CreateTranslation(-0.5f, -0.5f, -0.5f) * Matrix.CreateRotationY((float)Math.PI * 0.5f), new Vector3(1.0f, 1.0f, 2.0f), new Vector3(0.5f, 0.5f, 1.0f), "bed", spriteSheet);

            Voxel voxelUnder = new Voxel();


            if (WorldManager.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(WorldManager.ComponentManager, this, WorldManager.ChunkManager, voxelUnder);
            }

            Tags.Add("Bed");
            CollisionType = CollisionManager.CollisionType.Static;
        }

    }

    [JsonObject(IsReference = true)]
    public class Bookshelf : Body
    {
        private Box bedModel;
        public Bookshelf()
        {

        }
        // 20 x 8 x 32
        public Bookshelf(Vector3 position) :
            base("Bookshelf", WorldManager.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(32.0f / 32.0f, 8.0f / 32.0f, 20.0f / 32.0f), new Vector3(0.5f, 0.5f, 0.5f))
        {
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bookshelf);
            bedModel = new Box(WorldManager.ComponentManager, "model", this, Matrix.CreateTranslation(new Vector3(-20.0f / 64.0f, -32.0f / 64.0f, -8.0f / 64.0f)), new Vector3(32.0f / 32.0f, 8.0f / 32.0f, 20.0f / 32.0f), new Vector3(0.0f, 0.0f, 0.0f), "bookshelf", spriteSheet);
           
            Voxel voxelUnder = new Voxel();


            if (WorldManager.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(WorldManager.ComponentManager, this, WorldManager.ChunkManager, voxelUnder);
            }

            Tags.Add("Books");
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public override void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }
    }
}
