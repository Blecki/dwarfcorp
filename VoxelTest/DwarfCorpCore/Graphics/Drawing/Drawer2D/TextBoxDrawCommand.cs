using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Draws a box with text inside to the screen.
    /// </summary>
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

            if (unprojected.Z < 0)
            {
                return;
            }

            Rectangle rect = new Rectangle((int) (unprojected.X - extents.X / 2.0f - StrokeWeight), (int) (unprojected.Y - extents.Y / 2.0f - StrokeWeight),
                (int) (extents.X + StrokeWeight + 5), (int) (extents.Y + StrokeWeight + 5));

            Drawer2D.FillRect(batch, rect, FillColor);
            Drawer2D.DrawRect(batch, rect, RectStrokeColor, StrokeWeight);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X + 1, unprojected.Y) - extents / 2.0f, Vector2.Zero);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X - 1, unprojected.Y) - extents / 2.0f, Vector2.Zero);

            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X, unprojected.Y + 1) - extents / 2.0f, Vector2.Zero);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X, unprojected.Y - 1) - extents / 2.0f, Vector2.Zero);

            Drawer2D.SafeDraw(batch, Text, Font, TextColor, new Vector2(unprojected.X, unprojected.Y) - extents / 2.0f, Vector2.Zero);

        }
    }

}