using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    /// <summary>
    /// This is a convenience class for drawing lines, sprites, boxes, etc. to the screen from
    /// threads other than the main drawing thread.
    /// </summary>
    public class Drawer2D
    {
        public static ConcurrentQueue<DrawCommand2D> DrawCommands = new ConcurrentQueue<DrawCommand2D>();
        public static ContentManager Content { get; set; }
        public static SpriteFont DefaultFont { get; set; }
        public static Texture2D Pixel { get; set; }

        public Drawer2D(ContentManager content, GraphicsDevice graphics)
        {
            Content = content;
            DefaultFont = content.Load<SpriteFont>(Program.CreatePath("Fonts", "Default"));
            Pixel = new Texture2D(graphics, 1, 1);
            Color[] white = new Color[1];
            white[0] = Color.White;
            Pixel = new Texture2D(graphics, 1, 1);
            Pixel.SetData<Color>(white);
        }

        public static void DrawSprite(ImageFrame image, Vector3 worldPosition)
        {
            DrawCommands.Enqueue(new SpriteDrawCommand(worldPosition, image));
        }

        public static void DrawSprite(ImageFrame image, Vector3 worldPosition, Vector2 scale, Vector2 offset, Color tint)
        {
            DrawCommands.Enqueue(new SpriteDrawCommand(worldPosition, image) { Scale = scale, Offset =  offset, Tint = tint});
        }

        public static void DrawTextBox(string text, Vector3 position)
        {
            DrawCommands.Enqueue(new TextBoxDrawCommand(text, DefaultFont, position, Color.White, new Color(0, 0, 0, 200), new Color(50, 50, 50, 100), new Color(0, 0, 0, 200), 2.0f));
        }

        public static void DrawRect(Vector3 worldPos, Rectangle screenRect, Color backgroundColor, Color strokeColor, float strokewidth)
        {
            
            Vector3 screenPos = GameState.Game.GraphicsDevice.Viewport.Project(worldPos, PlayState.Camera.ProjectionMatrix, PlayState.Camera.ViewMatrix, Matrix.Identity);

            Rectangle rect = new Rectangle((int)(screenPos.X - screenRect.Width/2), (int)(screenPos.Y - screenRect.Height/2), screenRect.Width, screenRect.Height);
            
            DrawCommands.Enqueue(new RectDrawCommand(backgroundColor, strokeColor, strokewidth, rect));
        }

        public static void DrawPolygon(List<Vector2> points, Color color, int width, bool closed)
        {
            DrawCommands.Enqueue(new PolygonDrawCommand(points, closed, color, width));
        }

        public static void DrawPolygon(List<Vector3> points, Color color, int width, bool closed)
        {
            DrawCommands.Enqueue(new PolygonDrawCommand(points, closed, color, width));
        }

        public static void DrawZAlignedRect(Vector3 center, float xWidth, float zHeight, int width, Color color)
        {
            List<Vector3> points = new List<Vector3>
            {
                center + new Vector3(-xWidth, 0, -zHeight),
                center + new Vector3(-xWidth, 0, zHeight),
                center + new Vector3(xWidth, 0, zHeight),
                center + new Vector3(xWidth, 0, -zHeight)
            };

            DrawPolygon(points, color, width, true);

        } 

        public static void DrawRect(Rectangle rect, Color backgroundColor, Color strokeColor, float strokewidth)
        {
            DrawCommands.Enqueue(new RectDrawCommand(backgroundColor, strokeColor, strokewidth, rect));
        }

        public static void DrawText(string text, Vector3 position, Color color, Color strokeColor)
        {
            DrawCommands.Enqueue(new TextDrawCommand(text, DefaultFont, position, color, strokeColor));
        }

        public void Render(SpriteBatch batch, Camera camera, Viewport viewport)
        {
            foreach(DrawCommand2D draw in DrawCommands)
            {
                draw.Render(batch, camera, viewport);
            }

            while(DrawCommands.Count > 0)
            {
                DrawCommand2D draw = null;
                DrawCommands.TryDequeue(out draw);
            }
        }

        /// <summary>
        /// Streches a circle to create the desired ellipse.
        /// </summary>
        /// <param Name="batch">The sprite batch to use.</param>
        /// <param Name="circle">A texture to use for a ellipse.</param>
        /// <param Name="center">The center of the ellipse.</param>
        /// <param Name="a">The major axis of the ellipse.</param>
        /// <param Name="b">The minor axis of the ellipse</param>
        /// <param Name="phi">The angle of the major axis with the horizontal</param>
        /// <param Name="color">The color of the ellipse</param>
        public static void DrawEllipse(SpriteBatch batch, Texture2D circle, Vector2 center, float a, float b, float phi, Color color)
        {
            batch.Draw(circle, 100 * center - new Vector2(100 * a, 100 * b), null, color, phi, Vector2.Zero, new Vector2(200 * a / 256, 200 * b / 256), SpriteEffects.None, 0);
        }

        /// <summary>
        /// Fills a solid rectangle.
        /// </summary>
        /// <param Name="batch">The sprite batch to draw with.</param>
        /// <param Name="pixel">Pixel image to use.</param>
        /// <param Name="rect">The rectangle to fill.</param>
        /// <param Name="backgroundColor">The color of the rectangle.</param>
        public static void FillRect(SpriteBatch batch, Rectangle rect, Color backgroundColor)
        {
            batch.Draw(Pixel, rect, backgroundColor);
        }

        /// <summary>
        /// Draws the given rectangle's border with a width of 2 pixels
        /// </summary>
        /// <param Name="batch">The sprite batch to use</param>
        /// <param Name="rect">The rectangle to draw.</param>
        /// <param Name="borderColor">The color of the border of the rectangle.</param>
        public static void DrawRect(SpriteBatch batch, Rectangle rect, Color borderColor, float width)
        {
            batch.Draw(Pixel, new Rectangle((int) (rect.Left - width), (int) (rect.Top - width), (int) width, (int) (rect.Height + width)), borderColor);
            batch.Draw(Pixel, new Rectangle((int) (rect.Left - width), (int) (rect.Top - width), rect.Width, (int) width), borderColor);
            batch.Draw(Pixel, new Rectangle((int) (rect.Left - width), (int) (rect.Top + rect.Height), rect.Width, (int) width), borderColor);
            batch.Draw(Pixel, new Rectangle((int) (rect.Left + rect.Width - width), (int) (rect.Top - width), (int) width, (int) (rect.Height + width)), borderColor);
        }

        /// <summary>
        /// Draws a line between two points of the given color and width by scaling and rotating a pixel
        /// </summary>
        /// <param Name="batch">The sprite batch to use.</param>
        /// <param Name="pixel">The pixel image to use.</param>
        /// <param Name="point1">The first point of the line.</param>
        /// <param Name="point2">The second point of the line.</param>
        /// <param Name="lineColor">The color of the line.</param>
        /// <param Name="width">The width, in pixels, of the line.</param>
        public static void DrawLine(SpriteBatch batch, Vector2 point1, Vector2 point2, Color lineColor, int width)
        {
            float distance = Vector2.Distance(point1, point2);
            Vector2 normal = (point2 - point1);
            normal.Normalize();

            batch.Draw(Pixel, new Rectangle((int) (point1.X), (int) (point1.Y) - width / 2, (int) distance, width), new Rectangle(0, 0, 1, 1), lineColor, MathFunctions.RectangularToPolar(normal).Y, Vector2.Zero, SpriteEffects.None, 0.0f);
            batch.Draw(Pixel, point1, null, lineColor, MathFunctions.RectangularToPolar(normal).Y, Vector2.Zero, new Vector2(distance, width), SpriteEffects.None, 0);
        }


        /// <summary>
        /// Draws a polygon of lines.
        /// </summary>
        /// <param Name="spriteBatch">The sprite batch.</param>
        /// <param Name="pixel">The pixel image to use.</param>
        /// <param Name="lineColor">Color of the lines.</param>
        /// <param Name="width">The width of the lines.</param>
        /// <param Name="points">The points to connect.</param>
        public static void DrawPolygon(SpriteBatch spriteBatch, Color lineColor, int width, List<Vector2> points)
        {
            for(int i = 0; i < points.Count() - 1; i++)
            {
                DrawLine(spriteBatch, points[i], points[i + 1], lineColor, width);
            }
            DrawLine(spriteBatch, points[0], points[points.Count() - 1], lineColor, width);
        }


        /// <summary>
        /// Uses an expensive hack to draw text with a stroke of 1 pixel around it. Just draws the text five times
        /// </summary>
        /// <param Name="batch">The sprite batch.</param>
        /// <param Name="toDisplay">The text to display.</param>
        /// <param Name="Font">The font to use.</param>
        /// <param Name="textPosition">The text position.</param>
        /// <param Name="textColor">Color of the text.</param>
        /// <param Name="strokeColor">Color of the stroke outside the text.</param>
        public static void DrawStrokedText(SpriteBatch batch, string toDisplay, SpriteFont Font, Vector2 textPosition, Color textColor, Color strokeColor)
        {
            if(toDisplay == null)
            {
                toDisplay = "null";
            }

            SafeDraw(batch, toDisplay, Font, strokeColor, textPosition - new Vector2(1, 0), Vector2.Zero);
            SafeDraw(batch, toDisplay, Font, strokeColor, textPosition + new Vector2(1, 0), Vector2.Zero);
            SafeDraw(batch, toDisplay, Font, strokeColor, textPosition - new Vector2(0, 1), Vector2.Zero);
            SafeDraw(batch, toDisplay, Font, strokeColor, textPosition + new Vector2(0, 1), Vector2.Zero);
            SafeDraw(batch, toDisplay, Font, textColor, textPosition, Vector2.Zero);
        }

        [Flags]
        public enum Alignment
        {
            Center = 0,
            Left = 1,
            Right = 2,
            Top = 4,
            Bottom = 8
        }

        public static Rectangle Align(Rectangle bounds, int width, int height, Alignment align)
        {
            Vector2 size = new Vector2(width, height);

            Vector2 pos = new Vector2(bounds.X + bounds.Width / 2 - width/2, bounds.Y + bounds.Height / 2 - height);
            Vector2 origin = size * 0.5f;

            if (align.HasFlag(Alignment.Left))
            {
                origin.X -= bounds.Width / 2;
            }

            if (align.HasFlag(Alignment.Right))
            {
                origin.X += bounds.Width / 2;
            }

            if (align.HasFlag(Alignment.Top))
            {
                origin.Y += bounds.Height / 2;
            }

            if (align.HasFlag(Alignment.Bottom))
            {
                origin.Y -= bounds.Height / 2;
            }

            Vector2 corner = origin + pos;

            return new Rectangle((int)corner.X, (int)corner.Y, width, height);

        }

        public static void DrawAlignedStrokedText(SpriteBatch batch, string text, SpriteFont font, Color textColor, Color strokeColor, Alignment align, Rectangle bounds)
        {
            Vector2 size = Datastructures.SafeMeasure(font, text);

            Vector2 pos = new Vector2(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            Vector2 origin = size * 0.5f;

            if(align.HasFlag(Alignment.Left))
            {
                origin.X += bounds.Width / 2 - size.X / 2;
            }

            if(align.HasFlag(Alignment.Right))
            {
                origin.X -= bounds.Width / 2 - size.X / 2;
            }

            if(align.HasFlag(Alignment.Top))
            {
                origin.Y += bounds.Height / 2 - size.Y / 2;
            }

            if(align.HasFlag(Alignment.Bottom))
            {
                origin.Y -= bounds.Height / 2 - size.Y / 2;
            }

            SafeDraw(batch, text, font, strokeColor, pos - new Vector2(1, 0), origin);
            SafeDraw(batch, text, font, strokeColor, pos + new Vector2(1, 0), origin);
            SafeDraw(batch, text, font, strokeColor, pos - new Vector2(0, 1), origin);
            SafeDraw(batch, text, font, strokeColor, pos - new Vector2(0, 1), origin);
            SafeDraw(batch, text, font, textColor, pos, origin);
        }

        public static void DrawAlignedText(SpriteBatch batch, string text, SpriteFont font, Color textColor, Alignment align, Rectangle bounds)
        {
            Vector2 size = Datastructures.SafeMeasure(font, text);

            Vector2 pos = new Vector2(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            Vector2 origin = size * 0.5f;

            if(align.HasFlag(Alignment.Left))
            {
                origin.X += bounds.Width / 2 - size.X / 2;
            }

            if(align.HasFlag(Alignment.Right))
            {
                origin.X -= bounds.Width / 2 - size.X / 2;
            }

            if(align.HasFlag(Alignment.Top))
            {
                origin.Y += bounds.Height / 2 - size.Y / 2;
            }

            if(align.HasFlag(Alignment.Bottom))
            {
                origin.Y -= bounds.Height / 2 - size.Y / 2;
            }

            SafeDraw(batch, text, font, textColor, pos, origin);
        }


        public static char[] EscapeChars =
        {
            '\n',
            '\t',
            '\b'
        };

        public static string Internationalize(string text, SpriteFont font)
        {
            if(text == null)
            {
                return "null";
            }

            char[] arr = text.ToCharArray();
            for(int i = 0; i < text.Length; i++)
            {
                char c = arr[i];
                if(!EscapeChars.Contains(c) && !font.Characters.Contains(c))
                {
                    arr[i] = '?';
                }
            }

            return new string(arr);
        }

        public static void SafeDraw(SpriteBatch batch, string text, SpriteFont font, Color textColor, Vector2 pos, Vector2 origin)
        {
            batch.DrawString(font, Internationalize(text, font), pos, textColor, 0, origin, 1, SpriteEffects.None, 0);
        }
    }

}