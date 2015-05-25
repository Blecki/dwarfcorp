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


            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X + 1, unprojected.Y) - extents / 2.0f, Vector2.Zero);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X - 1, unprojected.Y) - extents / 2.0f, Vector2.Zero);

            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X, unprojected.Y + 1) - extents / 2.0f, Vector2.Zero);
            Drawer2D.SafeDraw(batch, Text, Font, StrokeColor, new Vector2(unprojected.X, unprojected.Y - 1) - extents / 2.0f, Vector2.Zero);

            Drawer2D.SafeDraw(batch, Text, Font, TextColor, new Vector2(unprojected.X, unprojected.Y) - extents / 2.0f, Vector2.Zero);
        }
    }

}