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

        private void AddIndicies(int BaseIndex, params short[] Indicies)
        {
            var indexBase = IndexCount;
            GrowIndicies(Indicies.Length);
            for (var i = 0; i < Indicies.Length; ++i)
                this.Indicies[indexBase + i] = (short)(BaseIndex + Indicies[i]);
        }

        public TemplateMeshPart QuadPart(TemplateVertex bottomLeft, TemplateVertex topLeft, TemplateVertex bottomRight, TemplateVertex topRight)
        {
            var result = new TemplateMeshPart
            {
                VertexOffset = VertexCount,
                VertexCount = 4,
                Mesh = this
            };

            var baseIndex = VertexCount;

            GrowVerticies(4);

            Verticies[baseIndex + 0] = topLeft.WithTextCoordinate(new Vector2(0.0f, 0.0f));
            Verticies[baseIndex + 1] = topRight.WithTextCoordinate(new Vector2(1.0f, 0.0f));
            Verticies[baseIndex + 2] = bottomRight.WithTextCoordinate(new Vector2(1.0f, 1.0f));
            Verticies[baseIndex + 3] = bottomLeft.WithTextCoordinate(new Vector2(0.0f, 1.0f));

            AddIndicies(baseIndex, 0, 1, 2, 3, 0, 2);

            return result;
        }

    }
}