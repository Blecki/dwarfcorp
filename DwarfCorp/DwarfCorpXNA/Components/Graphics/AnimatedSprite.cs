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
    [JsonObject(IsReference = true)]
    public class AnimatedSprite : Tinter, IUpdateableComponent, IRenderableComponent
    {
        public Dictionary<string, Animation> Animations { get; set; }

        [JsonIgnore]
        public AnimationPlayer AnimPlayer = new AnimationPlayer();

        public OrientMode OrientationType { get; set; }
        public bool DrawSilhouette { get; set; }
        public Color SilhouetteColor { get; set; }
        private Vector3 prevDistortion = Vector3.Zero;

        public enum OrientMode
        {
            Fixed,
            Spherical,
            YAxis,
        }

        public bool EnableWind { get; set; }

        public AnimatedSprite(ComponentManager Manager, string name, Matrix localTransform, bool addToCollisionManager) :
            base(Manager, name, localTransform, Vector3.Zero, Vector3.Zero, addToCollisionManager)
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

        public void AddAnimation(Animation animation)
        {
            AnimPlayer.Play(animation);
            Animations[animation.Name] = animation;
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

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch(messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    HasMoved = true;
                    break;
            }


            base.ReceiveMessageRecursive(messageToReceive);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            AnimPlayer.Update(gameTime);
            base.Update(gameTime, chunks, camera);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        new public void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Shader effect,
            bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (!IsVisible)
                return;

            if (!AnimPlayer.HasValidAnimation())
                return;

            if (AnimPlayer.Primitive == null) return;

            //AnimPlayer.PreRender(graphicsDevice);

            // Everything that draws should set it's tint, making this pointless.
            Color origTint = effect.VertexColorTint;

            var currDistortion = VertexNoise.GetNoiseVectorFromRepeatingTexture(GlobalTransform.Translation);
            var distortion = currDistortion * 0.1f + prevDistortion * 0.9f;
            prevDistortion = distortion;
            switch (OrientationType)
            {
                case OrientMode.Spherical:
                    {
                        Matrix bill = Matrix.CreateBillboard(GlobalTransform.Translation, camera.Position, camera.UpVector, null) * Matrix.CreateTranslation(distortion);
                        //Matrix noTransBill = bill;
                        //noTransBill.Translation = Vector3.Zero;
                        
                        //Matrix worldRot = noTransBill;
                        //worldRot.Translation = bill.Translation;// + VertexNoise.GetNoiseVectorFromRepeatingTexture(bill.Translation);
                        effect.World = bill;
                        break;
                    }
                case OrientMode.Fixed:
                    {
                        Matrix rotation = GlobalTransform;
                        rotation.Translation = rotation.Translation + distortion;
                        effect.World = rotation;
                        break;
                    }
                case OrientMode.YAxis:
                    {
                        Matrix worldRot = Matrix.CreateConstrainedBillboard(GlobalTransform.Translation, camera.Position, Vector3.UnitY, null, null);
                        worldRot.Translation = worldRot.Translation + distortion;
                        effect.World = worldRot;
                        break;
                    }
            }
             
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

            if (EnableWind)
            {
                effect.EnableWind = true;
            }

            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                AnimPlayer.Primitive.Render(graphicsDevice);
            }
            effect.VertexColorTint = origTint;
            effect.EnableWind = false;
            EndDraw(effect);
        }
    }

}
