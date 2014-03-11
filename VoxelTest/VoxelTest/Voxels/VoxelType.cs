using System.Collections.Generic;
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
        public string ResourceToRelease { get; set; }
        public float StartingHealth { get; set; }
        public float ProbabilityOfRelease { get; set; }
        public bool CanRamp { get; set; }
        public float RampSize { get; set; }
        public bool IsBuildable { get; set; }
        public string ParticleType { get; set; }
        public string ExplosionSound { get; set; }
        public bool HasTransitionTextures { get; set; }
        
        public Dictionary<TransitionTexture, BoxPrimitive.BoxTextureCoords> TransitionTextures { get; set; } 
        
        private static short maxID = 0;

        public static List<VoxelType> TypeList = new List<VoxelType>();

        public VoxelType()
        {
            ID = maxID;
            maxID++;
            Name = "";
            ReleasesResource = false;
            ResourceToRelease = "";
            StartingHealth = 0.0f;
            ProbabilityOfRelease = 0.0f;
            CanRamp = false;
            RampSize = 0.0f;
            IsBuildable = false;
            ParticleType = "puff";
            ExplosionSound = ContentPaths.Audio.gravel;
            HasTransitionTextures = false;
            TransitionTextures = new Dictionary<TransitionTexture, BoxPrimitive.BoxTextureCoords>();

            if(!TypeList.Contains(this))
            {
                TypeList.Add(this);
            }
        }
    }

}