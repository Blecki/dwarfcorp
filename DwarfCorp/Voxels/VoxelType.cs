using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// Specifies a specific kind of voxel.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class VoxelType : global::System.IEquatable<VoxelType>
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
        public string ResourceToRelease = ResourceType.Stone;
        public float StartingHealth = 0.0f;
        public float ProbabilityOfRelease = 0.0f;
        public bool CanRamp = false;
        public float RampSize = 0.0f;
        public bool IsBuildable = false;
        public string ParticleType = "puff";
        public bool IsFlammable = false;
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
        public bool GrassSpreadsHere = false;
        public Color Tint = Color.White;
        //public bool UseBiomeGrassTint = false;
                
        public bool SpawnOnSurface = false;
        public bool IsTransparent = false;

        public string ExplosionSoundResource = ContentPaths.Audio.gravel;
        public string[] HitSoundResources = new string[] { ContentPaths.Audio.pick };
        public List<Resource.ResourceTags> BuildRequirements = new List<Resource.ResourceTags>();

        [JsonIgnore] public SoundSource ExplosionSound;
        [JsonIgnore] public SoundSource HitSound;

        public Color MinimapColor = Color.White;

        public VoxelType()
        {
            
        }

        public bool CanBuildWith(Resource resource)
        {
            return IsBuildable && ((BuildRequirements.Count == 0 && resource.Name == ResourceToRelease) || 
                (BuildRequirements.Count > 0 && BuildRequirements.TrueForAll(requirement => resource.Tags.Contains(requirement))));
        }

        public string GetBuildRequirementsString()
        {
            if (BuildRequirements.Count == 0)
            {
                return ResourceToRelease;
            }
            return BuildRequirements[0].ToString();
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