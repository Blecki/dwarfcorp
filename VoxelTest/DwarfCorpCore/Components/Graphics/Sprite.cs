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
    public class Sprite : Tinter
    {
        public Dictionary<string, Animation> Animations { get; set; }

        public Texture2D SpriteSheet { get; set; }

        public Animation CurrentAnimation { get; set; }

        public OrientMode OrientationType { get; set; }


        public enum OrientMode
        {
            Fixed,
            Spherical,
            XAxis,
            YAxis,
            ZAxis
        }

        public float BillboardRotation { get; set; }

        private static readonly RasterizerState RasterState = new RasterizerState()
        {
            CullMode = CullMode.None,
        };

        public Sprite(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Texture2D spriteSheet, bool addToOctree) :
            base(name, parent, localTransform, Vector3.Zero, Vector3.Zero, addToOctree)
        {
            SpriteSheet = spriteSheet;
            Animations = new Dictionary<string, Animation>();
            OrientationType = OrientMode.Spherical;
            BillboardRotation = 0.0f;
        }

        public Sprite()
        {
           
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

        public override void Update(DwarfTime DwarfTime, ChunkManager chunks, Camera camera)
        {
            if(IsActive)
            {
                if(CurrentAnimation != null)
                {
                    CurrentAnimation.Update(DwarfTime);
                }
            }


            base.Update(DwarfTime, chunks, camera);
        }

        public override void Render(DwarfTime DwarfTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Effect effect,
            bool renderingForWater)
        {
            base.Render(DwarfTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if(!IsVisible)
            {
                return;
            }

            RasterizerState r = graphicsDevice.RasterizerState;
            graphicsDevice.RasterizerState = RasterState;


            if (CurrentAnimation != null && CurrentAnimation.CurrentFrame >= 0 && CurrentAnimation.CurrentFrame < CurrentAnimation.Primitives.Count)
            {
                CurrentAnimation.PreRender();
                SpriteSheet = CurrentAnimation.SpriteSheet;
                effect.Parameters["xTexture"].SetValue(SpriteSheet);

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
                            worldRot.Translation = bill.Translation;
                            effect.Parameters["xWorld"].SetValue(worldRot);
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
                            effect.Parameters["xWorld"].SetValue(worldRot);
                        }
                    }
                    else
                    {
                        Matrix rotation = Matrix.CreateRotationY(-(float) Math.PI * 0.25f) * Matrix.CreateTranslation(GlobalTransform.Translation);
                        effect.Parameters["xWorld"].SetValue(rotation);
                    }
                }
                else
                {
                    effect.Parameters["xWorld"].SetValue(GlobalTransform);
                }


                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    CurrentAnimation.Primitives[CurrentAnimation.CurrentFrame].Render(graphicsDevice);
                }
                effect.Parameters["xWorld"].SetValue(Matrix.Identity);
            }


            if(r != null)
            {
                graphicsDevice.RasterizerState = r;
            }
        }
    }

}