using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Draws a list of lines to the screen.
    /// </summary>
    internal class LineListCommand3D : DrawCommand3D
    {
        public Vector3[] Points;
        private readonly VertexPositionColor[] triangles;
        private readonly int triangleCount = 0;

        public LineListCommand3D(Vector3[] points, Color color, float thickness) :
            base(color)
        {
            this.Points = points;
            Matrix worldMatrix = Matrix.Identity;
            List<VertexPositionColor> vertices = Drawer3D.GetTriangleStrip(points, thickness, color, ref triangleCount, worldMatrix);
            triangles = new VertexPositionColor[vertices.Count];
            vertices.CopyTo(triangles);
        }

        public override void AccumulateStrips(LineStrip vertices)
        {
            if (triangleCount <= 0 || Points.Count() < 2)
            {
                return;
            }
            vertices.NumTriangles += triangleCount;
            vertices.Vertices.Add(triangles[0]);
            vertices.Vertices.AddRange(triangles);
            vertices.Vertices.Add(triangles[triangles.Length - 1]);
        }

        public override void Render(GraphicsDevice device, Effect effect)
        {
            if(triangleCount <= 0 || Points.Count() < 2)
            {
                return;
            }
            Matrix w = Matrix.Identity;
            effect.Parameters["xWorld"].SetValue(w);
            effect.CurrentTechnique = effect.Techniques["Untextured"];
            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, triangles, 0, triangleCount - 2);
            }
            effect.CurrentTechnique = effect.Techniques["Textured"];

        }
    }

}