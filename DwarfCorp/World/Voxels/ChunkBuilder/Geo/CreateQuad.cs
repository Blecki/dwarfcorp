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
        public static TemplateMesh Quad()
        {
            var r = TemplateMesh.EmptyMesh();
            r.QuadPart();
            return r;
        }

        public static TemplateMesh Quad(Vector3 bottomLeft, Vector3 topLeft, Vector3 bottomRight, Vector3 topRight)
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

        public TemplateMeshPart QuadPart()
        {
            var result = new TemplateMeshPart
            {
                VertexOffset = VertexCount,
                VertexCount = 4,
                Mesh = this
            };

            var baseIndex = VertexCount;

            GrowVerticies(4);

            Verticies[baseIndex + 0] = new TemplateVertex(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(0.0f, 0.0f));
            Verticies[baseIndex + 1] = new TemplateVertex(new Vector3(1.0f, 0.0f, 0.0f), new Vector2(1.0f, 0.0f));
            Verticies[baseIndex + 2] = new TemplateVertex(new Vector3(1.0f, 1.0f, 0.0f), new Vector2(1.0f, 1.0f));
            Verticies[baseIndex + 3] = new TemplateVertex(new Vector3(0.0f, 1.0f, 0.0f), new Vector2(0.0f, 1.0f));

            AddIndicies(baseIndex, 0, 1, 2, 3, 0, 2);

            return result;
        }


        public TemplateMeshPart QuadPart(Vector3 bottomLeft, Vector3 topLeft, Vector3 bottomRight, Vector3 topRight)
        {
            var result = new TemplateMeshPart
            {
                VertexOffset = VertexCount,
                VertexCount = 4,
                Mesh = this
            };

            var baseIndex = VertexCount;

            GrowVerticies(4);

            Verticies[baseIndex + 0] = new TemplateVertex(topLeft, new Vector2(0.0f, 0.0f));
            Verticies[baseIndex + 1] = new TemplateVertex(topRight, new Vector2(1.0f, 0.0f));
            Verticies[baseIndex + 2] = new TemplateVertex(bottomRight, new Vector2(1.0f, 1.0f));
            Verticies[baseIndex + 3] = new TemplateVertex(bottomLeft, new Vector2(0.0f, 1.0f));

            AddIndicies(baseIndex, 0, 1, 2, 3, 0, 2);

            return result;
        }

    }
}