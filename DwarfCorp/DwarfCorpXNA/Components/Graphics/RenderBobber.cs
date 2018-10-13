// Bobber.cs
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
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class SimpleBobber : SimpleSprite
    {
        public float Magnitude { get; set; }
        public float Rate { get; set; }
        public float Offset { get; set; }
        public float Bob;
        public Vector3 OriginalPos;
        public SimpleBobber(
            ComponentManager Manager,
            String Name,
            Matrix LocalTransform,
            SpriteSheet Sheet,
            Point Frame,
            float mag,
            float rate, 
            float offset)
            : base(Manager, Name, LocalTransform, Sheet, Frame)
        {
            Magnitude = mag;
            Rate = rate;
            Offset = offset;
            OriginalPos = LocalTransform.Translation;
            UpdateRate = 2;
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            float x = (float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + Offset) * Rate) * Magnitude;
            var transform = GlobalTransform;
            var originalOffset = transform.Translation;
            transform.Translation += new Vector3(OriginalPos.X, x, OriginalPos.Z);

            try
            {
                RawSetGlobalTransform(transform);
                base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            }
            finally
            {
                transform.Translation = originalOffset;
                RawSetGlobalTransform(transform);
            }
        }
    }

    public class LayeredBobber : LayeredSimpleSprite
    {
        public float Magnitude { get; set; }
        public float Rate { get; set; }
        public float Offset { get; set; }
        public float Bob;

        public LayeredBobber(
            ComponentManager Manager,
            String Name,
            Matrix LocalTransform,
            List<Layer> Layers,
            float mag,
            float rate,
            float offset)
            : base(Manager, Name, LocalTransform, Layers)
        {
            UpdateRate = 2;
            Magnitude = mag;
            Rate = rate;
            Offset = offset;
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            float x = (float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + Offset) * Rate) * Magnitude;
            var transform = GlobalTransform;
            var originalOffset = transform.Translation;
            transform.Translation += new Vector3(0, x, 0);

            try
            {
                RawSetGlobalTransform(transform);
                base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            }
            finally
            {
                transform.Translation = originalOffset;
                RawSetGlobalTransform(transform);
            }
        }
    }
}