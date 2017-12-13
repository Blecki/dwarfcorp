using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This is an animated "billboard". Essentially, a simple rectangle is drawn with a texture on it.
    /// The rectangle is drawn in such a way that it is always more or less facing the camera.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Sprite : Tinter, IUpdateableComponent, IRenderableComponent
    {
        public Dictionary<string, Animation> Animations { get; set; }

        [JsonIgnore]
        public AnimationPlayer AnimPlayer = new AnimationPlayer();

        public SpriteSheet SpriteSheet { get; set; }
        //public Animation CurrentAnimation { get; set; }
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

        public Sprite(ComponentManager Manager, string name, Matrix localTransform, SpriteSheet spriteSheet, bool addToCollisionManager) :
            base(Manager, name, localTransform, Vector3.Zero, Vector3.Zero, addToCollisionManager)
        {
            SpriteSheet = spriteSheet;
            Animations = new Dictionary<string, Animation>();
            OrientationType = OrientMode.Spherical;
            DrawSilhouette = false;
            SilhouetteColor = new Color(0.0f, 1.0f, 1.0f, 0.5f);
            EnableWind = false;
        }

        public Sprite()
        {
        }

        public void SetSimpleAnimation(int row = 0)
        {
            List<Point> frames = new List<Point>();

            for (int c = 0; c < SpriteSheet.Width/SpriteSheet.FrameWidth; c++)
            {
                frames.Add(new Point(c, row));
            }
            AddAnimation(new Animation(GameState.Game.GraphicsDevice, SpriteSheet, "Sprite", frames, Color.White, 5.0f, false));
        }

        public void SetSingleFrameAnimation(Point frame)
        {
            AddAnimation(new Animation(GameState.Game.GraphicsDevice, SpriteSheet, "Sprite", new List<Point>() { frame }, Color.White, 10.0f, false));
        }

        public void SetSingleFrameAnimation()
        {
            SetSingleFrameAnimation(new Point(0, 0));
        }

        public void AddAnimation(Animation animation)
        {
            AnimPlayer.Play(animation);
            //if(CurrentAnimation == null)
            //{
            //    CurrentAnimation = animation;
            //}
            Animations[animation.Name] = animation;
        }

        public Animation GetAnimation(string name)
        {
            return Animations.ContainsKey(name) ? Animations[name] : null;
        }

        public virtual void SetCurrentAnimation(string name, bool Play = false)
        {
            Animation anim = GetAnimation(name);
            SetCurrentAnimation(anim, Play);
        }

        public void SetCurrentAnimation(Animation Animation, bool Play = false)
        {
            AnimPlayer.CurrentAnimation = Animation;
            if (Play) AnimPlayer.Play(Animation);
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
            if (AnimPlayer.CurrentAnimation != null)
                AnimPlayer.CurrentAnimation.Update(gameTime, AnimPlayer.CurrentFrame);
            //if (CurrentAnimation != null)
            //    CurrentAnimation.Update(gameTime);

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

        public virtual void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Shader effect,
            bool renderingForWater)
        {
            if (!IsVisible)
                return;

            if (AnimPlayer.CurrentAnimation == null)
                return;
            var frame = AnimPlayer.CurrentFrame;
            if (frame < 0 || frame >= AnimPlayer.CurrentAnimation.Primitives.Count)
                return;
            //if (CurrentAnimation == null || CurrentAnimation.CurrentFrame < 0 ||
            //    CurrentAnimation.CurrentFrame >= CurrentAnimation.Primitives.Count) return;

            GamePerformance.Instance.StartTrackPerformance("Render - Sprite");

            // Everything that draws should set it's tint, making this pointless.
            Color origTint = effect.VertexColorTint;  
            AnimPlayer.CurrentAnimation.PreRender();
            SpriteSheet = AnimPlayer.CurrentAnimation.SpriteSheet;
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
             
            effect.MainTexture = SpriteSheet.GetTexture();
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
                    AnimPlayer.CurrentAnimation.Primitives[AnimPlayer.CurrentFrame].Render(graphicsDevice);
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
                AnimPlayer.CurrentAnimation.Primitives[AnimPlayer.CurrentFrame].Render(graphicsDevice);
            }
            effect.VertexColorTint = origTint;
            effect.EnableWind = false;

            GamePerformance.Instance.StopTrackPerformance("Render - Sprite");
        }
    }

}
