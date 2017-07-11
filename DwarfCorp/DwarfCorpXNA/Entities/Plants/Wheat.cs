// Wheat.cs
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
    public class Wheat : Plant
    {
        public Wheat()
        {
            
        }

        public Wheat(ComponentManager componentManager, Vector3 position) :
            base(componentManager, "Wheat", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, "wheat", 1.0f)
        {
            Seedlingsheet = new SpriteSheet(ContentPaths.Entities.Plants.gnarled, 32, 32);
            SeedlingFrame = new Point(0, 0);
            Matrix matrix = Matrix.CreateRotationY(MathFunctions.Rand(-0.1f, 0.1f));
            matrix.Translation = position + new Vector3(0.5f, -0.25f, 0.5f);
            LocalTransform = matrix;

            VoxelHandle voxelUnder = new VoxelHandle();
            bool success = componentManager.World.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder);

            if (success)
                AddChild(new VoxelListener(componentManager, componentManager.World.ChunkManager, voxelUnder));
            
            Inventory inventory = AddChild(new Inventory(componentManager, "Inventory", BoundingBox.Extents(), BoundingBoxPos)
            {
                Resources = new ResourceContainer()
                {
                    MaxResources = 4,
                    Resources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>()
                    {
                        {
                            ResourceLibrary.ResourceType.Grain,
                            new ResourceAmount(ResourceLibrary.ResourceType.Grain, MathFunctions.RandInt(1, 5))
                        }
                    }
                }
            }) as Inventory;

            AddChild(new Health(componentManager, "HP", 30, 0.0f, 30));
            AddChild(new Flammable(componentManager, "Flames"));

            Tags.Add("Wheat");
            Tags.Add("Vegetation");
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
