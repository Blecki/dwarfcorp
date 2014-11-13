using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Draw commands are queued up to draw simple 3D objects to the screen easily
    /// from multiple threads
    /// </summary>
    public abstract class DrawCommand3D
    {
        public struct LineStrip
        {
            public VertexPositionColor[] Vertices;
            public int NumTriangles;
        }

        public Color ColorToDraw = Color.White;

        protected DrawCommand3D(Color color)
        {
            ColorToDraw = color;
        }

        public abstract void Render(GraphicsDevice device, Effect effect);

        public abstract void AccumulateStrips(List<LineStrip> vertices);
    }

}