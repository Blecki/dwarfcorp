// TextDrawCommand.cs
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
    /// Draws text to the screen
    /// </summary>
    public class TextDrawCommand : DrawCommand2D
    {
        public SpriteFont Font { get; set; }
        public string Text { get; set; }
        public Vector3 Position { get; set; }
        public Color TextColor { get; set; }
        public Color StrokeColor { get; set; }

        public TextDrawCommand(string text, SpriteFont font, Vector3 position, Color color, Color strokeColor)
        {
            Font = font;
            Text = text;
            Position = position;
            TextColor = color;
            StrokeColor = strokeColor;
        }

        public override void Render(SpriteBatch batch, Camera camera, Viewport viewport)
        {
            if (camera == null) return;
            Vector2 extents = Datastructures.SafeMeasure(Font, Text);

            Vector3 unprojected = viewport.Project(Position, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

            if (unprojected.Z < 0.999f && viewport.Bounds.Contains((int) unprojected.X, (int) unprojected.Y))
            {
                Drawer2D.SafeDraw(batch, Text, Font, StrokeColor,
                    new Vector2(unprojected.X + 1, unprojected.Y) - extents/2.0f, Vector2.Zero);
                Drawer2D.SafeDraw(batch, Text, Font, StrokeColor,
                    new Vector2(unprojected.X - 1, unprojected.Y) - extents/2.0f, Vector2.Zero);

                Drawer2D.SafeDraw(batch, Text, Font, StrokeColor,
                    new Vector2(unprojected.X, unprojected.Y + 1) - extents/2.0f, Vector2.Zero);
                Drawer2D.SafeDraw(batch, Text, Font, StrokeColor,
                    new Vector2(unprojected.X, unprojected.Y - 1) - extents/2.0f, Vector2.Zero);

                Drawer2D.SafeDraw(batch, Text, Font, TextColor, new Vector2(unprojected.X, unprojected.Y) - extents/2.0f,
                    Vector2.Zero);
            }
        }
    }

}