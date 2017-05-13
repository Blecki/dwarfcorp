// LineListCommand3D.cs
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
        public Color[] Colors;

        public LineListCommand3D(Vector3[] points, IEnumerable<Color> color, float thickness) :
            base(Color.Black)
        {
            Colors = color.ToArray();
            this.Points = points;
            Matrix worldMatrix = Matrix.Identity;
            List<VertexPositionColor> vertices = Drawer3D.GetTriangleStrip(points, thickness, Colors, ref triangleCount, worldMatrix);
            triangles = new VertexPositionColor[vertices.Count];
            vertices.CopyTo(triangles);
        }


        public LineListCommand3D(Vector3[] points, Color color, float thickness) :
            base(color)
        {
            this.Points = points;
            Matrix worldMatrix = Matrix.Identity;
            List<VertexPositionColor> vertices =
                Colors == null ? Drawer3D.GetTriangleStrip(points, thickness, Colors, ref triangleCount, worldMatrix) :
                Drawer3D.GetTriangleStrip(points, thickness, color, ref triangleCount, worldMatrix);

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
            if (vertices.Vertices.Count > 0)
            {
                vertices.Vertices.Add(vertices.Vertices.Last());
                vertices.Vertices.Add(triangles[0]);
                vertices.NumTriangles += 1;
            }
            vertices.Vertices.AddRange(triangles);

        }

        public override void Render(GraphicsDevice device, Shader effect)
        {
            if(triangleCount <= 0 || Points.Count() < 2)
            {
                return;
            }
            Matrix w = Matrix.Identity;
            effect.World = w;
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Untextured];
            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, triangles, 0, triangleCount - 2);
            }
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Textured];

        }
    }

}
