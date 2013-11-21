using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;

namespace DwarfCorp
{

    public class DrawCommand2D
    {
        public virtual void Render(SpriteBatch batch, Camera camera, Viewport viewport)
        {
        }
    }

    public class TextBoxDrawCommand : DrawCommand2D
    {
        public SpriteFont Font { get; set; }
        public string Text { get; set; }
        public Vector3 Position { get; set; }
        public Color TextColor { get; set; }
        public Color StrokeColor { get; set; }
        public Color RectStrokeColor { get; set; }
        public Color FillColor { get; set; }
        public float StrokeWeight { get; set; }

        public TextBoxDrawCommand(string text, SpriteFont font, Vector3 position, Color color, Color strokeColor, Color rectStroke, Color fillColor, float strokeWeight)
        {
            Font = font;
            Text = text;
            Position = position;
            TextColor = color;
            StrokeColor = strokeColor;
            FillColor = fillColor;
            StrokeWeight = strokeWeight;
            RectStrokeColor = rectStroke;
        }

        public override void Render(SpriteBatch batch, Camera camera, Viewport viewport)
        {
            if(batch == null || camera == null)
            {
                return;
            }

            Vector2 extents = Datastructures.SafeMeasure(Font, Text);


            Vector3 unprojected = viewport.Project(Position, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

            Rectangle rect = new Rectangle((int) (unprojected.X - extents.X / 2.0f - StrokeWeight), (int) (unprojected.Y - extents.Y / 2.0f - StrokeWeight),
                (int) (extents.X + StrokeWeight + 5), (int) (extents.Y + StrokeWeight + 5));

            Drawer2D.FillRect(batch, rect, FillColor);
            Drawer2D.DrawRect(batch, rect, RectStrokeColor, StrokeWeight);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X + 1, unprojected.Y) - extents / 2.0f, Vector2.Zero);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X - 1, unprojected.Y) - extents / 2.0f, Vector2.Zero);

            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X, unprojected.Y + 1) - extents / 2.0f, Vector2.Zero);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X, unprojected.Y - 1) - extents / 2.0f, Vector2.Zero);

            Drawer2D.SafeDraw(batch, Text, Font, TextColor, new Vector2(unprojected.X, unprojected.Y) - extents / 2.0f, Vector2.Zero);

            base.Render(batch, camera, viewport);
        }
    }

    public class RectDrawCommand : DrawCommand2D
    {
        public Color FillColor { get; set; }
        public Color StrokeColor { get; set; }
        public float StrokeWeight { get; set; }
        public Rectangle Bounds { get; set; }

        public RectDrawCommand(Color fill, Color stroke, float strokeWeight, Rectangle bounds)
        {
            Bounds = bounds;
            FillColor = fill;
            StrokeColor = stroke;
            StrokeWeight = strokeWeight;
        }


        public override void Render(SpriteBatch batch, Camera camera, Viewport viewport)
        {
            Drawer2D.FillRect(batch, Bounds, FillColor);
            Drawer2D.DrawRect(batch, Bounds, StrokeColor, StrokeWeight);
            base.Render(batch, camera, viewport);
        }
    }

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
            Vector2 extents = Datastructures.SafeMeasure(Font, Text);

            Vector3 unprojected = viewport.Project(Position, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);


            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X + 1, unprojected.Y) - extents / 2.0f, Vector2.Zero);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X - 1, unprojected.Y) - extents / 2.0f, Vector2.Zero);

            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X, unprojected.Y + 1) - extents / 2.0f, Vector2.Zero);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X, unprojected.Y - 1) - extents / 2.0f, Vector2.Zero);

            Drawer2D.SafeDraw(batch, Text, Font, TextColor, new Vector2(unprojected.X, unprojected.Y) - extents / 2.0f, Vector2.Zero);

            base.Render(batch, camera, viewport);
        }
    }

    public class Drawer2D
    {
        public static ConcurrentQueue<DrawCommand2D> DrawCommands = new ConcurrentQueue<DrawCommand2D>();
        public static ContentManager Content { get; set; }
        public static SpriteFont DefaultFont { get; set; }
        public static Texture2D Pixel { get; set; }

        public Drawer2D(ContentManager content, GraphicsDevice graphics)
        {
            Content = content;
            DefaultFont = content.Load<SpriteFont>("Default");
            Pixel = new Texture2D(graphics, 1, 1);
            Color[] white = new Color[1];
            white[0] = Color.White;
            Pixel = new Texture2D(graphics, 1, 1);
            Pixel.SetData<Color>(white);
        }

        public static void DrawTextBox(string text, Vector3 position)
        {
            DrawCommands.Enqueue(new TextBoxDrawCommand(text, DefaultFont, position, Color.White, new Color(0, 0, 0, 200), new Color(50, 50, 50, 100), new Color(0, 0, 0, 200), 2.0f));
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
        /// <param name="batch">The sprite batch to use.</param>
        /// <param name="circle">A texture to use for a ellipse.</param>
        /// <param name="center">The center of the ellipse.</param>
        /// <param name="a">The major axis of the ellipse.</param>
        /// <param name="b">The minor axis of the ellipse</param>
        /// <param name="phi">The angle of the major axis with the horizontal</param>
        /// <param name="color">The color of the ellipse</param>
        public static void DrawEllipse(SpriteBatch batch, Texture2D circle, Vector2 center, float a, float b, float phi, Color color)
        {
            batch.Draw(circle, 100 * center - new Vector2(100 * a, 100 * b), null, color, phi, Vector2.Zero, new Vector2(200 * a / 256, 200 * b / 256), SpriteEffects.None, 0);
        }

        /// <summary>
        /// Fills a solid rectangle.
        /// </summary>
        /// <param name="batch">The sprite batch to draw with.</param>
        /// <param name="pixel">Pixel image to use.</param>
        /// <param name="rect">The rectangle to fill.</param>
        /// <param name="backgroundColor">The color of the rectangle.</param>
        public static void FillRect(SpriteBatch batch, Rectangle rect, Color backgroundColor)
        {
            batch.Draw(Pixel, rect, backgroundColor);
        }

        /// <summary>
        /// Draws the given rectangle's border with a width of 2 pixels
        /// </summary>
        /// <param name="batch">The sprite batch to use</param>
        /// <param name="rect">The rectangle to draw.</param>
        /// <param name="borderColor">The color of the border of the rectangle.</param>
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
        /// <param name="batch">The sprite batch to use.</param>
        /// <param name="pixel">The pixel image to use.</param>
        /// <param name="point1">The first point of the line.</param>
        /// <param name="point2">The second point of the line.</param>
        /// <param name="lineColor">The color of the line.</param>
        /// <param name="width">The width, in pixels, of the line.</param>
        public static void DrawLine(SpriteBatch batch, Vector2 point1, Vector2 point2, Color lineColor, int width)
        {
            float distance = Vector2.Distance(point1, point2);
            Vector2 normal = (point2 - point1);
            normal.Normalize();

            batch.Draw(Pixel, new Rectangle((int) (point1.X), (int) (point1.Y) - width / 2, (int) distance, width), new Rectangle(0, 0, 1, 1), lineColor, LinearMathHelpers.RectangularToPolar(normal).Y, Vector2.Zero, SpriteEffects.None, 0.0f);
            batch.Draw(Pixel, point1, null, lineColor, LinearMathHelpers.RectangularToPolar(normal).Y, Vector2.Zero, new Vector2(distance, width), SpriteEffects.None, 0);
        }


        /// <summary>
        /// Draws a polygon of lines.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="pixel">The pixel image to use.</param>
        /// <param name="lineColor">Color of the lines.</param>
        /// <param name="width">The width of the lines.</param>
        /// <param name="points">The points to connect.</param>
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
        /// <param name="batch">The sprite batch.</param>
        /// <param name="toDisplay">The text to display.</param>
        /// <param name="Font">The font to use.</param>
        /// <param name="textPosition">The text position.</param>
        /// <param name="textColor">Color of the text.</param>
        /// <param name="strokeColor">Color of the stroke outside the text.</param>
        public static void DrawStrokedText(SpriteBatch batch, string toDisplay, SpriteFont Font, Vector2 textPosition, Color textColor, Color strokeColor)
        {
            if(toDisplay == null)
            {
                toDisplay = "null";
            }

            Drawer2D.SafeDraw(batch, toDisplay, Font, strokeColor, textPosition - new Vector2(1, 0), Vector2.Zero);
            Drawer2D.SafeDraw(batch, toDisplay, Font, strokeColor, textPosition + new Vector2(1, 0), Vector2.Zero);
            Drawer2D.SafeDraw(batch, toDisplay, Font, strokeColor, textPosition - new Vector2(0, 1), Vector2.Zero);
            Drawer2D.SafeDraw(batch, toDisplay, Font, strokeColor, textPosition + new Vector2(0, 1), Vector2.Zero);
            Drawer2D.SafeDraw(batch, toDisplay, Font, textColor, textPosition, Vector2.Zero);
        }

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


        public static char[] escapeChars =
        {
            '\n',
            '\t',
            '\b'
        };

        public static string Internationalize(string text, SpriteFont font)
        {
            char[] arr = text.ToCharArray();
            for(int i = 0; i < text.Length; i++)
            {
                char c = arr[i];
                if(!escapeChars.Contains(c) && !font.Characters.Contains(c))
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