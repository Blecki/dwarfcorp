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
        
        public SpriteSheet SpriteSheet { get; set; }
        public Animation CurrentAnimation { get; set; }
        public OrientMode OrientationType { get; set; }
        public bool DistortPosition { get; set; }
        public bool DrawSilhouette { get; set; }
        public Color SilhouetteColor { get; set; }


        public enum OrientMode
        {
            Fixed,
            Spherical,
            XAxis,
            YAxis,
            ZAxis
        }

        public float BillboardRotation { get; set; }
        public bool EnableWind { get; set; }

        public Sprite(ComponentManager Manager, string name, Matrix localTransform, SpriteSheet spriteSheet, bool addToCollisionManager) :
            base(Manager, name, localTransform, Vector3.Zero, Vector3.Zero, addToCollisionManager)
        {
            SpriteSheet = spriteSheet;
            Animations = new Dictionary<string, Animation>();
            OrientationType = OrientMode.Spherical;
            BillboardRotation = 0.0f;
            DistortPosition = true;
            DrawSilhouette = false;
            SilhouetteColor = new Color(0.0f, 1.0f, 1.0f, 0.5f);
            EnableWind = false;
        }

        public Sprite()
        {
            DistortPosition = true;
        }

        public void SetSimpleAnimation(int row = 0)
        {
            List<Point> frames = new List<Point>();

            for (int c = 0; c < SpriteSheet.Width/SpriteSheet.FrameWidth; c++)
            {
                frames.Add(new Point(c, row));
            }
            AddAnimation(new Animation(GameState.Game.GraphicsDevice, SpriteSheet, "Sprite", frames, true, Color.White, 5.0f, false));
        }

        public void SetSingleFrameAnimation(Point frame)
        {
            AddAnimation(new Animation(GameState.Game.GraphicsDevice, SpriteSheet, "Sprite", new List<Point>() { frame }, true, Color.White, 10.0f, false));
        }

        public void SetSingleFrameAnimation()
        {
            SetSingleFrameAnimation(new Point(0, 0));
        }

        public void AddAnimation(Animation animation)
        {
            if(CurrentAnimation == null)
            {
                CurrentAnimation = animation;
            }
            Animations[animation.Name] = animation;
        }

        public Animation GetAnimation(string name)
        {
            return Animations.ContainsKey(name) ? Animations[name] : null;
        }

        public virtual void SetCurrentAnimation(string name)
        {
            Animation anim = GetAnimation(name);

            if(anim != null)
            {
                CurrentAnimation = anim;
            }
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

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (CurrentAnimation != null)
                CurrentAnimation.Update(gameTime);

            base.Update(gameTime, chunks, camera);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            effect.SelectionBufferColor = GetGlobalIDColor().ToVector4();
            Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        public void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Shader effect,
            bool renderingForWater)
        {
            ApplyTintingToEffect(effect);

            if(!IsVisible)
            {
                return;
            }

            if (CurrentAnimation == null || CurrentAnimation.CurrentFrame < 0 ||
                CurrentAnimation.CurrentFrame >= CurrentAnimation.Primitives.Count) return;

            CurrentAnimation.PreRender();
            SpriteSheet = CurrentAnimation.SpriteSheet;
            if(OrientationType != OrientMode.Fixed)
            {
                if(camera.Projection == Camera.ProjectionMode.Perspective)
                {
                    if(OrientationType == OrientMode.Spherical)
                    {
                        float xscale = GlobalTransform.Left.Length();
                        float yscale = GlobalTransform.Up.Length();
                        float zscale = GlobalTransform.Forward.Length();
                        Matrix rot = Matrix.CreateRotationZ(BillboardRotation);
                        Matrix bill = Matrix.CreateBillboard(GlobalTransform.Translation, camera.Position, camera.UpVector, null);
                        Matrix noTransBill = bill;
                        noTransBill.Translation = Vector3.Zero;

                        Matrix worldRot = Matrix.CreateScale(new Vector3(xscale, yscale, zscale)) * rot * noTransBill;
                        worldRot.Translation = DistortPosition ? bill.Translation + VertexNoise.GetNoiseVectorFromRepeatingTexture(bill.Translation) : bill.Translation;
                        effect.World = worldRot;
                    }
                    else
                    {
                        Vector3 axis = Vector3.Zero;

                        switch(OrientationType)
                        {
                            case OrientMode.XAxis:
                                axis = Vector3.UnitX;
                                break;
                            case OrientMode.YAxis:
                                axis = Vector3.UnitY;
                                break;
                            case OrientMode.ZAxis:
                                axis = Vector3.UnitZ;
                                break;
                        }

                        Matrix worldRot = Matrix.CreateConstrainedBillboard(GlobalTransform.Translation, camera.Position, axis, null, null);
                        worldRot.Translation = DistortPosition ? worldRot.Translation + VertexNoise.GetNoiseVectorFromRepeatingTexture(worldRot.Translation) : worldRot.Translation;
                        effect.World = worldRot;
                    }
                }
                else
                {
                    Matrix rotation = Matrix.CreateRotationY(-(float) Math.PI * 0.25f) * Matrix.CreateTranslation(GlobalTransform.Translation);
                    rotation.Translation = DistortPosition ? rotation.Translation + VertexNoise.GetNoiseVectorFromRepeatingTexture(rotation.Translation) : rotation.Translation;
                    effect.World = rotation;
                }
            }
            else
            {
                Matrix rotation = GlobalTransform;
                rotation.Translation = DistortPosition ? rotation.Translation + VertexNoise.GetNoiseVectorFromRepeatingTexture(rotation.Translation) : rotation.Translation;
                effect.World = rotation;
            }

                
            effect.MainTexture = SpriteSheet.GetTexture();
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
                    CurrentAnimation.Primitives[CurrentAnimation.CurrentFrame].Render(graphicsDevice);
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
                CurrentAnimation.Primitives[CurrentAnimation.CurrentFrame].Render(graphicsDevice);
            }

            effect.EnableWind = false;
        }
    }

}
