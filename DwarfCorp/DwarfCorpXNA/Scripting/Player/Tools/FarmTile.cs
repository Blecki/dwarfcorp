// BuildTool.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class FarmTile
    {
        public VoxelHandle Voxel = VoxelHandle.InvalidHandle;
        public float Progress = 0.0f;
        public float TargetProgress = 100.0f;
        public string SeedResourceType = null;
        public List<ResourceAmount> RequiredResources = null;
        public bool Finished = false;

        public bool IsTilled()
        {
            return (Voxel.IsValid) && Voxel.Type.Name == "TilledSoil";
        }

        public void CreatePlant(WorldManager world)
        {
            var plant = EntityFactory.CreateEntity<Plant>(ResourceLibrary.Resources[SeedResourceType].PlantToGenerate, Voxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f));
            plant.Farm = this;
            
            Matrix original = plant.LocalTransform;
            original.Translation += Vector3.Down;
            plant.AnimationQueue.Add(new EaseMotion(0.5f, original, plant.LocalTransform.Translation));

            world.ParticleManager.Trigger("puff", original.Translation, Color.White, 20);

            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_plant_grow, Voxel.WorldPosition, true);

            Finished = true;
        }
    }
}
