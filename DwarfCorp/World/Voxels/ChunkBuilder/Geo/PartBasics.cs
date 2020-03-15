using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Voxels.Geo
{
    public partial class TemplateMeshPart
    {
        private TemplateMeshPart Transform(Matrix M, int start, int count)
        {
            for (int i = start; i < start + count; ++i)
                Mesh.Verticies[VertexOffset + i].Position = Vector3.Transform(Mesh.Verticies[VertexOffset + i].Position, M);
            return this;
        }

        public TemplateMeshPart Transform(Matrix m)
        {
            return Transform(m, 0, VertexCount);
        }

        public TemplateMeshPart Scale(float X, float Y)
        {
            return Transform(Matrix.CreateScale(X, Y, 1.0f));
        }

        public TemplateMeshPart Translate(float X, float Y)
        {
            return Transform(Matrix.CreateTranslation(X, Y, 0.0f));
        }

        public TemplateMeshPart Transform(float X, float Y, float w, float h, float r)
        {
            return Transform(Matrix.CreateTranslation(-w * 0.5f, -h * 0.5f, 0) * Matrix.CreateRotationZ(r) * Matrix.CreateTranslation(w * 0.5f, h * 0.5f, 0) * Matrix.CreateTranslation(X, Y, 0));
        }

        public TemplateMeshPart Texture(Matrix m)
        {
            return Texture(m, 0, VertexCount);
        }

        public TemplateMeshPart Texture(Matrix M, int StartIndex, int Count)
        {
            for (int i = StartIndex; i < StartIndex + Count; ++i)
                Mesh.Verticies[VertexOffset + i].TextureCoordinate = Vector2.Transform(Mesh.Verticies[VertexOffset + i].TextureCoordinate, M);
            return this;
        }
               
        public TemplateMeshPart Morph(Func<Vector3, Vector3> func)
        {
            for (int i = 0; i < VertexCount; ++i)
                Mesh.Verticies[VertexOffset + i].Position = func(Mesh.Verticies[VertexOffset + i].Position);
            return this;
        }

        public TemplateMeshPart MorphEx(Func<TemplateVertex, TemplateVertex> func)
        {
            for (int i = 0; i < VertexCount; ++i)
                Mesh.Verticies[VertexOffset + i] = func(Mesh.Verticies[VertexOffset + i]);
            return this;
        }
    }
}