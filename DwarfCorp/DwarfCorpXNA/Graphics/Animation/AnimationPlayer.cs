// AnimationPlayer.cs
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
        public BillboardPrimitive Primitive = null;
        public bool InstancingPossible { get; private set; }

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
            IsLooping = Animation.Loops;

            if ((Options & ChangeAnimationOptions.Reset) == ChangeAnimationOptions.Reset)
                CurrentFrame = 0;

            if ((Options & ChangeAnimationOptions.Play) == ChangeAnimationOptions.Play)
                IsPlaying = true;

            if ((Options & ChangeAnimationOptions.Stop) == ChangeAnimationOptions.Stop)
                IsPlaying = false;

            if (CurrentAnimation != null)
            {
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

        //Todo: What uses this?
        public void Sychronize(AnimationPlayer Other)
        {
            this.CurrentAnimation = Other.CurrentAnimation;
            this.LastFrame = Other.LastFrame;
            this.CurrentFrame = Other.CurrentFrame;
            this.FrameTimer = Other.FrameTimer;
        }

        public virtual void Update(DwarfTime gameTime, bool WillUseInstancingIfPossible, Timer.TimerMode mode = Timer.TimerMode.Game)
        {
            InstancingPossible = false;

            if (CurrentAnimation == null)
                return;

            if (IsPlaying)
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


            if (!WillUseInstancingIfPossible || !CurrentAnimation.CanUseInstancing)
            {
                // Todo: Only update when actually needed.
                if (Primitive == null)
                    Primitive = new BillboardPrimitive();
                CurrentAnimation.UpdatePrimitive(Primitive, CurrentFrame);
            }
            else
                InstancingPossible = true;
        }

        public void UpdateInstance(NewInstanceData InstanceData)
        {
            var sheet = CurrentAnimation.SpriteSheet;
            var frame = CurrentAnimation.Frames[CurrentFrame];
            InstanceData.SpriteBounds = new Rectangle(sheet.FrameWidth * frame.X, sheet.FrameHeight * frame.Y, sheet.FrameWidth, sheet.FrameHeight);
            InstanceData.TextureAsset = sheet.AssetName;
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

        public Texture2D GetTexture()
        {
            if (CurrentAnimation != null)
                return CurrentAnimation.GetTexture();
            return null;
        }

        public bool HasValidAnimation()
        {
            return CurrentAnimation != null;
        }
    }
}