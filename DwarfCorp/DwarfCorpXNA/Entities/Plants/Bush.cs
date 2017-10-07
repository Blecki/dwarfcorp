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
    //Todo: Split file
    [JsonObject(IsReference = true)]
    public class Bush : Plant
    {
        public Bush() { }

        public Bush(ComponentManager componentManager, Vector3 position, string asset, float bushSize) :
            base(componentManager, "Berry Bush", Matrix.Identity, new Vector3(bushSize, bushSize, bushSize), asset, bushSize)
        {
            BoundingBoxPos = Vector3.Zero;
            Seedlingsheet = new SpriteSheet(ContentPaths.Entities.Plants.berrybushsprout, 32, 32);
            SeedlingFrame = new Point(0, 0);
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position + new Vector3(0.5f, -0.15f, 0.5f);
            LocalTransform = matrix;
            AddChild(new Health(componentManager, "HP", 30 * bushSize, 0.0f, 30 * bushSize));
            AddChild(new Flammable(componentManager, "Flames"));

            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                Manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(position)));
            if (voxelUnder.IsValid)
                AddChild(new VoxelListener(Manager, Manager.World.ChunkManager,
                    voxelUnder));

            var particles = AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
    Matrix.Identity, BoundingBoxPos, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_bush_harvest_1
            }) as ParticleTrigger;

            Tags.Add("Vegetation");
            Tags.Add("Bush");
            Tags.Add("EmitsFood");
            Inventory inventory = AddChild(new Inventory(componentManager, "Inventory", BoundingBox.Extents(), BoundingBoxPos)) as Inventory;

            inventory.AddResource(new ResourceAmount()
            {
                NumResources = 3,
                ResourceType = ResourceLibrary.ResourceType.Berry
            });

            AddToCollisionManager = true;
            CollisionType = CollisionManager.CollisionType.Static;
            PropogateTransforms();
        }
    }
}
