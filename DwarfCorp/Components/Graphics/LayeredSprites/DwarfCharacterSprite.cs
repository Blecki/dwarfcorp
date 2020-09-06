using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;

namespace DwarfCorp.DwarfSprites
{
    public class DwarfCharacterSprite : Tinter, ISprite
    {
        public SpriteOrientation CurrentOrientation { get; set; }
        protected string CurrentAnimationName = "";

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            throw new InvalidOperationException();
        }

        protected Dictionary<string, Animation> Animations { get; set; }

        [JsonIgnore] public AnimationPlayer AnimPlayer = new AnimationPlayer();

        public bool DrawSilhouette { get; set; }
        public Color SilhouetteColor { get; set; }
        private Vector3 prevDistortion = Vector3.Zero;

        [JsonIgnore] public GraphicsDevice Graphics { get { return GameState.Game.GraphicsDevice; } }

        private Timer blinkTimer = new Timer(0.1f, false);
        private Timer coolDownTimer = new Timer(1.0f, false);
        private Timer blinkTrigger = new Timer(0.0f, true);
        private bool isBlinking = false;
        private bool isCoolingDown = false;
        private Color tintOnBlink = Color.White;
        public SpriteSheet SpriteSheet = null;
        public BillboardPrimitive Primitive = null;

        private LayerStack Layers = new LayerStack();

        public DwarfCharacterSprite(ComponentManager Manager, string name, Matrix localTransform) :
            base(Manager, name, localTransform, Vector3.One, Vector3.Zero)
        {
            Animations = new Dictionary<string, Animation>();
            DrawSilhouette = false;
            SilhouetteColor = new Color(0.0f, 1.0f, 1.0f, 0.5f);
            CurrentAnimationName = "Idle";

            Manager.World.Renderer.DwarfInstanceRenderer.AddDwarfSprite(this);
        }

        public Rectangle GetCurrentFrameRect()
        {
            if (SpriteSheet == null)
                return new Rectangle(0, 0, 48, 40);
            return SpriteSheet.GetTileRectangle(AnimPlayer.GetCurrentAnimation().Frames[AnimPlayer.CurrentFrame]);
        }

        public DwarfCharacterSprite()
        {
        }

        public virtual void AddAnimation(Animation animation)
        {
            AnimPlayer.Play(animation);
            Animations[animation.Name] = animation;
        }

        public virtual void SetAnimations(Dictionary<String, Animation> Animations)
        {
            this.Animations.Clear();
            foreach (var anim in Animations)
                AddAnimation(anim.Value);
        }

        public Animation GetAnimation(string name)
        {
            return Animations.ContainsKey(name) ? Animations[name] : null;
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            Manager.World.Renderer.DwarfInstanceRenderer.AddDwarfSprite(this);
        }

        public virtual void SetCurrentAnimation(string name, bool Play = false)
        {
            CurrentAnimationName = name;
            var s = CurrentAnimationName + SpriteOrientationHelper.OrientationStrings[(int)CurrentOrientation];
            if (Animations.ContainsKey(s))
                SetCurrentAnimation(Animations[s], Play);
        }

        public void SetCurrentAnimation(Animation Animation, bool Play = false)
        {
            AnimPlayer.ChangeAnimation(Animation, Play ? AnimationPlayer.ChangeAnimationOptions.Play : AnimationPlayer.ChangeAnimationOptions.Stop);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            //base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            //RenderBody(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false, InstanceRenderMode.SelectionBuffer);
        }

        private void RenderBody(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater,
            InstanceRenderMode Mode)
        {
            if (!IsVisible) return;
            if (!AnimPlayer.HasValidAnimation()) return;
            var tex = Layers.GetCompositeTexture();
            if (tex == null || tex.IsDisposed || tex.GraphicsDevice.IsDisposed)
                return;
            if (SpriteSheet == null)
                SpriteSheet = new SpriteSheet(tex, 48, 40);
            SpriteSheet.SwapFixedTexture(tex);


            Color origTint = effect.VertexColorTint;
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            effect.World = GetWorldMatrix(camera);
            effect.MainTexture = tex;
            ApplyTintingToEffect(effect);

            if (Primitive == null)
                Primitive = new BillboardPrimitive();
            Primitive.SetFrame(SpriteSheet, SpriteSheet.GetTileRectangle(AnimPlayer.GetCurrentAnimation().Frames[AnimPlayer.CurrentFrame]), 1.0f, 1.0f, Color.White, Color.White);

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
                    Primitive.Render(graphicsDevice);
                }

