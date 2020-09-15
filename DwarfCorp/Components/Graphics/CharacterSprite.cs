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
    public class CharacterSprite : OrientedAnimatedSprite, ISprite
    {
        [OnSerializing]
        new internal void OnSerializingMethod(StreamingContext context)
        {
            //throw new InvalidOperationException();
        }

        [JsonIgnore]
        public GraphicsDevice Graphics { get { return GameState.Game.GraphicsDevice; } }

        private Timer blinkTimer = new Timer(0.1f, false);
        private Timer coolDownTimer = new Timer(1.0f, false);
        private Timer blinkTrigger = new Timer(0.0f, true);
        private bool isBlinking = false;
        private bool isCoolingDown = false;
        private Color tintOnBlink = Color.White;

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
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

        }

        public CharacterSprite()
        {
        }

        public CharacterSprite(ComponentManager manager, string name, Matrix localTransform) :
                base(manager, name, localTransform)
        {
            CurrentAnimationName = "Idle";
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
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

        bool ISprite.HasAnimation(CharacterMode Mode, SpriteOrientation Orientation)
        {
            return Animations.ContainsKey(Mode.ToString() + SpriteOrientationHelper.OrientationStrings[(int)Orientation]);
        }

        void ISprite.SetCurrentAnimation(string name, bool Play)
        {
            this.SetCurrentAnimation(name, Play);
        }

        void ISprite.Blink(float blinkTime)
        {
            if (isBlinking || isCoolingDown)
            {
                return;
            }

            isBlinking = true;
            tintOnBlink = VertexColorTint;
            blinkTrigger.Reset(blinkTime);
        }

        void ISprite.ResetAnimations(CharacterMode mode)
        {
            SetCurrentAnimation(mode.ToString());
            AnimPlayer.Reset();
        }

        void ISprite.ReloopAnimations(CharacterMode mode)
        {
            SetCurrentAnimation(mode.ToString(), true);
            if (AnimPlayer.IsDone()) AnimPlayer.Reset();
        }

        void ISprite.PauseAnimations()
        {
            AnimPlayer.Pause();
        }

        void ISprite.PlayAnimations()
        {
            AnimPlayer.Play();
        }

        int ISprite.GetCurrentFrame()
        {
            return AnimPlayer.CurrentFrame;
        }

        bool ISprite.HasValidAnimation()
        {
            return AnimPlayer.HasValidAnimation();
        }

        bool ISprite.IsDone()
        {
            return AnimPlayer.IsDone();
        }

        void ISprite.SetDrawSilhouette(bool DrawSilhouette)
        {
        }
    }

}
