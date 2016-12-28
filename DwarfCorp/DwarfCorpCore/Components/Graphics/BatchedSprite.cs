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

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Draw aggregate (hundreds or thousands) of 3D sprites using a batch billboard primitive. This is for
    ///     STATIC sprites. It is used to draw hundreds to thousands of little grass motes.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BatchedSprite : Sprite
    {
        /// <summary>
        ///     Don't cull billboards so they are visible from both sides.
        /// </summary>
        private static readonly RasterizerState rasterState = new RasterizerState
        {
            CullMode = CullMode.None,
        };

        /// <summary>
        ///     (x, y) position in the texture to draw the geometry from.
        /// </summary>
        private readonly Point Frame;

        /// <summary>
        ///     Height of the frame in pixels.
        /// </summary>
        private readonly int Height = 32;

        /// <summary>
        ///     Width of the frame in pixels.
        /// </summary>
        private readonly int Width = 32;

        /// <summary>
        ///     Do not attempt to draw geometry if it is further away than this amount (in squared voxels)
        /// </summary>
        public float CullDistance = 1000.0f;

        /// <summary>
        ///     Giant vertex buffer containing all the geometry.
        /// </summary>
        public BatchBillboardPrimitive Primitive;

        public BatchedSprite()
        {
        }

        /// <summary>
        ///     Create a batched sprite component with respect to the parent component.
        /// </summary>
        /// <param name="manager">Manager that owns this component.</param>
        /// <param name="name">Name of the batched sprite.</param>
        /// <param name="parent">Parent to attach the batched sprite to.</param>
        /// <param name="localTransform">Transform of the batched sprite w.r.t the parent.</param>
        /// <param name="spriteSheet">Sprites to use for the batched sprite.</param>
        /// <param name="numBillboards">Number of child sprites to draw.</param>
        public BatchedSprite(ComponentManager manager,
            string name,
            GameComponent parent,
            Matrix localTransform,
            SpriteSheet spriteSheet,
            int numBillboards) :
                base(manager, name, parent, localTransform, spriteSheet, false)
        {
            LocalTransforms = new List<Matrix>(numBillboards);
            Rotations = new List<float>(numBillboards);
            Tints = new List<Color>(numBillboards);
            Colors = new List<Color>(numBillboards);
            FrustrumCull = false;
            Width = spriteSheet.Width;
            Height = spriteSheet.Height;
            Frame = new Point(0, 0);
            LightsWithVoxels = false;
        }

        /// <summary>
        ///     List of transforms of all the child geometry.
        /// </summary>
        public List<Matrix> LocalTransforms { get; set; }

        /// <summary>
        ///     Rotations around the z axis of all the child geometry.
        /// </summary>
        public List<float> Rotations { get; set; }

        /// <summary>
        ///     Light tings of all the child geometry.
        /// </summary>
        public List<Color> Tints { get; set; }

        /// <summary>
        ///     Vertex colors of all the child geometry.
        /// </summary>
        public List<Color> Colors { get; set; }

        /// <summary>
        ///     Adds a new sprite to the batched sprite. Optionally rebuilds the geometry.
        /// </summary>
        /// <param name="transform">The relative transform of the child geometry.</param>
        /// <param name="rotation">The rotation around Z of the child geometry.</param>
        /// <param name="tint">The light tint of the child geometry.</param>
        /// <param name="color">The vertex color of the child geometry.</param>
        /// <param name="rebuild">If true, rebuilds the static vertex buffer.</param>
        public void AddTransform(Matrix transform, float rotation, Color tint, Color color, bool rebuild)
        {
            LocalTransforms.Add(transform*Matrix.Invert(GlobalTransform));
            Rotations.Add(rotation);
            Tints.Add(tint);
            Colors.Add(color);
            if (rebuild)
            {
                RebuildPrimitive();
            }
        }

        /// <summary>
        ///     Rebuild the static vertex buffer using new geometry. This should be called as infrequently as possible.
        /// </summary>
        public void RebuildPrimitive()
        {
            lock (Primitive.VertexBuffer)
            {
                if (Primitive != null && Primitive.VertexBuffer != null)
                {
                    Primitive.VertexBuffer.Dispose();
                }
            }
            Primitive = new BatchBillboardPrimitive(SpriteSheet.GetTexture(), Width, Height, Frame, 1.0f, 1.0f, false,
                LocalTransforms, Tints, Colors);
        }

        /// <summary>
        ///     Deletes a child sprite at the given index. Rebuilds the geometry.
        /// </summary>
        /// <param name="index">The index of the child sprite to remove.</param>
        public void RemoveTransform(int index)
        {
            if (index >= 0 && index < LocalTransforms.Count)
            {
                LocalTransforms.RemoveAt(index);
                Rotations.RemoveAt(index);
                Tints.RemoveAt(index);
                Primitive = new BatchBillboardPrimitive(SpriteSheet.GetTexture(), Width, Height, Frame, 1.0f, 1.0f,
                    false, LocalTransforms, Tints, Colors);
            }
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (LightsWithVoxels)
            {
                base.Update(gameTime, chunks, camera);
            }
        }

        /// <summary>
        ///     Use a simplified test to see if we should draw this sprite.
        /// </summary>
        /// <param name="camera">Camera drawing the sprite.</param>
        /// <returns>True if the sprite should be drawn.</returns>
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
            if (!IsVisible || !ShouldDraw(camera)) return;

            // If we still don't have a static vertex buffer, create one.
            if (Primitive == null)
            {
                Primitive = new BatchBillboardPrimitive(SpriteSheet.GetTexture(), Width, Height, Frame, 1.0f, 1.0f,
                    false, LocalTransforms, Tints, Colors);
            }

            // Either tint the entire batched primitive, or ignore the tint altogether.
            effect.Parameters["xTint"].SetValue(!LightsWithVoxels ? new Vector4(1, 1, 1, 1) : Tint.ToVector4());

            // Make sure to draw the backs of the primitives.
            RasterizerState r = graphicsDevice.RasterizerState;
            graphicsDevice.RasterizerState = rasterState;

            effect.Parameters["xTexture"].SetValue(SpriteSheet.GetTexture());

            effect.Parameters["xWorld"].SetValue(GlobalTransform);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }

            effect.Parameters["xWorld"].SetValue(Matrix.Identity);

            // Reset the graphics rasterizer state.
            if (r != null)
            {
                graphicsDevice.RasterizerState = r;
            }
        }
    }
}