using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
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
        public float BasePitch = 0.0f;
        public Dictionary<string, List<string>> Noises { get; set; }

        public NoiseMaker()
        {
            // Todo: Why does every noisemaker need these noises?
            Noises = new Dictionary<string, List<string>>();
            Noises["Chew"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_eat_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_eat_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_eat_3,
            };
            Noises["Sleep"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_sleep_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_sleep_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_sleep_3,
            };
            Noises["Hurt"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_generic_hurt_1,
                ContentPaths.Audio.Oscar.sfx_ic_generic_hurt_2,
                ContentPaths.Audio.Oscar.sfx_ic_generic_hurt_3,
            };
            Noises["Swim"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_env_water_splash
            };
            Noises["Stash"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_stash_item
            };

            Noises["StashMoney"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_stash_money
            };

            Noises["Stockpile"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_stockpile
            };

            Noises["Craft"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_craft
            };

            Noises["Cook"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_cook_meal
            };

        }

        public void MakeNoise(string noise, Vector3 position, bool randomPitch = false, float volume = 1.0f)
        {
            if (!Noises.ContainsKey(noise))
            {
                return;
            }

            if (CurrentSound == null || CurrentSound.EffectInstance == null || CurrentSound.EffectInstance.IsDisposed || 
                CurrentSound.EffectInstance.State == SoundState.Stopped || CurrentSound.EffectInstance.State == SoundState.Paused)
            {
                List<string> availableNoises = Noises[noise];
                int index = MathFunctions.Random.Next(availableNoises.Count);
                CurrentSound = SoundManager.PlaySound(availableNoises[index], position, randomPitch, volume, BasePitch);
               
            }
            

        }
    }
}
