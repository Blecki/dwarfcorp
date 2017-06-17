// VoxelType.cs
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
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// Specifies a specific kind of voxel.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class VoxelType
    {
        public short ID { get; set; }
        public string Name { get; set; }
        public bool ReleasesResource { get; set; }
        public ResourceLibrary.ResourceType ResourceToRelease { get; set; }
        public float StartingHealth { get; set; }
        public float ProbabilityOfRelease { get; set; }
        public bool CanRamp { get; set; }
        public float RampSize { get; set; }
        public bool IsBuildable { get; set; }
        public string ParticleType { get; set; }
        public SoundSource ExplosionSound { get; set; }
        public bool HasTransitionTextures { get; set; }
        public bool EmitsLight { get; set; }
        public float MinSpawnHeight { get; set; }
        public float MaxSpawnHeight { get; set; }
        public float SpawnProbability { get; set; }
        public float Rarity { get; set; }
        public bool SpawnVeins { get; set; }
        public bool SpawnClusters { get; set; }
        public float ClusterSize { get; set; }
        public float VeinLength { get; set; }
        public Dictionary<TransitionTexture, BoxPrimitive.BoxTextureCoords> TransitionTextures { get; set; }
        public bool IsSoil { get; set; }
        public bool IsSurface { get; set; }
        public bool IsInvincible { get; set; }
        public Color Tint { get; set; }
        public bool SpawnOnSurface { get; set; }
        private static short maxID = 0;
        public SoundSource HitSound { get; set; }
        public static List<VoxelType> TypeList = new List<VoxelType>();
        public bool IsTransparent { get; set; }
        public VoxelType(VoxelType parent, string subtype)
        {
            ID = maxID;
            maxID++;
            Name = subtype;
            ReleasesResource = parent.ReleasesResource;
            ResourceToRelease = parent.ResourceToRelease;
            StartingHealth = parent.StartingHealth;
            ProbabilityOfRelease = parent.ProbabilityOfRelease;
            CanRamp = parent.CanRamp;
            RampSize = parent.RampSize;
            IsBuildable = parent.IsBuildable;
            ParticleType = parent.ParticleType;
            IsInvincible = parent.IsInvincible;
            ExplosionSound = parent.ExplosionSound;
            HasTransitionTextures = parent.HasTransitionTextures;
            TransitionTextures = parent.TransitionTextures;
            IsSoil = parent.IsSoil;
            EmitsLight = parent.EmitsLight;
            Tint = parent.Tint;
            IsSurface = parent.IsSurface;
            IsTransparent = parent.IsTransparent;
            if (!TypeList.Contains(this))
            {
                TypeList.Add(this);
            }

            MinSpawnHeight = -999;
            MaxSpawnHeight = 999;
            SpawnProbability = 1.0f;
            ClusterSize = 0.0f;
            VeinLength = 0.0f;
            SpawnClusters = false;
            SpawnVeins = false;
            Rarity = 1.0f;
            SpawnOnSurface = false;
            HitSound = parent.HitSound;
        }

        public VoxelType()
        {
            ID = maxID;
            maxID++;
            Name = "";
            IsSurface = false;
            ReleasesResource = false;
            ResourceToRelease = ResourceLibrary.ResourceType.Dirt;
            StartingHealth = 0.0f;
            ProbabilityOfRelease = 0.0f;
            CanRamp = false;
            RampSize = 0.0f;
            IsBuildable = false;
            ParticleType = "puff";
            IsInvincible = false;
            ExplosionSound = SoundSource.Create(ContentPaths.Audio.gravel);
            HasTransitionTextures = false;
            TransitionTextures = new Dictionary<TransitionTexture, BoxPrimitive.BoxTextureCoords>();
            IsSoil = false;
            EmitsLight = false;
            Tint = Color.White;
            if(!TypeList.Contains(this))
            {
                TypeList.Add(this);
            }
            MinSpawnHeight = -999;
            MaxSpawnHeight = 999;
            SpawnProbability = 1.0f;
            ClusterSize = 0.0f;
            VeinLength = 0.0f;
            SpawnClusters = false;
            SpawnVeins = false;
            Rarity = 1.0f;
            SpawnOnSurface = false;
            IsTransparent = false;
            HitSound = SoundSource.Create(ContentPaths.Audio.pick);
        }
    }

}