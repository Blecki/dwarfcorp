using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace DwarfCorp
{
    /// <summary>
    /// Just holds a collection of noises, and can only make one such noise
    /// at a time (used, for instance, to make creatures have noises).
    /// </summary>
    public class NoiseMaker
    {
        public Sound3D CurrentSound { get; set; }

        public Dictionary<string, List<string>> Noises { get; set; }

        public NoiseMaker()
        {
            Noises = new Dictionary<string, List<string>>();
        }

        public void MakeNoise(string noise, Vector3 position, bool randomPitch = false, float volume = 1.0f)
        {
            try
            {
                if (!Noises.ContainsKey(noise))
                {
                    return;
                }

                if (CurrentSound == null || CurrentSound.EffectInstance.State == SoundState.Stopped || CurrentSound.EffectInstance.State == SoundState.Paused)
                {
                    List<string> availableNoises = Noises[noise];
                    CurrentSound = SoundManager.PlaySound(availableNoises[PlayState.Random.Next(availableNoises.Count)], position, randomPitch, volume);
                }
            }
            catch(ObjectDisposedException exception)
            {
                CurrentSound = null;
                Console.Error.WriteLine(exception.Message);
            }

        }
    }
}
