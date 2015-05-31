// BatchedSprite.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
    public class BatchedSprite : Sprite
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

        public BatchedSprite()
        {
            
        }

        public BatchedSprite(ComponentManager manager,
            string name,
            GameComponent parent,
            Matrix localTransform,
            SpriteSheet spriteSheet,
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
            lock (Primitive.VertexBuffer)
            {
                if (Primitive != null && Primitive.VertexBuffer != null)
                {
                    Primitive.VertexBuffer.Dispose();
                }
            }
            Primitive = new BatchBillboardPrimitive(graphicsDevice, SpriteSheet.GetTexture(), Width, Height, Frame, 1.0f, 1.0f, false, LocalTransforms, Tints);
        }

        public void RemoveTransform(int index)
        {
            if(index >= 0 && index < LocalTransforms.Count)
            {
                LocalTransforms.RemoveAt(index);
                Rotations.RemoveAt(index);
                Tints.RemoveAt(index);
            }
            Primitive = new BatchBillboardPrimitive(graphicsDevice, SpriteSheet.GetTexture(), Width, Height, Frame, 1.0f, 1.0f, false, LocalTransforms, Tints);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
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

        public override void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Effect effect, bool renderingForWater)
        {
            if(Primitive == null)
            {
                Primitive = new BatchBillboardPrimitive(graphicsDevice, SpriteSheet.GetTexture(), Width, Height, Frame, 1.0f, 1.0f, false, LocalTransforms, Tints);
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
                effect.Parameters["xTexture"].SetValue(SpriteSheet.GetTexture());

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