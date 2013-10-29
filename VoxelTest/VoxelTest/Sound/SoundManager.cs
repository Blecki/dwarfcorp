using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{
    public class Sound3D
    {
        public SoundEffectInstance EffectInstance;
        public Vector3 Position;
        public bool HasStarted;
        public string name;
    }

    public class SoundManager
    {
        public static List<Sound3D> ActiveSounds = new List<Sound3D>();
        public static AudioListener Listener = new AudioListener();
        public static AudioEmitter Emitter = new AudioEmitter();
        public static ContentManager Content { get; set; }
        public static int MaxSounds = 20;
        public static Dictionary<string, int> SoundCounts = new Dictionary<string, int>();
        public static Dictionary<string, SoundEffect> EffectLibrary = new Dictionary<string, SoundEffect>();

        public static void LoadDefaultSounds()
        {
            string[] defaultSounds = { "pick", "hit", "jump", "ouch", "gravel", "splash", "sword" };

            foreach (string name in defaultSounds)
            {
                SoundEffect effect = Content.Load<SoundEffect>(name);
                EffectLibrary[name] = effect;
            }
        }

        public static void PlayMusic(string name)
        {
            if (GameSettings.Default.MasterVolume < 0.001f || GameSettings.Default.MusicVolume < 0.001f)
            {
                return;
            }

            try
            {
                MediaPlayer.Play(Content.Load<Song>(name));
                MediaPlayer.Volume = GameSettings.Default.MasterVolume * GameSettings.Default.MusicVolume;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return;
            }
        }

        public static void PlaySound(string name, Vector3 location, bool randomPitch)
        {
            if (Content == null)
            {
                return;
            }
            try
            {
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


                Sound3D sound = new Sound3D();
                sound.Position = location;
                sound.EffectInstance = effect.CreateInstance();
                sound.EffectInstance.IsLooped = false;
                sound.HasStarted = false;
                sound.name = name;

                if (randomPitch)
                {
                    sound.EffectInstance.Pitch = (float)(PlayState.random.NextDouble() * 1.0f - 0.5f);
                }

                if (!SoundCounts.ContainsKey(name))
                {
                    SoundCounts[name] = 0;
                }

                if (SoundCounts[name] < MaxSounds)
                {
                    SoundCounts[name]++;
                    ActiveSounds.Add(sound);
                }
                else
                {
                    sound.EffectInstance.Stop();
                    sound.EffectInstance.Dispose();
                    sound.EffectInstance = null;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
                return;
            }
        }

        public static void PlaySound(string name, Vector3 location)
        {
            PlaySound(name, location, false);
        }

        public static void Update(GameTime time, Camera camera)
        {
            List<Sound3D> toRemove = new List<Sound3D>();

            Matrix viewInverse = Matrix.Invert(camera.ViewMatrix);
            Listener.Position = camera.Position;
            Listener.Up = viewInverse.Up;
            Listener.Velocity = camera.Velocity;
            Listener.Forward = viewInverse.Forward;
          


            foreach (Sound3D instance in ActiveSounds)
            {
                if (instance.HasStarted && instance.EffectInstance.State == SoundState.Stopped || instance.EffectInstance.State == SoundState.Paused)
                {
                    instance.EffectInstance.Dispose();
                    toRemove.Add(instance);
                    SoundCounts[instance.name]--;
                }
                else if (!instance.HasStarted)
                {
                    Emitter.Position = instance.Position;
                    instance.EffectInstance.Apply3D(Listener, Emitter);
                    instance.EffectInstance.Volume *= (GameSettings.Default.MasterVolume * GameSettings.Default.SoundEffectVolume);
                 
                    instance.EffectInstance.Play();
                    instance.HasStarted = true;
                }
            }

            foreach (Sound3D r in toRemove)
            {
                ActiveSounds.Remove(r);
            }
        }
    }
}
