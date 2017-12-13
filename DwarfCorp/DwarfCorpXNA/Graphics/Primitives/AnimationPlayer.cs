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
        public bool IsLooping = false;
        private float FrameTimer = 0.0f;
        public Animation CurrentAnimation = null;

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

        public void Play(Animation Animation)
        {
            CurrentAnimation = Animation;
            IsPlaying = true;
        }

        public void Play()
        {
            IsPlaying = true;
        }

        public void Stop()
        {
            IsPlaying = false;
            CurrentFrame = 0;
        }

        public void Loop(Animation Animation)
        {
            CurrentAnimation = Animation;
            IsPlaying = true;
            IsLooping = true;
        }

        public void StopLooping()
        {
            IsPlaying = false;
            IsLooping = false;
        }

        //Todo: What uses this?
        public void Sychronize(AnimationPlayer Other)
        {
            this.CurrentAnimation = Other.CurrentAnimation;
            this.LastFrame = Other.LastFrame;
            this.CurrentFrame = Other.CurrentFrame;
            this.FrameTimer = Other.FrameTimer;
        }

        public virtual void Update(DwarfTime gameTime, Timer.TimerMode mode = Timer.TimerMode.Game)
        {
            if (IsPlaying && CurrentAnimation != null)
            {
                LastFrame = CurrentFrame;
                float dt = mode == Timer.TimerMode.Game ? (float)gameTime.ElapsedGameTime.TotalSeconds : (float)gameTime.ElapsedRealTime.TotalSeconds;
                FrameTimer += dt;

                float time = CurrentAnimation.FrameHZ;

                if (CurrentAnimation.Speeds.Count > 0)
                    time = CurrentAnimation.Speeds[Math.Min(CurrentFrame, CurrentAnimation.Speeds.Count - 1)];

                if (FrameTimer * CurrentAnimation.SpeedMultiplier >= 1.0f / time)
                {
                    NextFrame();
                    FrameTimer = 0.0f;
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
    }
}