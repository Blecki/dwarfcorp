using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Voxels.Geo
{
    public partial class TemplateMesh
    {
        public TemplateVertex[] Verticies;
        public int VertexCount { get; private set; }

        private TemplateMesh() { }

        public static TemplateMesh EmptyMesh()
        {
            return new TemplateMesh()
            {
                Verticies = new TemplateVertex[0],
                VertexCount = 0,
            };
        }

        private void GrowVerticies(int by)
        {
            VertexCount += by;
            if (Verticies.Length < VertexCount)
            {
                var newVerts = new TemplateVertex[(int)Math.Ceiling(VertexCount * 1.5)];
                Verticies.CopyTo(newVerts, 0);
                Verticies = newVerts;
            }
        }

        public TemplateMeshPart BeginPart()
        {
            return GetPart(VertexCount, 0);
        }

        public TemplateMeshPart EntireMeshAsPart()
        {
            return GetPart(0, VertexCount);
        }

        public TemplateMeshPart GetPart(int VertexOffset, int VertexCount)
        {
            return new TemplateMeshPart
            {
                Mesh = this,
                VertexOffset = VertexOffset,
                VertexCount = VertexCount
            };
        }
    }
}