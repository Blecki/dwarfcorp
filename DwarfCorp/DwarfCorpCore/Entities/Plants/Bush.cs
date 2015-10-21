// Bush.cs
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
    public class Cactus : Body
    {
        public Cactus() { }

        public Cactus(Vector3 position, string asset, float bushSize) :
            base("Cactus", PlayState.ComponentManager.RootComponent, Matrix.Identity, new Vector3(bushSize, bushSize, bushSize), Vector3.Zero)
        {
            ComponentManager componentManager = PlayState.ComponentManager;
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position + new Vector3(0.5f, -0.2f, 0.5f);
            LocalTransform = matrix;

            new Mesh(componentManager, "Model", this, Matrix.CreateScale(bushSize, bushSize, bushSize), asset, false);

            Health health = new Health(componentManager, "HP", this, 30 * bushSize, 0.0f, 30 * bushSize);
            new Flammable(componentManager, "Flames", this, health);

            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            Tags.Add("Vegetation");
            Tags.Add("Cactus");

            AddToCollisionManager = true;
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }

    [JsonObject(IsReference = true)]
    public class Bush : Body
    {
        public Bush() { }

        public Bush(Vector3 position, string asset, float bushSize) :
            base("Bush", PlayState.ComponentManager.RootComponent, Matrix.Identity, new Vector3(bushSize, bushSize, bushSize),Vector3.Zero)
        {
            ComponentManager componentManager = PlayState.ComponentManager;
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position + new Vector3(0.5f, -0.2f, 0.5f);
            LocalTransform = matrix;

            new Mesh(componentManager, "Model", this, Matrix.CreateScale(bushSize, bushSize, bushSize), asset, false);

            Health health = new Health(componentManager, "HP", this, 30 * bushSize, 0.0f, 30 * bushSize);
            new Flammable(componentManager, "Flames", this, health);

            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            Tags.Add("Vegetation");
            Tags.Add("Bush");
            Tags.Add("EmitsFood");
            Inventory inventory = new Inventory("Inventory", this)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 4
                }
            };

            inventory.Resources.AddResource(new ResourceAmount()
            {
                NumResources = MathFunctions.RandInt(1, 4),
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Berry]
            });

            AddToCollisionManager = true;
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
