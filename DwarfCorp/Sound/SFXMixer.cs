using System.Collections.Generic;

namespace DwarfCorp
{
    /// <summary>
    /// User controlled gains on volumes for SFX.
    /// </summary>
    public class SFXMixer
    {
        public struct Levels
        {
            public float Volume;
            public float RandomPitch;
        }
        public Dictionary<string, Levels> Gains { get; set; }
        public float SFXScale = 0.5f;
        public float DopplerScale = 0.5f;
        public Levels GetOrCreateLevels(string asset)
        {
            Levels levels;
            if (!Gains.TryGetValue(asset, out levels))
            {
                levels.Volume = 1.0f;
                levels.RandomPitch = 0.5f;
                Gains.Add(asset, levels);
            }

            return levels;
        }

        public void SetLevels(string asset, Levels levels)
        {
            Gains[asset] = levels;
        }
    }
}