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
            }

            if (PlayLoopOverIntro && !string.IsNullOrEmpty(Loop))
            {
                LoopCue = sounds.GetCue(Loop);
                LoopCue.Play();
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

        public string CurrentTrack { get { return currentTrack; } }

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
                levels.RandomPitch = 0.1f;
                Gains.Add(asset, levels);
            }

            return levels;
        }

        public void SetLevels(string asset, Levels levels)
        {
            Gains[asset] = levels;
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
        public static List<SoundEffectInstance> ActiveSounds2D = new List<SoundEffectInstance>();
        public static bool HasAudioDevice = true;
        public static string AudioError = "";
        public static SFXMixer Mixer = null;

        public static void LoadDefaultSounds()
        {
            try
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
                try
                {
                    Mixer = FileUtils.LoadJson<SFXMixer>(ContentPaths.mixer, false);
                }
                catch (FileNotFoundException exception)
                {
                    Console.Out.WriteLine("Mixer file didn't exist. Creating a new mixer.");
                    Mixer = new SFXMixer()
                    {
                        Gains = new Dictionary<string, SFXMixer.Levels>()
                    };
                }
                SoundEffect.DistanceScale = 0.5f;
                //SoundEffect.DopplerScale = 0.1f;
                AudioEngine = new AudioEngine("Content\\Audio\\XACT\\Win\\Sounds.xgs");
                SoundBank = new SoundBank(AudioEngine, "Content\\Audio\\XACT\\Win\\SoundBank.xsb");
                WaveBank = new WaveBank(AudioEngine, "Content\\Audio\\XACT\\Win\\WaveBank.xwb");

                CurrentMusic = new FancyMusic();
                CurrentMusic.AddTrack("main_theme_day", new MusicTrack(SoundBank)
                {
                    Intro = "music_1_intro",
                    Loop = "music_1_loop",
                    PlayLoopOverIntro = false
                });
                CurrentMusic.AddTrack("main_theme_night", new MusicTrack(SoundBank)
                {
                    Intro = "music_1_night_intro",
                    Loop = "music_1_night",
                    PlayLoopOverIntro = true
                });
                CurrentMusic.AddTrack("menu_music", new MusicTrack(SoundBank)
                {
                    Loop = "music_menu",
                    PlayLoopOverIntro = true
                });
                CurrentMusic.AddTrack("molemen", new MusicTrack(SoundBank)
                {
                    Loop = "molemen",
                    PlayLoopOverIntro = true
                });
                CurrentMusic.AddTrack("elf", new MusicTrack(SoundBank)
                {
                    Loop = "elf",
                    PlayLoopOverIntro = true
                });
                CurrentMusic.AddTrack("undead", new MusicTrack(SoundBank)
                {
                    Loop = "undead",
                    PlayLoopOverIntro = true
                });
                CurrentMusic.AddTrack("goblin", new MusicTrack(SoundBank)
                {
                    Loop = "goblin",
                    PlayLoopOverIntro = true
                });

                foreach (var cue in ActiveCues)
                {
                    cue.Value.Stop(AudioStopOptions.Immediate);
                }
                ActiveCues.Clear();

            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                HasAudioDevice = false;
                AudioError = exception.Message;
            }
        }

        public static void StopAmbience()
        {
            if (!HasAudioDevice) return;
            foreach (var cue in ActiveCues)
            {
                cue.Value.Stop(AudioStopOptions.Immediate);
            }
        }

        public static void PlayAmbience(string sound)
        {
            if (!HasAudioDevice) return;
            Cue cue;
            if (!ActiveCues.TryGetValue(sound, out cue))
            {
                cue = SoundBank.GetCue(sound);
                ActiveCues[sound] = cue;
            }
            if (!cue.IsPlaying && !cue.IsStopped)
            {
                cue.Play();
            }
            else if (cue.IsStopped)
            {
                Cue newCue = SoundBank.GetCue(sound);
                newCue.Play();
                ActiveCues[sound] = newCue;
            }
        }

        public static void SetActiveSongs(params string[] songs)
        {
            if (!HasAudioDevice) return;
            ActiveSongs = new List<Song>();

            foreach (string song in songs)
            {
                ActiveSongs.Add(Content.Load<Song>(song));
            }
        }

        public static void PlayMusic(string name)
        {
            if (!HasAudioDevice) return;
            AudioEngine.GetCategory("Music").SetVolume(GameSettings.Default.MusicVolume * GameSettings.Default.MasterVolume);
            CurrentMusic.PlayTrack(name);
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
            if (!HasAudioDevice) return null;
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
                SFXMixer.Levels levels = Mixer.GetOrCreateLevels(name);
                sound.EffectInstance.IsLooped = false;
                sound.VolumeMultiplier = volume * levels.Volume;


                if (randomPitch)
                {
                    sound.EffectInstance.Pitch = MathFunctions.Clamp((float)(MathFunctions.Random.NextDouble() * 1.0f - 0.5f) * levels.RandomPitch + pitch, -1.0f, 1.0f);
                }
                ActiveSounds.Add(sound);

                return sound;
            }


            return null;

        }

        public static SoundEffectInstance PlaySound(string name, float volume = 1.0f, float pitch  = 0.0f)
        {
            if (!HasAudioDevice) return null;
            // TODO: Remove this block once the SoundManager is initialized in a better location.
            if (Content == null) return null;

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
            SFXMixer.Levels levels = Mixer.GetOrCreateLevels(name);
            SoundEffectInstance instance = effect.CreateInstance();
            instance.Volume = GameSettings.Default.MasterVolume*GameSettings.Default.SoundEffectVolume*volume*levels.Volume;
            instance.Pitch = pitch;
            instance.Play();
            ActiveSounds2D.Add(instance);
            return instance;
        }

        public static Sound3D PlaySound(string name, Vector3 location, float volume = 1.0f)
        {
            if (!HasAudioDevice) return null;
            return PlaySound(name, location, false, volume);
        }


        public static void Update(DwarfTime time, Camera camera, WorldTime worldTime)
        {
            if (!HasAudioDevice) return;
            AudioEngine.Update();
            if (worldTime != null)
            {
                AudioEngine.SetGlobalVariable("TimeofDay", worldTime.GetTimeOfDay());
            }
            AudioEngine.GetCategory("Ambience").SetVolume(GameSettings.Default.SoundEffectVolume * 0.1f * GameSettings.Default.MasterVolume);
            AudioEngine.GetCategory("Music").SetVolume(GameSettings.Default.MusicVolume * GameSettings.Default.MasterVolume);
            CurrentMusic.Update();
            List<Sound3D> toRemove = new List<Sound3D>();

            if (camera != null)
            {
                Listener.Position = camera.Position;
                Listener.Up = Vector3.Up;
                Listener.Velocity = camera.Velocity;
                Listener.Forward = (camera.Target - camera.Position);
                Listener.Forward.Normalize();
            }

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

            ActiveSounds2D.RemoveAll(sound => sound.State == SoundState.Stopped);

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