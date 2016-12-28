// SpriteDrawCommand.cs
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
    ///     Draws a sprite to the screen.
    /// </summary>
    public class SpriteDrawCommand : DrawCommand2D
    {
        public SpriteDrawCommand()
        {
            Scale = new Vector2(1, 1);
            Tint = Color.White;
            Rotation = 0.0f;
            Effects = SpriteEffects.None;
            Offset = Vector2.Zero;
        }

        public SpriteDrawCommand(Vector3 worldPosition, ImageFrame image)
        {
            WorldPosition = worldPosition;
            Image = image;
            Tint = Color.White;
        }

        public Vector3 WorldPosition { get; set; }

        public ImageFrame Image { get; set; }

        public Color Tint { get; set; }

        public Vector2 Scale { get; set; }

        public float Rotation { get; set; }

        public SpriteEffects Effects { get; set; }

        public Vector2 Offset { get; set; }


        public override void Render(SpriteBatch batch, Camera camera, Viewport viewport)
        {
            if (camera == null)
            {
                return;
            }

            Vector3 screenCoord = viewport.Project(WorldPosition, camera.ProjectionMatrix, camera.ViewMatrix,
                Matrix.Identity);
            if (viewport.Bounds.Contains((int) screenCoord.X, (int) screenCoord.Y) && screenCoord.Z < 0.999f &&
                Image != null)
            {
                batch.Draw(Image.Image, new Vector2(screenCoord.X, screenCoord.Y) + Offset, Image.SourceRect, Tint,
                    Rotation, new Vector2(Image.SourceRect.Width/2.0f, Image.SourceRect.Height/2.0f), Scale, Effects,
                    0.0f);
            }
        }
    }
}