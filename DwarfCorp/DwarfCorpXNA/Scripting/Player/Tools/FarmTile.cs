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
        public Plant Plant = null;
        public float Progress = 0.0f;
        public CreatureAI Farmer = null;
        public bool IsCanceled { get { return false; } set { } } 
        public string PlantedType = null;
        public DesignationType ActiveDesignations = DesignationType._None;

        public bool IsTilled()
        {
            return (Voxel.IsValid) && Voxel.Type.Name == "TilledSoil";
        }

        public bool IsFree()
        {
            return (Plant == null || Plant.IsDead) && Farmer == null;
        }

        public bool PlantExists()
        {
            return !(Plant == null || Plant.IsDead);
        }

        public void CreatePlant(string SeedResourceType, WorldManager world)
        {
            PlantedType = SeedResourceType;
            Plant = EntityFactory.CreateEntity<Plant>(ResourceLibrary.Resources[SeedResourceType].PlantToGenerate, Voxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f));
            Plant.Farm = this;
            
            Matrix original = Plant.LocalTransform;
            original.Translation += Vector3.Down;
            Plant.AnimationQueue.Add(new EaseMotion(0.5f, original, Plant.LocalTransform.Translation));

            world.ParticleManager.Trigger("puff", original.Translation, Color.White, 20);

            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_plant_grow, Voxel.WorldPosition, true);

        }

        public void TriggerAutoHarvest()
        {
            if (Plant != null && !Plant.IsDead)
            {
                if (Plant.World.PlayerFaction.Designations.AddEntityDesignation(Plant, DesignationType.Chop) == DesignationSet.AddDesignationResult.Added)
                    Plant.World.Master.TaskManager.AddTask(new KillEntityTask(Plant, KillEntityTask.KillType.Chop) { Priority = Task.PriorityType.Low });
            }
        }

        public void TriggerAutoReplant(WorldManager world)
        {
            if (Plant == null && Voxel.IsValid && Voxel.Type.Name == "TilledSoil" && !String.IsNullOrEmpty(PlantedType))
            {
                if (world.PlayerFaction.Designations.AddVoxelDesignation(Voxel, DesignationType.Plant, this) == DesignationSet.AddDesignationResult.Added)
                    world.Master.TaskManager.AddTask(new PlantTask(this) { Plant = PlantedType });
            }
        }

    }
}
