// Box.cs
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     This component draws a simple textured rectangular box.
    /// </summary>
    public class Box : Tinter
    {
        /// <summary>
        ///     Create a new box.
        /// </summary>
        /// <param name="name">Name of the box for debugging</param>
        /// <param name="parent">Parent component of the box.</param>
        /// <param name="localTransform">Transform of the box w.r.t its parent.</param>
        /// <param name="boundingBoxExtents">Size of the bounding box (in voxels)</param>
        /// <param name="boundingBoxPos">Relative position of the center of the bounding box.</param>
        /// <param name="primitive">Box model asset tag.</param>
        /// <param name="tex">Texture to use for the box.</param>
        public Box(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents,
            Vector3 boundingBoxPos, string primitive, Texture2D tex) :
                base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos, false)
        {
            Primitive = primitive;
            Texture = tex;
        }

        /// <summary>
        ///     Box model asset tag from the BoxPrimitiveLibrary to draw.
        /// </summary>
        public string Primitive { get; set; }

        /// <summary>
        ///     Texture associated with the box.
        /// </summary>
        public Texture2D Texture { get; set; }

        public override void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            effect.Parameters["xTexture"].SetValue(Texture);
            effect.Parameters["xWorld"].SetValue(GlobalTransform);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                PrimitiveLibrary.BoxPrimitives[Primitive].Render(graphicsDevice);
            }
        }
    }
}