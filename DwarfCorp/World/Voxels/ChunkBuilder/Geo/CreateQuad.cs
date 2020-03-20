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
        public static TemplateMesh Quad(TemplateVertex bottomLeft, TemplateVertex topLeft, TemplateVertex bottomRight, TemplateVertex topRight)
        {
            var r = TemplateMesh.EmptyMesh();
            r.QuadPart(bottomLeft, topLeft, bottomRight, topRight);
            return r;
        }

        public void QuadPart(TemplateVertex bottomLeft, TemplateVertex topLeft, TemplateVertex bottomRight, TemplateVertex topRight)
        {
            var baseIndex = VertexCount;

            GrowVerticies(4);

            Verticies[baseIndex + 0] = topLeft.WithTextCoordinate(new Vector2(0.0f, 0.0f));
            Verticies[baseIndex + 1] = topRight.WithTextCoordinate(new Vector2(1.0f, 0.0f));
            Verticies[baseIndex + 2] = bottomRight.WithTextCoordinate(new Vector2(1.0f, 1.0f));
            Verticies[baseIndex + 3] = bottomLeft.WithTextCoordinate(new Vector2(0.0f, 1.0f));
        }
    }
}