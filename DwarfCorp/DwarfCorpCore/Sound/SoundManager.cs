﻿// SoundManager.cs
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
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorpCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    /// <summary>
    /// Manages and creates 3D sounds.
    /// </summary>
    public class SoundManager
    {
        public static List<Song> ActiveSongs = new List<Song>();
        public static List<Sound3D> ActiveSounds = new List<Sound3D>();
        public static AudioListener Listener = new AudioListener();
        public static AudioEmitter Emitter = new AudioEmitter();
        public static ContentManager Content { get; set; }
        public static int MaxSounds = 5;
        public static Dictionary<string, int> SoundCounts = new Dictionary<string, int>();
        public static Dictionary<string, SoundEffect> EffectLibrary = new Dictionary<string, SoundEffect>();

        public static void LoadDefaultSounds()
        {
            string[] defaultSounds =
            {
                ContentPaths.Audio.pick,
                ContentPaths.Audio.hit,
                ContentPaths.Audio.jump,
                ContentPaths.Audio.ouch,
                ContentPaths.Audio.gravel,
                ContentPaths.Audio.river
            };

            foreach (string name in defaultSounds)
            {
                SoundEffect effect = Content.Load<SoundEffect>(name);
                EffectLibrary[name] = effect;
            }
        }

        public static void SetActiveSongs(params string[] songs)
        {
            ActiveSongs = new List<Song>();

            foreach (string song in songs)
            {
                ActiveSongs.Add(Content.Load<Song>(song));
            }
        }

        public static void PlayMusic(string name)
        {
            if(GameSettings.Default.MasterVolume < 0.001f || GameSettings.Default.MusicVolume < 0.001f)
            {
                return;
            }
            Song song = Content.Load<Song>(name);
            MediaPlayer.Play(song);
            MediaPlayer.Volume = GameSettings.Default.MasterVolume * GameSettings.Default.MusicVolume;
        }

        public static Sound3D PlaySound(string name, Vector3 location, bool randomPitch, float volume = 1.0f)
        {
            if(Content == null)
            {
                return null;
            }
            SoundEffect effect = null;

            if (!EffectLibrary.ContainsKey(name))
            {
                effect = Content.Load<SoundEffect>(name);
                EffectLibrary[name] = effect;
            }
            else
            {
                effect = EffectLibrary[name];
            }




            if (!SoundCounts.ContainsKey(name))
            {
                SoundCounts[name] = 0;
            }

            if (SoundCounts[name] < MaxSounds)
            {
                SoundCounts[name]++;

                Sound3D sound = new Sound3D
                {
                    Position = location,
                    EffectInstance = effect.CreateInstance(),
                    HasStarted = false,
                    Name = name
                };
                sound.EffectInstance.IsLooped = false;
                sound.VolumeMultiplier = volume;


                if (randomPitch)
                {
                    sound.EffectInstance.Pitch = (float)(WorldManager.Random.NextDouble() * 1.0f - 0.5f);
                }
                ActiveSounds.Add(sound);

                return sound;
            }


            return null;

        }

        public static void PlaySound(string name)
        {
            PlaySound(name, 1.0f);
        }

        public static void PlaySound(string name, float volume = 1.0f)
        {
            // TODO: Remove this block once the SoundManager is initialized in a better location.
            if (Content == null) return;

            SoundEffect effect = null;

            if (!EffectLibrary.ContainsKey(name))
            {
                effect = Content.Load<SoundEffect>(name);
                EffectLibrary[name] = effect;
            }
            else
            {
                effect = EffectLibrary[name];
            }

            effect.Play(GameSettings.Default.MasterVolume * GameSettings.Default.SoundEffectVolume * volume, 0.0f, 0.0f);


        }

        public static Sound3D PlaySound(string name, Vector3 location, float volume = 1.0f)
        {
            return PlaySound(name, location, false, volume);
        }


        private static bool once = true;
        public static void Update(DwarfTime time, Camera camera)
        {
            List<Sound3D> toRemove = new List<Sound3D>();

            Matrix viewInverse = Matrix.Invert(camera.ViewMatrix);
            Listener.Position = camera.Position;
            Listener.Up = viewInverse.Up;
            Listener.Velocity = camera.Velocity;
            Listener.Forward = viewInverse.Forward;
           

            foreach(Sound3D instance in ActiveSounds)
            {
                if(instance.HasStarted && instance.EffectInstance.State == SoundState.Stopped || instance.EffectInstance.State == SoundState.Paused)
                {
                    if(!instance.EffectInstance.IsDisposed)
                        instance.EffectInstance.Dispose();
                    toRemove.Add(instance);
                    SoundCounts[instance.Name]--;
                }
                else if(!instance.HasStarted)
                {
                    if (float.IsNaN(instance.Position.X) || 
                        float.IsNaN(instance.Position.Y) ||
                        float.IsNaN(instance.Position.Z))
                    {
                        instance.Position = Vector3.Zero;
                    }
                    Emitter.Position = instance.Position;
                    instance.EffectInstance.Apply3D(Listener, Emitter);

                    instance.EffectInstance.Volume = Math.Max(Math.Min(400.0f / (camera.Position - instance.Position).LengthSquared(), 0.999f), 0.001f);
                    instance.EffectInstance.Volume *= (GameSettings.Default.MasterVolume * GameSettings.Default.SoundEffectVolume * instance.VolumeMultiplier);

                    instance.EffectInstance.Play();
                    instance.HasStarted = true;
                }
            }

            MediaPlayer.Volume = GameSettings.Default.MasterVolume*GameSettings.Default.MusicVolume * 0.1f;
            if (MediaPlayer.State == MediaState.Stopped)
            {
                if (once  && ActiveSongs.Count > 0)
                {
                    MediaPlayer.Play(ActiveSongs[WorldManager.Random.Next(ActiveSongs.Count)]);
                    once = false;
                }
            }
            else
            {
                once = true;
            }

            foreach(Sound3D r in toRemove)
            {
                ActiveSounds.Remove(r);
            }
        }
    }

}