                graphicsDevice.DepthStencilState = DepthStencilState.Default;
                effect.VertexColorTint = oldTint;
                effect.CurrentTechnique = oldTechnique;
            }

            effect.EnableWind = false;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }
            effect.VertexColorTint = origTint;
            effect.EnableWind = false;
            EndDraw(effect);
        }

        public Matrix GetWorldMatrix(Camera camera)
        {
            var currDistortion = VertexNoise.GetNoiseVectorFromRepeatingTexture(GlobalTransform.Translation);
            var distortion = currDistortion * 0.1f + prevDistortion * 0.9f;
            prevDistortion = distortion;
            var pos = GlobalTransform.Translation;
            var bill = Matrix.CreateScale(SpriteSheet.FrameWidth / 32.0f, SpriteSheet.FrameHeight / 32.0f, 1.0f) * Matrix.CreateBillboard(pos, camera.Position, camera.UpVector, null) * Matrix.CreateTranslation(distortion);
            return bill;
        }


        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            /*if (!isBlinking)
                VertexColorTint = tintOnBlink;
            else
            {
                if (blinkTimer.CurrentTimeSeconds < 0.5f * blinkTimer.TargetTimeSeconds)
                    VertexColorTint = new Color(new Vector3(1.0f, blinkTimer.CurrentTimeSeconds / blinkTimer.TargetTimeSeconds, blinkTimer.CurrentTimeSeconds / blinkTimer.TargetTimeSeconds));
                else
                    VertexColorTint = tintOnBlink;
            }

            RenderBody(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater, InstanceRenderMode.Normal);

            CurrentOrientation = SpriteOrientationHelper.CalculateSpriteOrientation(camera, GlobalTransform);

            var s = CurrentAnimationName + SpriteOrientationHelper.OrientationStrings[(int)CurrentOrientation];
            if (Animations.ContainsKey(s))
            {
                AnimPlayer.ChangeAnimation(Animations[s], AnimationPlayer.ChangeAnimationOptions.Play);
                AnimPlayer.Update(gameTime);
            }

            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);*/
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            Layers.Update(chunks.World.GraphicsDevice);

            if (isBlinking)
            {
                blinkTimer.Update(gameTime);
                blinkTrigger.Update(gameTime);

                if (blinkTrigger.HasTriggered)
                {
                    isBlinking = false;
                    isCoolingDown = true;
                }
            }

            if (isCoolingDown)
            {
                VertexColorTint = tintOnBlink;
                coolDownTimer.Update(gameTime);

                if (coolDownTimer.HasTriggered)
                {
                    isCoolingDown = false;
                }
            }

            CurrentOrientation = SpriteOrientationHelper.CalculateSpriteOrientation(camera, GlobalTransform);

            var s = CurrentAnimationName + SpriteOrientationHelper.OrientationStrings[(int)CurrentOrientation];
            if (Animations.ContainsKey(s))
                AnimPlayer.ChangeAnimation(Animations[s], AnimationPlayer.ChangeAnimationOptions.Play);

            AnimPlayer.Update(gameTime);

            var tex = Layers.GetCompositeTexture();
            if (tex == null || tex.IsDisposed || tex.GraphicsDevice.IsDisposed)
                return;
            if (SpriteSheet == null)
                SpriteSheet = new SpriteSheet(tex, 48, 40);
            SpriteSheet.SwapFixedTexture(tex);

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
            this.DrawSilhouette = DrawSilhouette;
        }


        public LayerStack GetLayers()
        {
            return Layers;
        }

        public void AddLayer(Layer Layer, Palette Palette)
        {
            Layers.AddLayer(Layer, Palette);
        }

        public void RemoveLayer(String Type)
        {
            Layers.RemoveLayer(Type);
        }
    }
}
