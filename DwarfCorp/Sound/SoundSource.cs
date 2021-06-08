using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    public struct SoundSource
    {
        public List<string> Sounds;
        public float Volume;
        public bool RandomPitch;


        public static implicit operator SoundSource(string sound)
        {
            return SoundSource.Create(sound);
        }

        public static implicit operator SoundSource(string[] sounds)
        {
            return SoundSource.Create(sounds);
        }

        public static SoundSource Create(string sound)
        {
            return new SoundSource()
            {
                RandomPitch = true,
                Sounds = new List<string>() {sound},
                Volume = 1.0f
            };
        }

        public static SoundSource Create(params string[] sounds)
        {
            return new SoundSource()
            {
                RandomPitch = true,
                Sounds = sounds.ToList(),
                Volume = 1.0f
            };
        }

        public void Play(Vector3 position, float pitch = 0.0f)
        {
            SoundManager.PlaySound(Datastructures.SelectRandom(Sounds), position, RandomPitch, Volume, pitch);
        }
    }

}