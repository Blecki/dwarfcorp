using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Draws a rectangle to the screen.
    /// </summary>
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
        }
    }

}