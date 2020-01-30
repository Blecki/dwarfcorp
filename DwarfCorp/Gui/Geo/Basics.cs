using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public partial class Mesh
    {
        private Mesh Transform(Matrix m, int start, int count)
        {
            for (int i = start; i < start + count; ++i)
                Verticies[i].Position = Vector3.Transform(Verticies[i].Position, m);
            return this;
        }

        public Mesh Transform(Matrix m)
        {
            return Transform(m, 0, VertexCount);
        }

        public Mesh Scale(float X, float Y)
        {
            return Transform(Matrix.CreateScale(X, Y, 1.0f));
        }

        public Mesh Translate(float X, float Y)
        {
            return Transform(Matrix.CreateTranslation(X, Y, 0.0f));
        }

        public Mesh Transform(float X, float Y, float w, float h, float r)
        {
            return Transform(Matrix.CreateTranslation(-w * 0.5f, -h * 0.5f, 0) * Matrix.CreateRotationZ(r) * Matrix.CreateTranslation(w * 0.5f, h * 0.5f, 0) * Matrix.CreateTranslation(X, Y, 0));
        }

        public static Mesh Merge(params Mesh[] parts)
        {
            var result = new Mesh();

            result.Verticies = new Vertex[parts.Sum((p) => p.VertexCount)];
            result.indicies = new short[parts.Sum((p) => p.indicies.Length)];
            result.VertexCount = result.Verticies.Length;

            int vCount = 0;
            int iCount = 0;
            foreach (var part in parts)
            {
                for (int i = 0; i < part.VertexCount; ++i) result.Verticies[i + vCount] = part.Verticies[i];
                for (int i = 0; i < part.indicies.Length; ++i) result.indicies[i + iCount] = (short)(part.indicies[i] + vCount);
                vCount += part.VertexCount;
                iCount += part.indicies.Length;
            }
            return result;
        }

        public MeshPart Concat(Mesh Other)
        {
            var result = this.BeginPart();
            result.VertexCount = Other.VertexCount;
            var indexStart = this.indicies.Length;

            this.GrowVerticies(Other.VertexCount);
            this.GrowIndicies(Other.indicies.Length);

            for (int i = 0; i < Other.VertexCount; ++i) this.Verticies[i + result.VertexOffset] = Other.Verticies[i];
            for (int i = 0; i < Other.indicies.Length; ++i) this.indicies[i + indexStart] = (short)(Other.indicies[i] + result.VertexOffset);

            return result;
        }
    }
}