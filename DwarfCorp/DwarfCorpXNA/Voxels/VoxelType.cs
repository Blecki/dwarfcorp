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
using System;

namespace DwarfCorp
{

    /// <summary>
    /// Specifies a specific kind of voxel.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class VoxelType : System.IEquatable<VoxelType>
    {
        public enum TransitionType
        {
            None,
            Horizontal,
            Vertical
        }

        public short ID;
        public string Name = "";

        // Graphics properties
        public Point Top;
        public Point Bottom;
        public Point Sides;
        public TransitionType Transitions = TransitionType.Horizontal;
        public bool HasTransitionTextures = false;
        public Point[] TransitionTiles = null;
        [JsonIgnore] public Dictionary<BoxTransition, BoxPrimitive.BoxTextureCoords> TransitionTextures = null;
        
        public bool ReleasesResource = false;
        public ResourceLibrary.ResourceType ResourceToRelease = ResourceLibrary.ResourceType.Stone;
        public float StartingHealth = 0.0f;
        public float ProbabilityOfRelease = 0.0f;
        public bool CanRamp = false;
        public float RampSize = 0.0f;
        public bool IsBuildable = false;
        public string ParticleType = "puff";
        
        public bool EmitsLight = false;
        public float MinSpawnHeight = -999;
        public float MaxSpawnHeight = 999;
        public float SpawnProbability = 1.0f;
        public float Rarity = 1.0f;
        public bool SpawnVeins = false;
        public bool SpawnClusters = false;
        public float ClusterSize = 0.0f;
        public float VeinLength = 0.0f;
        public bool IsSoil = false;
        public bool IsSurface = false;
        public bool IsInvincible = false;
        public Color Tint = Color.White;
        //public bool UseBiomeGrassTint = false;
                
        public bool SpawnOnSurface = false;
        public bool IsTransparent = false;

        public string ExplosionSoundResource = ContentPaths.Audio.gravel;
        public string[] HitSoundResources = new string[] { ContentPaths.Audio.pick };

        [JsonIgnore] public SoundSource ExplosionSound;
        [JsonIgnore] public SoundSource HitSound;

        public VoxelType()
        {
            
        }

        public static bool operator ==(VoxelType obj1, VoxelType obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            if (ReferenceEquals(obj1, null))
            {
                return false;
            }
            if (ReferenceEquals(obj2, null))
            {
                return false;
            }

            return obj1.ID == obj2.ID;
        }

        // this is second one '!='
        public static bool operator !=(VoxelType obj1, VoxelType obj2)
        {
            return !(obj1 == obj2);
        }

        public bool Equals(VoxelType other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((VoxelType)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ID;
            }
        }
    }

}