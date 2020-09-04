using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    /// <summary>
    /// This is an animated "billboard". Essentially, a simple rectangle is drawn with a texture on it.
    /// The rectangle is drawn in such a way that it is always more or less facing the camera.
    /// </summary>
    public class AnimatedSprite : Tinter
    {
        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            //throw new InvalidOperationException();
        }

        protected Dictionary<string, Animation> Animations { get; set; }

        [JsonIgnore]
        public AnimationPlayer AnimPlayer = new AnimationPlayer();

        public OrientMode OrientationType { get; set; }
        public bool DrawSilhouette { get; set; }
        public Color SilhouetteColor { get; set; }
        private Vector3 prevDistortion = Vector3.Zero;

        private NewInstanceData InstanceData;
        [JsonIgnore] public SpriteSheet SpriteSheet = null;
        [JsonIgnore] public BillboardPrimitive Primitive = null;


        public enum OrientMode
        {
            Fixed,
            Spherical,
            YAxis,
        }

        public bool EnableWind { get; set; }

        public AnimatedSprite(ComponentManager Manager, string name, Matrix localTransform) :
            base(Manager, name, localTransform, Vector3.One, Vector3.Zero)
        {
            Animations = new Dictionary<string, Animation>();
            OrientationType = OrientMode.Spherical;
            DrawSilhouette = false;
            SilhouetteColor = new Color(0.0f, 1.0f, 1.0f, 0.5f);
            EnableWind = false;
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
            var anim = GetAnimation(name);
            SetCurrentAnimation(anim, Play);
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

        private void PrepareInstanceData(Camera Camera)
        {
            if (InstanceData == null)
                InstanceData = new NewInstanceData(Manager.World.Renderer.InstanceRenderer.PrepareCombinedTiledInstance(), Matrix.Identity, Color.White);
            
            InstanceData.Transform = GetWorldMatrix(Camera);
            InstanceData.LightRamp = LightRamp;
            InstanceData.VertexColorTint = VertexColorTint;
            InstanceData.SelectionBufferColor = this.GetGlobalIDColor();
            if (Stipple)
            {
                InstanceData.VertexColorTint.A = 256 / 2;
            }
            if (SpriteSheet == null)
            {
                if (AnimPlayer.GetCurrentAnimation() != null && AnimPlayer.GetCurrentAnimation().Frames.Count > AnimPlayer.CurrentFrame && AnimPlayer.CurrentFrame >= 0)
                {
                    var frame = AnimPlayer.GetCurrentAnimation().Frames[AnimPlayer.CurrentFrame];
                    InstanceData.SpriteBounds = SpriteSheet.GetTileRectangle(frame);
                    InstanceData.TextureAsset = SpriteSheet.AssetName;
                }
            }
            else
            {
                var sheet = SpriteSheet;
                var frame = AnimPlayer.GetCurrentAnimation().Frames[AnimPlayer.CurrentFrame];
                InstanceData.SpriteBounds = new Rectangle(sheet.FrameWidth * frame.X, sheet.FrameHeight * frame.Y, sheet.FrameWidth, sheet.FrameHeight);
                InstanceData.TextureAsset = sheet.AssetName;
            }
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
        }

        private void RenderBody(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater,
            InstanceRenderMode Mode)
        {
            if (!IsVisible) return;
            if (!AnimPlayer.HasValidAnimation()) return;

            if (AnimPlayer.InstancingPossible)
            {
                PrepareInstanceData(camera);
                Manager.World.Renderer.InstanceRenderer.RenderInstance(InstanceData, graphicsDevice, effect, camera, Mode);
            }
            else
            {
                if (Primitive == null)
                    Primitive = new BillboardPrimitive();
                Primitive.SetFrame(SpriteSheet, SpriteSheet.GetTileRectangle(AnimPlayer.GetCurrentAnimation().Frames[AnimPlayer.CurrentFrame]), 1.0f, 1.0f, Color.White, Color.White);

                Color origTint = effect.VertexColorTint;
                effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
                effect.World = GetWorldMatrix(camera);
                var tex = SpriteSheet.GetTexture();
                if (tex != null && !tex.IsDisposed && !tex.GraphicsDevice.IsDisposed)
                    effect.MainTexture = tex;
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
                        Primitive.Render(graphicsDevice);
                    }

                    graphicsDevice.DepthStencilState = DepthStencilState.Default;
                    effect.VertexColorTint = oldTint;
                    effect.CurrentTechnique = oldTechnique;
                }

                if (EnableWind)
                {
                    effect.EnableWind = true;
                }

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Primitive.Render(graphicsDevice);
                }
                effect.VertexColorTint = origTint;
                effect.EnableWind = false;
                EndDraw(effect);
            }
        }

        private Matrix GetWorldMatrix(Camera camera)
        {
            var currDistortion = VertexNoise.GetNoiseVectorFromRepeatingTexture(GlobalTransform.Translation);
            var distortion = currDistortion * 0.1f + prevDistortion * 0.9f;
            prevDistortion = distortion;
            var frameSize = new Vector2(SpriteSheet.FrameWidth / 32.0f, SpriteSheet.FrameHeight / 32.0f);
            var pos = GlobalTransform.Translation;
            switch (OrientationType)
            {
                case OrientMode.Spherical:
                    {
                        Matrix bill = Matrix.CreateScale(frameSize.X, frameSize.Y, 1.0f) * Matrix.CreateBillboard(pos, camera.Position, camera.UpVector, null) * Matrix.CreateTranslation(distortion);
                        //Matrix noTransBill = bill;
                        //noTransBill.Translation = Vector3.Zero;

                        //Matrix worldRot = noTransBill;
                        //worldRot.Translation = bill.Translation;// + VertexNoise.GetNoiseVectorFromRepeatingTexture(bill.Translation);
                        return bill;
                    }
                case OrientMode.Fixed:
                    {
                        Matrix rotation = Matrix.CreateScale(frameSize.X, frameSize.Y, 1.0f) * GlobalTransform;
                        rotation.Translation = pos + distortion;
                        return rotation;
                    }
                case OrientMode.YAxis:
                    {
                        Matrix worldRot = Matrix.CreateScale(frameSize.X, frameSize.Y, 1.0f) * Matrix.CreateConstrainedBillboard(pos, camera.Position, Vector3.UnitY, null, null);
                        worldRot.Translation = worldRot.Translation + distortion;
                        return worldRot;
                    }
                default:
                    throw new InvalidProgramException();
            }
        }
    }

}
