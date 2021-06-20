using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;

namespace DwarfCorp
{
    /// <summary>
    /// Manages and creates 3D sounds.
    /// </summary>
    public class SoundManager
    {
        public static List<Sound3D> ActiveSounds = new List<Sound3D>();
        public static AudioListener Listener = new AudioListener();
        public static AudioEmitter Emitter = new AudioEmitter();
        public static ContentManager Content { get; set; }
        public static int MaxSounds = 5;
        public static Dictionary<string, int> SoundCounts = new Dictionary<string, int>();
        public static Dictionary<string, SoundEffect> EffectLibrary = new Dictionary<string, SoundEffect>();
        public static bool HasAudioDevice = true;
        public static string AudioError = "";
        public static SFXMixer Mixer = null;

        private static SoundEffectInstance CurrentAmbience = null;
        
               public static void LoadMixerSettings()
        {
            try
            {
                try
                {
                    Mixer = FileUtils.LoadJsonFromResolvedPath<SFXMixer>(ContentPaths.mixer);
                }
                catch (FileNotFoundException)
                {
                    Console.Out.WriteLine("Mixer file didn't exist. Creating a new mixer.");
                    Mixer = new SFXMixer()
                    {
                        Gains = new Dictionary<string, SFXMixer.Levels>()
                    };
                }

                SoundEffect.DistanceScale = 1.0f;// 0.25f;

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
            if (CurrentAmbience != null)
                CurrentAmbience.Stop();
            CurrentAmbience = null;
        }

        public static void PlayAmbience(string sound)
        {
            if (!HasAudioDevice) return;
            var soundEffect = GetSound(sound);
            if (CurrentAmbience != null)
                StopAmbience();
            CurrentAmbience = soundEffect.CreateInstance();
            CurrentAmbience.IsLooped = true;
            CurrentAmbience.Play();
        }

        public static void PlayMusic(String IntroName, String LoopName)
        {
            if (!HasAudioDevice) return;
            MusicPlayer.CueSong(
                String.IsNullOrEmpty(IntroName) ? null : Content.Load<Song>(AssetManager.ResolveContentPath(IntroName)), 
                Content.Load<Song>(AssetManager.ResolveContentPath(LoopName)));
        }

        public static Sound3D PlaySound(string name, Vector3 location, bool randomPitch, float volume = 1.0f, float pitch = 0.0f)
        {
            if (!HasAudioDevice) return null;

            if (Content == null) return null;
            var effect = GetSound(name);

            if (CanPlay(name))
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
                    sound.EffectInstance.Pitch = MathFunctions.Clamp((float)(MathFunctions.Random.NextDouble() * 1.0f - 0.5f) * levels.RandomPitch + pitch, -1.0f, 1.0f);
                else
                    sound.EffectInstance.Pitch = MathFunctions.Clamp(pitch, -1.0f, 1.0f);
                ActiveSounds.Add(sound);

                return sound;
            }


            return null;

        }

        public static bool CanPlay(String Name)
        {
            if (!SoundCounts.ContainsKey(Name))
                SoundCounts[Name] = 0;

            return SoundCounts[Name] < MaxSounds;
        }

        public static SoundEffect GetSound(String Name)
        {
            if (Content == null)
                return null;

            if (!EffectLibrary.ContainsKey(Name))
                EffectLibrary.Add(Name, Content.Load<SoundEffect>(AssetManager.ResolveContentPath(Name)));
            return EffectLibrary[Name];
        }

        public static SoundEffectInstance PlaySound(string name, float volume = 1.0f, float pitch  = 0.0f)
        {
            if (!HasAudioDevice) return null;
            // TODO: Remove this block once the SoundManager is initialized in a better location.
            if (Content == null) return null;

            SoundEffect effect = GetSound(name);

            
            var levels = Mixer.GetOrCreateLevels(name);
            var instance = effect.CreateInstance();
            instance.Pitch = MathFunctions.Clamp(pitch, -1.0f, 1.0f);
            instance.Play();
            instance.Pan = MathFunctions.Rand(-0.25f, 0.25f);
            instance.Volume = GameSettings.Current.MasterVolume * GameSettings.Current.SoundEffectVolume * volume * levels.Volume;
            instance.Pitch = MathFunctions.Clamp(pitch, -1.0f, 1.0f);
            
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

            if (camera != null)
            {
                Listener.Position = camera.Position;
                Listener.Up = camera.UpVector;
                Listener.Velocity = camera.Velocity;
                Listener.Forward = (camera.Target - camera.Position);
                Listener.Forward.Normalize();
            }

            try
            {
                foreach (var instance in ActiveSounds)
                {
                    if (instance.EffectInstance.IsDisposed || (instance.HasStarted && instance.EffectInstance.State == SoundState.Stopped || instance.EffectInstance.State == SoundState.Paused))
                    {
                        if (!instance.EffectInstance.IsDisposed)
                            instance.EffectInstance.Dispose();
                        SoundCounts[instance.Name]--;
                    }
                    else if (!instance.HasStarted)
                    {
                        if (float.IsNaN(instance.Position.X) ||
                            float.IsNaN(instance.Position.Y) ||
                            float.IsNaN(instance.Position.Z))
                        {
                            instance.Position = Vector3.Zero;
                        }
                        instance.EffectInstance.Volume *= (GameSettings.Current.MasterVolume * GameSettings.Current.SoundEffectVolume * instance.VolumeMultiplier);
                        Emitter.Position = instance.Position;
                        instance.EffectInstance.Apply3D(Listener, Emitter);
                        instance.EffectInstance.Play();
                        instance.HasStarted = true;
                    }
                }
            }
            catch (Exception e)
            {
                // Collection getting modified somehow? ??? Dunno. Very odd and rare crash.
            }

            ActiveSounds.RemoveAll(sound => sound.EffectInstance.IsDisposed || sound.EffectInstance.State != SoundState.Playing);

            if (CurrentAmbience != null && !CurrentAmbience.IsDisposed)
                CurrentAmbience.Volume = GameSettings.Current.SoundEffectVolume * GameSettings.Current.MasterVolume;

            MusicPlayer.Update(time);
        }
    }

}