// CharacterSprite.cs
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
    /// <summary>
    /// This is a special kind of sprite which assumes that it is attached to a character
    /// which has certain animations and can face in four directions. Also provides interfaces to
    /// certain effects such as blinking.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CharacterSprite : OrientedAnimatedSprite, IUpdateableComponent, IRenderableComponent
    {
        [OnSerialized]
        private void _onSerialized(StreamingContext Context)
        {
            var x = 5;

        }

        [JsonIgnore]
        public GraphicsDevice Graphics { get; set; }

        private Timer blinkTimer = new Timer(0.1f, false);
        private Timer coolDownTimer = new Timer(1.0f, false);
        private Timer blinkTrigger = new Timer(0.0f, true);
        private bool isBlinking = false;
        private bool isCoolingDown = false;
        private Color tintOnBlink = Color.White;

        public override void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            if (!isBlinking)
            {
                VertexColorTint = tintOnBlink;
                base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            }
            else
            {
                if (blinkTimer.CurrentTimeSeconds < 0.5f*blinkTimer.TargetTimeSeconds)
                {
                    VertexColorTint = new Color(new Vector3(1.0f, blinkTimer.CurrentTimeSeconds / blinkTimer.TargetTimeSeconds, blinkTimer.CurrentTimeSeconds / blinkTimer.TargetTimeSeconds));
                }
                else
                {
                    VertexColorTint = tintOnBlink;
                }
                base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Graphics = (context.Context as WorldManager).ChunkManager.Graphics;
        }

        public CharacterSprite()
        {
            currentMode = "Idle";
        }

        public CharacterSprite(GraphicsDevice graphics, ComponentManager manager, string name, 
            Matrix localTransform) :
                base(manager, name, localTransform)
        {
            Graphics = graphics;
            currentMode = "Idle";
        }

        public bool HasAnimation(CharacterMode mode, Orientation orient)
        {
            return Animations.ContainsKey(mode.ToString() + OrientationStrings[(int) orient]);
        }

        public List<Animation> GetAnimations(CharacterMode mode)
        {
            return
                OrientationStrings.Where((t, i) => HasAnimation(mode, (Orientation) i))
                    .Select(t => Animations[mode.ToString() + t])
                    .ToList();
        }

        public void ReloopAnimations(CharacterMode mode)
        {
            SetCurrentAnimation(mode.ToString(), true);
            if (AnimPlayer.IsDone()) AnimPlayer.Reset();
        }

        public void ResetAnimations(CharacterMode mode)
        {
            SetCurrentAnimation(mode.ToString());
            AnimPlayer.Reset();
        }

        public void Blink(float blinkTime)
        {
            if(isBlinking || isCoolingDown)
            {
                return;
            }

            isBlinking = true;
            tintOnBlink = VertexColorTint;
            blinkTrigger.Reset(blinkTime);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if(isBlinking)
            {
                blinkTimer.Update(gameTime);
                blinkTrigger.Update(gameTime);

                if(blinkTrigger.HasTriggered)
                {
                    isBlinking = false;
                    isCoolingDown = true;
                }
            }

            if(isCoolingDown)
            {
                VertexColorTint = tintOnBlink;
                coolDownTimer.Update(gameTime);

                if(coolDownTimer.HasTriggered)
                {
                    isCoolingDown = false;
                }
            }

            base.Update(gameTime, chunks, camera);
        }

        public void PauseAnimations(CharacterMode mode)
        {
            AnimPlayer.Pause();
        }

        public void PlayAnimations(CharacterMode mode)
        {
            AnimPlayer.Play();
        }
    }

}
