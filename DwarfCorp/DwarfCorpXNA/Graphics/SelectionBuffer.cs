
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Manages a selection buffer and allows selection from the screen
    /// using rendered pixels.
    /// </summary>
    public class SelectionBuffer
    {
        private RenderTarget2D Buffer;
        private Color[] colorBuffer;
        private int Scale = 4;
        private Timer renderTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
        private bool renderThisFrame = false;
        public SelectionBuffer(int scale, GraphicsDevice device)
        {
            Scale = scale;
        }

        public void ValidateBuffer(GraphicsDevice device)
        {
            PresentationParameters pp = device.PresentationParameters;

            int width = pp.BackBufferWidth / Scale;
            int height = pp.BackBufferHeight / Scale;
            if (Buffer == null || Buffer.Width != width ||
                Buffer.Height != height)
            {
                Buffer = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
            }
        }

        public bool Begin(GraphicsDevice device)
        {
            renderTimer.Update(DwarfTime.LastTime);
            renderThisFrame = renderTimer.HasTriggered || colorBuffer == null;
            if (!renderThisFrame)
            {
                return false;
            }
            ValidateBuffer(device);
            device.SetRenderTarget(Buffer);
            device.Clear(Color.Transparent);
            return true;
        }

        public void End(GraphicsDevice device)
        {
            device.SetRenderTarget(null);
            if (colorBuffer == null)
            {
                colorBuffer = new Color[(device.Viewport.Width / Scale) * (device.Viewport.Height / Scale)];
            }
            Buffer.GetData(colorBuffer);
        }

        /// <summary>
        /// Gets a unique set of identifiers that were selected on the screen.
        /// </summary>
        /// <param name="screenRectangle">The screen rectangle to select from.</param>
        /// <returns></returns>
        public IEnumerable<uint> GetIDsSelected(Rectangle screenRectangle)
        {
            int width = Buffer.Width;
            int height = Buffer.Height;

            int startX = MathFunctions.Clamp(screenRectangle.X/Scale, 0, width - 1);
            int startY = MathFunctions.Clamp(screenRectangle.Y/Scale, 0, height - 1);
            int endX = MathFunctions.Clamp(screenRectangle.Right/Scale, 0, width - 1);
            int endY = MathFunctions.Clamp(screenRectangle.Bottom/Scale, 0, height - 1);
            HashSet<uint> selected = new HashSet<uint>();
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    uint id = GameComponent.GlobalIDFromColor(colorBuffer[x + y*width]);
                    if (id == 0) continue;
                    if (selected.Contains(id))
                    {
                        continue;
                    }
                    selected.Add(id);
                    yield return id;
                }
            }
        }

        public void DebugDraw(Rectangle rect)
        {
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
            DwarfGame.SpriteBatch.Draw((Texture2D)Buffer, rect, Color.White);
            DwarfGame.SpriteBatch.End();
        }
    }
}
