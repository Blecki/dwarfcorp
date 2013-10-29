using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{



    public class BillboardSpriteComponent : TintableComponent
    {
        public Dictionary<string, Animation> Animations { get; set; }

        [JsonIgnore]
        public Texture2D SpriteSheet { get; set; }
        
        [JsonIgnore]
        public Animation CurrentAnimation { get; set; }

        [JsonIgnore]
        private static Matrix InvertY =  Matrix.CreateScale(1, -1, 1);
        
        public OrientMode OrientationType { get; set; }


        public enum OrientMode { Fixed, Spherical, XAxis, YAxis, ZAxis }
        
        public float BillboardRotation { get; set; }

        private static RasterizerState rasterState = new RasterizerState()
        {
            CullMode = CullMode.None,
        };

        public BillboardSpriteComponent(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Texture2D spriteSheet, bool addToOctree) :
            base(manager, name, parent, localTransform, Vector3.Zero, Vector3.Zero, addToOctree)
        {
            SpriteSheet = spriteSheet;
            Animations = new Dictionary<string, Animation> ();
            OrientationType = OrientMode.Spherical;
            BillboardRotation = 0.0f;
        }

        public void AddAnimation(Animation animation)
        {
            if (CurrentAnimation == null)
            {
                CurrentAnimation = animation;
            }
            Animations[animation.Name] = animation;
        }

        public Animation GetAnimation(string name)
        {
            if (Animations.ContainsKey(name))
            {
                return Animations[name];
            }

            return null;
        }

        public virtual void SetCurrentAnimation(string name)
        {
            Animation anim = GetAnimation(name);

            if (anim != null)
            {
                CurrentAnimation = anim;
            }
        }


        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch (messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    HasMoved = true;
                    break;
            }


            base.ReceiveMessageRecursive(messageToReceive);
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (IsActive)
            {
                if (CurrentAnimation != null)
                {
                    CurrentAnimation.Update(gameTime);
                }

            }


            base.Update(gameTime, chunks, camera);
        }

        public override void Render(GameTime gameTime,
                                    ChunkManager chunks,
                                    Camera camera,
                                    SpriteBatch spriteBatch,
                                    GraphicsDevice graphicsDevice,
                                    Effect effect,
                                    bool renderingForWater)
        {

            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            
            if (IsVisible)
            {



                RasterizerState r = graphicsDevice.RasterizerState;
                graphicsDevice.RasterizerState = rasterState;
                effect.Parameters["xTexture"].SetValue(SpriteSheet);



                if (CurrentAnimation != null)
                {
                    //Matrix oldWorld = effect.Parameters["xWorld"].GetValueMatrix();

                    if (OrientationType != OrientMode.Fixed)
                    {
                        if (camera.Projection == Camera.ProjectionMode.Perspective)
                        {
                            if (OrientationType == OrientMode.Spherical)
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

                                switch (OrientationType)
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
                            Matrix rotation = Matrix.CreateRotationY(-(float)Math.PI * 0.25f) * Matrix.CreateTranslation(GlobalTransform.Translation);
                            effect.Parameters["xWorld"].SetValue(rotation);
                        }


                    }
                    else
                    {
                        effect.Parameters["xWorld"].SetValue(GlobalTransform);
                    }

                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        CurrentAnimation.Primitives[CurrentAnimation.CurrentFrame].Render(graphicsDevice);
                    }
                    effect.Parameters["xWorld"].SetValue(Matrix.Identity);
                }


                if (r != null)
                {
                    graphicsDevice.RasterizerState = r;
                }
            }
        }



    }
}
