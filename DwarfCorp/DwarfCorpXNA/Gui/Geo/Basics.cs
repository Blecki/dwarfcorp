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

            result.verticies = new Vertex[verticies.Length];
            for (int i = 0; i < verticies.Length; ++i) result.verticies[i] = verticies[i];
            result.indicies = new short[indicies.Length];
            CopyIndicies(result.indicies, 0, indicies);
            return result;
        }

        private Mesh Transform(Matrix m, int start, int count)
        {
            if (start < 0) start = verticies.Length - start;
            for (int i = start; i < start + count; ++i)
                verticies[i].Position = Vector3.Transform(verticies[i].Position, m);
            return this;
        }

        public Mesh Transform(Matrix m)
        {
            return Transform(m, 0, verticies.Length);
        }

        public Mesh Scale(float X, float Y)
        {
            return Transform(Matrix.CreateScale(X, Y, 1.0f));
        }

        public Mesh Translate(float X, float Y)
        {
            return Transform(Matrix.CreateTranslation(X, Y, 0.0f));
        }

        public Mesh Texture(Matrix m)
        {
            for (int i = 0; i < verticies.Length; ++i)
                verticies[i].TextureCoordinate = Vector2.Transform(verticies[i].TextureCoordinate, m);
            return this;
        }

        public Mesh Texture(Matrix M, int StartIndex, int Count)
        {
            for (int i = StartIndex; i < StartIndex + Count; ++i)
                verticies[i].TextureCoordinate = Vector2.Transform(verticies[i].TextureCoordinate, M);
            return this;
        }

        public Mesh TileScaleAndTexture(ITileSheet Sheet, int T)
        {
            return this.Scale(Sheet.TileWidth, Sheet.TileHeight)
                .Texture(Sheet.TileMatrix(T));
        }

        public Mesh Colorize(Vector4 Color)
        {
            for (int i = 0; i < verticies.Length; ++i)
                verticies[i].Color = Color;
            return this;
        }

        public Mesh Colorize(Vector4 Color, int StartIndex, int Count)
        {
            for (int i = StartIndex; i < verticies.Length && i < StartIndex + Count; ++i)
                verticies[i].Color = Color;
            return this;
        }

        public Mesh Morph(Func<Vector3, Vector3> func)
        {
            for (int i = 0; i < verticies.Length; ++i)
                verticies[i].Position = func(verticies[i].Position);
            return this;
        }

        public Mesh MorphEx(Func<Vertex, Vertex> func)
        {
            for (int i = 0; i < verticies.Length; ++i)
                verticies[i] = func(verticies[i]);
            return this;
        }

        public Vertex[] GetVerticies(int startIndex, int Length)
        {
            var r = new Vertex[Length];
            for (int i = 0; i < Length; ++i) r[i] = verticies[i + startIndex];
            return r;
        }

        public static Mesh Clip(Mesh other, Rectangle rect)
        {
            var result = new Mesh();
            result.indicies = new short[other.indicies.Length];
            result.verticies = new Vertex[other.verticies.Length];
            other.indicies.CopyTo(result.indicies, 0);
            other.verticies.CopyTo(result.verticies, 0);

            for (int i = 0; i < other.verticies.Length; i++)
            {
                result.verticies[i].Position = MathFunctions.Clamp(result.verticies[i].Position, rect);
            }
            return result;
        }

        public static Mesh Merge(params Mesh[] parts)
        {
            var result = new Mesh();

            result.verticies = new Vertex[parts.Sum((p) => p.verticies.Length)];
            result.indicies = new short[parts.Sum((p) => p.indicies.Length)];

            int vCount = 0;
            int iCount = 0;
            foreach (var part in parts)
            {
                for (int i = 0; i < part.verticies.Length; ++i) result.verticies[i + vCount] = part.verticies[i];
                for (int i = 0; i < part.indicies.Length; ++i) result.indicies[i + iCount] = (short)(part.indicies[i] + vCount);
                vCount += part.verticies.Length;
                iCount += part.indicies.Length;
            }
            return result;
        }
    }
}