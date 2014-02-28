using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This component represents a list of several billboards which are efficiently drawn through state batching.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BatchBillboard : BillboardSpriteComponent
    {
        public List<Matrix> LocalTransforms { get; set; }
        public List<float> Rotations { get; set; }
        public List<Color> Tints { get; set; }
        public float CullDistance = 1000.0f;
        public BatchBillboardPrimitive Primitive;
        private Point Frame;
        private int Width = 32;
        private int Height = 32;

        private static RasterizerState rasterState = new RasterizerState()
        {
            CullMode = CullMode.None,
        };

        private GraphicsDevice graphicsDevice;

        public BatchBillboard()
        {
            
        }

        public BatchBillboard(ComponentManager manager,
            string name,
            GameComponent parent,
            Matrix localTransform,
            Texture2D spriteSheet,
            int numBillboards, GraphicsDevice graphi) :
                base(manager, name, parent, localTransform, spriteSheet, false)
        {
            LocalTransforms = new List<Matrix>(numBillboards);
            Rotations = new List<float>(numBillboards);
            Tints = new List<Color>(numBillboards);
            FrustrumCull = false;
            Width = spriteSheet.Width;
            Height = spriteSheet.Height;
            Frame = new Point(0, 0);
            graphicsDevice = graphi;
            LightsWithVoxels = false;
        }

        public void AddTransform(Matrix transform, float rotation, Color tint, bool rebuild)
        {
            LocalTransforms.Add(transform * Matrix.Invert(GlobalTransform));
            Rotations.Add(rotation);
            Tints.Add(tint);
            if(rebuild)
            {
                RebuildPrimitive();
            }
        }

        public void RebuildPrimitive()
        {
            if(Primitive != null && Primitive.VertexBuffer != null)
            {
                Primitive.VertexBuffer.Dispose();
            }

            Primitive = new BatchBillboardPrimitive(graphicsDevice, SpriteSheet, Width, Height, Frame, 1.0f, 1.0f, false, LocalTransforms, Tints);
        }

        public void RemoveTransform(int index)
        {
            if(index >= 0 && index < LocalTransforms.Count)
            {
                LocalTransforms.RemoveAt(index);
                Rotations.RemoveAt(index);
                Tints.RemoveAt(index);
            }
            Primitive = new BatchBillboardPrimitive(graphicsDevice, SpriteSheet, Width, Height, Frame, 1.0f, 1.0f, false, LocalTransforms, Tints);
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if(LightsWithVoxels)
            {
                base.Update(gameTime, chunks, camera);
            }
        }

        public bool ShouldDraw(Camera camera)
        {
            Vector3 diff = (GlobalTransform.Translation - camera.Position);

            return (diff).LengthSquared() < CullDistance;
        }

        public override void Render(GameTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Effect effect, bool renderingForWater)
        {
            if(Primitive == null)
            {
                Primitive = new BatchBillboardPrimitive(graphicsDevice, SpriteSheet, Width, Height, Frame, 1.0f, 1.0f, false, LocalTransforms, Tints);
            }


            if(IsVisible && ShouldDraw(camera))
            {
                if(!LightsWithVoxels)
                {
                    effect.Parameters["xTint"].SetValue(new Vector4(1, 1, 1, 1));
                }
                else
                {
                    effect.Parameters["xTint"].SetValue(Tint.ToVector4());
                }

                RasterizerState r = graphicsDevice.RasterizerState;
                graphicsDevice.RasterizerState = rasterState;
                effect.Parameters["xTexture"].SetValue(SpriteSheet);

                DepthStencilState origDepthStencil = graphicsDevice.DepthStencilState;
                DepthStencilState newDepthStencil = DepthStencilState.DepthRead;
                graphicsDevice.DepthStencilState = newDepthStencil;


                //Matrix oldWorld = effect.Parameters["xWorld"].GetValueMatrix();
                effect.Parameters["xWorld"].SetValue(GlobalTransform);

                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Primitive.Render(graphicsDevice);
                }

                effect.Parameters["xWorld"].SetValue(Matrix.Identity);

                if(origDepthStencil != null)
                {
                    graphicsDevice.DepthStencilState = origDepthStencil;
                }

                if(r != null)
                {
                    graphicsDevice.RasterizerState = r;
                }
            }
        }
    }

}