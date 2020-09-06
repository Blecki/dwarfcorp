using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class AnimationPlayer
    {
        public int CurrentFrame = 0;
        public int LastFrame = 0;
        public bool IsPlaying = false;
        private bool IsLooping = false;
        private float FrameTimer = 0.0f;
        private Animation CurrentAnimation = null;

        public Animation GetCurrentAnimation()
        {
            return CurrentAnimation;
        }

        public AnimationPlayer() { }

        public AnimationPlayer(Animation Animation)
        {
            Play(Animation);
        }

        public void Reset()
        {
            CurrentFrame = 0;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public enum ChangeAnimationOptions
        {
            NoStateChange = 0,
            Reset = 1,
            Play = 2,
            Stop = 8,

            ResetAndPlay = Reset | Play,
            ResetAndStop = Reset | Stop
        }

        public void ChangeAnimation(Animation Animation, ChangeAnimationOptions Options)
        {
            CurrentAnimation = Animation;

            if ((Options & ChangeAnimationOptions.Reset) == ChangeAnimationOptions.Reset)
                CurrentFrame = 0;

            if ((Options & ChangeAnimationOptions.Play) == ChangeAnimationOptions.Play)
                IsPlaying = true;

            if ((Options & ChangeAnimationOptions.Stop) == ChangeAnimationOptions.Stop)
                IsPlaying = false;

            if (CurrentAnimation != null)
            {
                IsLooping = Animation.Loops;
                if (CurrentFrame >= Animation.GetFrameCount())
                    CurrentFrame = Animation.GetFrameCount() - 1;
            }
        }

        public void Play(Animation Animation)
        {
            CurrentAnimation = Animation;
            if (CurrentFrame >= Animation.GetFrameCount())
                CurrentFrame = Animation.GetFrameCount() - 1;
            IsPlaying = true;
            IsLooping = Animation.Loops;
        }

        public void Play()
        {
            IsPlaying = true;
            if (CurrentAnimation != null)
                IsLooping = CurrentAnimation.Loops;
        }

        public void Stop()
        {
            IsPlaying = false;
            CurrentFrame = 0;
        }

        public virtual void Update(DwarfTime gameTime, Timer.TimerMode mode = Timer.TimerMode.Game)
        {
            if (CurrentAnimation == null)
                return;

            if (IsPlaying)
            {
                LastFrame = CurrentFrame;
                float dt = mode == Timer.TimerMode.Game ? (float)gameTime.ElapsedGameTime.TotalSeconds : (float)gameTime.ElapsedRealTime.TotalSeconds;
                FrameTimer += dt;
                float hz = CurrentAnimation.FrameHZ > 0 ? CurrentAnimation.FrameHZ : 1;
                float time = 1.0f / hz;

                if (CurrentAnimation.Speeds.Count > 0)
                    time = CurrentAnimation.Speeds[Math.Min(CurrentFrame, CurrentAnimation.Speeds.Count - 1)];

                time /= CurrentAnimation.SpeedMultiplier;

                if (FrameTimer >= time)
                {
                    NextFrame();
                    FrameTimer = 0;
                }
            }
        }

        public void NextFrame()
        {
            CurrentFrame++;

            if (CurrentAnimation != null && CurrentFrame >= CurrentAnimation.GetFrameCount())
            {
                if (IsLooping)
                    CurrentFrame = 0;
                else
                    CurrentFrame = CurrentAnimation.GetFrameCount() - 1;
            }
        }

        public bool IsDone()
        {
            return CurrentAnimation == null || CurrentFrame >= CurrentAnimation.GetFrameCount() - 1;
        }

        public int GetFrame(float time)
        {
            if (CurrentAnimation == null) return 0;

            if (IsLooping)
                return (int)(time * CurrentAnimation.FrameHZ) % CurrentAnimation.GetFrameCount();
            else
                return Math.Min((int)(time * CurrentAnimation.FrameHZ), CurrentAnimation.GetFrameCount() - 1);
        }

        public bool HasValidAnimation()
        {
            return CurrentAnimation != null;
        }
    }
}