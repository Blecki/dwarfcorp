using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;

namespace DwarfCorp
{    
    /// <summary>
    /// Holds a particular instant of a 3D sound, its location, and its volume.
    /// </summary>
    public static class MusicPlayer
    {
        public class Track
        {
            public Song Intro;
            public Song Loop;
        }

        public static Track CurrentTrack = null;
        public static Track NextTrack = null;
        public static Song CurrentlyPlaying = null;

        private enum PlayerState
        {
            FadeIn,
            Loop,
            FadeOut
        }

        private static PlayerState State;
        private static float PlayTime = 0.0f;
        private static float FadeTime = 0.0f;
        private static float FadeDuration = 5.0f;

        private static void SetActiveSong()
        {
            Song newSong = null;
            if (CurrentTrack == null)
                return;
            if (CurrentTrack.Intro != null && PlayTime < CurrentTrack.Intro.Duration.TotalSeconds)
                newSong = CurrentTrack.Intro;
            else
                newSong = CurrentTrack.Loop;

            if (!object.ReferenceEquals(newSong, CurrentlyPlaying))
                MediaPlayer.Play(newSong);
            CurrentlyPlaying = newSong;
        }

        public static void CueSong(Song Intro, Song Loop)
        {
            NextTrack = new Track
            {
                Intro = Intro,
                Loop = Loop
            };

            State = PlayerState.FadeOut;
            if (FadeTime > FadeDuration) FadeTime = FadeDuration;
        }

        public static void Update(DwarfTime Time)
        {
            MediaPlayer.IsRepeating = true;
            PlayTime += (float)Time.ElapsedRealTime.TotalSeconds;
            SetActiveSong();

            if (CurrentTrack == null)
            {
                CurrentTrack = NextTrack;
                State = PlayerState.FadeIn;
                FadeTime = 0.0f;
                PlayTime = 0.0f;
            }

            switch (State)
            {
                case PlayerState.FadeIn:
                    MediaPlayer.Volume = GameSettings.Current.MasterVolume * GameSettings.Current.MusicVolume * (FadeTime / FadeDuration);
                    FadeTime += (float)Time.ElapsedRealTime.TotalSeconds;
                    if (FadeTime >= FadeDuration)
                        State = PlayerState.Loop;
                    break;
                case PlayerState.Loop:
                    MediaPlayer.Volume = GameSettings.Current.MasterVolume * GameSettings.Current.MusicVolume;
                    break;
                case PlayerState.FadeOut:
                    MediaPlayer.Volume = GameSettings.Current.MasterVolume * GameSettings.Current.MusicVolume * (FadeTime / FadeDuration);
                    FadeTime -= (float)Time.ElapsedRealTime.TotalSeconds;
                    if (FadeTime < 0)
                        CurrentTrack = null;
                    break;
            }
        }       
    }
}