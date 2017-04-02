// SoundManager.cs
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


    public class MusicTrack
    {
        public string Intro;
        public string Loop;
        public string Outro;
        public bool PlayLoopOverIntro = false;
        private Cue IntroCue;
        private Cue LoopCue;
        private Cue OutroCue;
        private SoundBank sounds;

        public MusicTrack(SoundBank soundbank)
        {
            sounds = soundbank;
        }

        public void Start()
        {
            if (!string.IsNullOrEmpty(Intro))
            {
                IntroCue = sounds.GetCue(Intro);
                IntroCue.Play();

                if (PlayLoopOverIntro && !string.IsNullOrEmpty(Loop))
                {
                    LoopCue = sounds.GetCue(Loop);
                    LoopCue.Play();
                }
            }
        }

        public void Update()
        {
            if (IntroCue != null && IntroCue.IsStopped && !string.IsNullOrEmpty(Loop))
            {
                if (!PlayLoopOverIntro)
                {
                    LoopCue = sounds.GetCue(Loop);
                    LoopCue.Play();
                }
                IntroCue = null;
            }
        }

        public void Stop()
        {
            if (IntroCue != null && IntroCue.IsPlaying)
            {
                IntroCue.Stop(AudioStopOptions.AsAuthored);
            }

            if (OutroCue == null && !string.IsNullOrEmpty(Outro))
            {
                OutroCue = sounds.GetCue(Outro);
                OutroCue.Play();
            }

            if (LoopCue != null)
            {
                LoopCue.Stop(AudioStopOptions.AsAuthored);
            }
        }

    }

    public class FancyMusic
    {
        public Dictionary<string, MusicTrack> Tracks = new Dictionary<string, MusicTrack>();
        private string currentTrack = null;

        public FancyMusic()
        {
            
        }

        public void AddTrack(string name, MusicTrack track)
        {
            Tracks[name] = track;
        }

        public void PlayTrack(string name)
        {
            if (currentTrack == name)
            {
                return;
            }

            if (currentTrack != null)
            {
                Tracks[currentTrack].Stop();
                currentTrack = null;
            }

            currentTrack = name;
            Tracks[currentTrack].Start();
        }

        public void Update()
        {
            if (currentTrack != null)
            {
                Tracks[currentTrack].Update();
            }
        }

    }

    /// <summary>
    /// Manages and creates 3D sounds.
    /// </summary>
    public class SoundManager
    {
        public static AudioEngine AudioEngine { get; set; }
        public static SoundBank SoundBank { get; set; }
        public static WaveBank WaveBank { get; set; }
        public static List<Song> ActiveSongs = new List<Song>();
        public static List<Sound3D> ActiveSounds = new List<Sound3D>();
        public static AudioListener Listener = new AudioListener();
        public static AudioEmitter Emitter = new AudioEmitter();
        public static ContentManager Content { get; set; }
        public static int MaxSounds = 5;
        public static Dictionary<string, int> SoundCounts = new Dictionary<string, int>();
        public static Dictionary<string, SoundEffect> EffectLibrary = new Dictionary<string, SoundEffect>();
        public static Dictionary<string, Cue> ActiveCues = new Dictionary<string, Cue>();
        public static FancyMusic CurrentMusic = null;

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
            //SoundEffect.DistanceScale = 0.1f;
            //SoundEffect.DopplerScale = 0.1f;
            AudioEngine = new AudioEngine("Content\\Audio\\XACT\\Win\\Sounds.xgs");
            SoundBank = new SoundBank(AudioEngine, "Content\\Audio\\XACT\\Win\\SoundBank.xsb");
            WaveBank = new WaveBank(AudioEngine, "Content\\Audio\\XACT\\Win\\WaveBank.xwb");

            CurrentMusic = new FancyMusic();
            CurrentMusic.AddTrack("main_theme_day", new MusicTrack(SoundBank)
            {
                Intro = "music_1_intro",
                Loop = "music_1_loop"
            });
            CurrentMusic.AddTrack("main_theme_night", new MusicTrack(SoundBank)
            {
                Intro = "music_1_night_intro",
                Loop = "music_1_night",
                PlayLoopOverIntro = true
            });
        }

        public static void PlayAmbience(string sound)
        {
            Cue cue;
            if (!ActiveCues.TryGetValue(sound, out cue))
            {
                cue = SoundBank.GetCue(sound);
            }
            if (!cue.IsPlaying)
            {
                cue.Play();
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
            /*
            if(GameSettings.Default.MasterVolume < 0.001f || GameSettings.Default.MusicVolume < 0.001f)
            {
                return;
            }
            Song song = Content.Load<Song>(name);
            MediaPlayer.Play(song);
            MediaPlayer.Volume = GameSettings.Default.MasterVolume * GameSettings.Default.MusicVolume;
             * */
        }

        public static Sound3D PlaySound(string name, Vector3 location, bool randomPitch, float volume = 1.0f, float pitch = 0.0f)
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
                    sound.EffectInstance.Pitch = MathFunctions.Clamp((float)(MathFunctions.Random.NextDouble() * 1.0f - 0.5f) + pitch, -1.0f, 1.0f);
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

        public static void PlaySound(string name, float volume)
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
        public static void Update(DwarfTime time, Camera camera, WorldTime worldTime)
        {
            AudioEngine.Update();
            AudioEngine.SetGlobalVariable("TimeofDay", worldTime.GetTimeOfDay());
            PlayAmbience("grassland_ambience_day");
            PlayAmbience("grassland_ambience_night");
            AudioEngine.GetCategory("Ambience").SetVolume(GameSettings.Default.SoundEffectVolume * 0.1f);
            AudioEngine.GetCategory("Music").SetVolume(GameSettings.Default.MusicVolume);
            CurrentMusic.Update();
            List<Sound3D> toRemove = new List<Sound3D>();

            Listener.Position = camera.Position;
            Listener.Up = Vector3.Up;
            Listener.Velocity = camera.Velocity;
            Listener.Forward = (camera.Target - camera.Position);
            Listener.Forward.Normalize();
          

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
                    instance.EffectInstance.Volume *= (GameSettings.Default.MasterVolume * GameSettings.Default.SoundEffectVolume * instance.VolumeMultiplier);
                    instance.EffectInstance.Apply3D(Listener, Emitter);
                    instance.EffectInstance.Play();
                    Emitter.Position = instance.Position;
                    //instance.EffectInstance.Apply3D(Listener, Emitter);

                    //instance.EffectInstance.Volume = Math.Max(Math.Min(400.0f / (camera.Position - instance.Position).LengthSquared(), 0.999f), 0.001f);
                    instance.HasStarted = true;
                }
            }


            /*
            MediaPlayer.Volume = GameSettings.Default.MasterVolume*GameSettings.Default.MusicVolume * 0.1f;
            if (MediaPlayer.State == MediaState.Stopped)
            {
                if (once  && ActiveSongs.Count > 0)
                {
                    MediaPlayer.Play(ActiveSongs[MathFunctions.Random.Next(ActiveSongs.Count)]);
                    once = false;
                }
            }
            else
            {
                once = true;
            }
             */

            foreach(Sound3D r in toRemove)
            {
                ActiveSounds.Remove(r);
            }
        }
    }

}