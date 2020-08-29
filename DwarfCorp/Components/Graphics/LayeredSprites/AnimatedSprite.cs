using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.DwarfSprites
{
    public class AnimatedSprite : Tinter
    {
        public enum Orientation
        {
            Right = 0,
            Left = 1,
            Forward = 2,
            Backward = 3
        }

        protected static string[] OrientationStrings =
        {
            "RIGHT",
            "LEFT",
            "FORWARD",
            "BACKWARD"
        };

        public Orientation CurrentOrientation { get; set; }

        protected string currentMode = "";

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            //throw new InvalidOperationException();
        }

        protected Dictionary<string, Animation> Animations { get; set; }

        [JsonIgnore] public AnimationPlayer AnimPlayer = new AnimationPlayer();

        public bool DrawSilhouette { get; set; }
        public Color SilhouetteColor { get; set; }
        private Vector3 prevDistortion = Vector3.Zero;

        public AnimatedSprite(ComponentManager Manager, string name, Matrix localTransform) :
            base(Manager, name, localTransform, Vector3.One, Vector3.Zero)
        {
            Animations = new Dictionary<string, Animation>();
            DrawSilhouette = false;
            SilhouetteColor = new Color(0.0f, 1.0f, 1.0f, 0.5f);
        }

        public AnimatedSprite()
        {
        }

        public virtual void AddAnimation(Animation animation)
        {
            AnimPlayer.Play(animation);
            Animations[animation.Name] = animation;
        }

        public virtual void SetAnimations(Dictionary<String, Animation> Animations)
        {
            this.Animations = Animations;
        }

        public Animation GetAnimation(string name)
        {
            return Animations.ContainsKey(name) ? Animations[name] : null;
        }

        public virtual void SetCurrentAnimation(string name, bool Play = false)
        {
            currentMode = name;
            var s = currentMode + OrientationStrings[(int)CurrentOrientation];
            if (Animations.ContainsKey(s))
                SetCurrentAnimation(Animations[s], Play);
        }

        public void SetCurrentAnimation(Animation Animation, bool Play = false)
        {
            AnimPlayer.ChangeAnimation(Animation, Play ? AnimationPlayer.ChangeAnimationOptions.Play : AnimationPlayer.ChangeAnimationOptions.Stop);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            AnimPlayer.Update(gameTime, !DrawSilhouette); // Can't use instancing if we want the silhouette.
            base.Update(gameTime, chunks, camera);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            RenderBody(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false, InstanceRenderMode.SelectionBuffer);
        }

        override public void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Shader effect,
            bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            RenderBody(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater, InstanceRenderMode.Normal);

            CalculateCurrentOrientation(camera);


            var s = currentMode + OrientationStrings[(int)CurrentOrientation];
            if (Animations.ContainsKey(s))
            {
                AnimPlayer.ChangeAnimation(Animations[s], AnimationPlayer.ChangeAnimationOptions.Play);
                AnimPlayer.Update(gameTime, true);
            }

        }

        private void RenderBody(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater,
            InstanceRenderMode Mode)
        {
            if (!IsVisible) return;
            if (!AnimPlayer.HasValidAnimation()) return;

                if (AnimPlayer.Primitive == null) return;

                Color origTint = effect.VertexColorTint;
                effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
                effect.World = GetWorldMatrix(camera);
                var tex = AnimPlayer.GetTexture();
                if (tex != null && !tex.IsDisposed && !tex.GraphicsDevice.IsDisposed)
                    effect.MainTexture = AnimPlayer.GetTexture();
                ApplyTintingToEffect(effect);

                if (DrawSilhouette)
                {
                    Color oldTint = effect.VertexColorTint;
                    effect.VertexColorTint = SilhouetteColor;
                    graphicsDevice.DepthStencilState = DepthStencilState.None;
                    var oldTechnique = effect.CurrentTechnique;
                    effect.CurrentTechnique = effect.Techniques[Shader.Technique.Silhouette];
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        AnimPlayer.Primitive.Render(graphicsDevice);
                    }

                    graphicsDevice.DepthStencilState = DepthStencilState.Default;
                    effect.VertexColorTint = oldTint;
                    effect.CurrentTechnique = oldTechnique;
                }

                    effect.EnableWind = false;

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    AnimPlayer.Primitive.Render(graphicsDevice);
                }
                effect.VertexColorTint = origTint;
                effect.EnableWind = false;
                EndDraw(effect);
        }

        private Matrix GetWorldMatrix(Camera camera)
        {
            var currDistortion = VertexNoise.GetNoiseVectorFromRepeatingTexture(GlobalTransform.Translation);
            var distortion = currDistortion * 0.1f + prevDistortion * 0.9f;
            prevDistortion = distortion;
            var frameSize = AnimPlayer.GetCurrentFrameSize();
            var offsets = AnimPlayer.GetCurrentAnimation().YOffset;
            float verticalOffset = offsets == null || offsets.Count == 0 ? 0.0f : offsets[Math.Min(AnimPlayer.CurrentFrame, offsets.Count - 1)] * 1.0f / 32.0f;
            var pos = GlobalTransform.Translation + Vector3.Up * verticalOffset;
            var bill = Matrix.CreateScale(frameSize.X, frameSize.Y, 1.0f) * Matrix.CreateBillboard(pos, camera.Position, camera.UpVector, null) * Matrix.CreateTranslation(distortion);
            return bill;
        }

        public void CalculateCurrentOrientation(Camera camera)
        {
            float xComponent = Vector3.Dot(camera.ViewMatrix.Forward, GlobalTransform.Left);
            float yComponent = Vector3.Dot(camera.ViewMatrix.Forward, GlobalTransform.Forward);

            // Todo: There should be a way to do this without trig.
            float angle = (float)Math.Atan2(yComponent, xComponent);

            if (angle > 3.0f * MathHelper.PiOver4) // 135 degrees
                CurrentOrientation = Orientation.Right;
            else if (angle > MathHelper.PiOver4) // 45 degrees
                CurrentOrientation = Orientation.Backward;
            else if (angle > -MathHelper.PiOver4) // -45 degrees
                CurrentOrientation = Orientation.Left;
            else if (angle > -3.0f * MathHelper.PiOver4) // -135 degrees
                CurrentOrientation = Orientation.Forward;
            else
                CurrentOrientation = Orientation.Right;
        }

    }

}
