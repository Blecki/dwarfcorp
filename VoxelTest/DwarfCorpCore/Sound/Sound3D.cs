using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Newtonsoft.Json;

namespace DwarfCorp
{
    
    /// <summary>
    /// Holds a particular instant of a 3D sound, its location, and its volume.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Sound3D
    {
        [JsonIgnore]
        public SoundEffectInstance EffectInstance;
        public Vector3 Position;
        public bool HasStarted;
        public string Name;
        public float VolumeMultiplier { get; set; }
    }

}