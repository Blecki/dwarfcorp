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
        public static void CopyIndicies(short[] into, int at, short[] source)
        {
            for (int i = 0; i < source.Length; ++i)
                into[at + i] = source[i];
        }

        public Mesh Copy()
        {
            var result = new Mesh();

            result.VertexCount = VertexCount;
            result.Verticies = new Vertex[Verticies.Length];
            Verticies.CopyTo(result.Verticies, 0);
            result.indicies = new short[indicies.Length];
            CopyIndicies(result.indicies, 0, indicies);
            return result;
        }

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

        public Mesh Texture(Matrix M)
        {
            return Texture(M, 0, VertexCount);
        }

        public Mesh Texture(Matrix M, int StartIndex, int Count)
        {
            for (int i = StartIndex; i < StartIndex + Count; ++i)
                Verticies[i].TextureCoordinate = Vector2.Transform(Verticies[i].TextureCoordinate, M);
            return this;
        }

        public Mesh TileScaleAndTexture(ITileSheet Sheet, int T)
        {
            return this.Scale(Sheet.TileWidth, Sheet.TileHeight)
                .Texture(Sheet.TileMatrix(T));
        }

        public Mesh Colorize(Vector4 Color)
        {
            return Colorize(Color, 0, VertexCount);
        }

        public Mesh Colorize(Vector4 Color, int StartIndex, int Count)
        {
            for (int i = StartIndex; i < VertexCount && i < StartIndex + Count; ++i)
                Verticies[i].Color = Color;
            return this;
        }

        public Mesh Morph(Func<Vector3, Vector3> func)
        {
            for (int i = 0; i < VertexCount; ++i)
                Verticies[i].Position = func(Verticies[i].Position);
            return this;
        }

        public Mesh MorphEx(Func<Vertex, Vertex> func)
        {
            for (int i = 0; i < VertexCount; ++i)
                Verticies[i] = func(Verticies[i]);
            return this;
        }

        public static Mesh ClipToNewMesh(Mesh other, Rectangle rect)
        {
            var result = new Mesh();
            result.indicies = new short[other.indicies.Length];
            result.Verticies = new Vertex[other.Verticies.Length];
            result.VertexCount = other.VertexCount;
            other.indicies.CopyTo(result.indicies, 0);
            other.Verticies.CopyTo(result.Verticies, 0);

            for (int i = 0; i < other.VertexCount; i++)
                result.Verticies[i].Position = MathFunctions.Clamp(result.Verticies[i].Position, rect);

            return result;
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
    }
}