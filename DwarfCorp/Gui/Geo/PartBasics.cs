using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public partial class MeshPart
    {
        private MeshPart Transform(Matrix M, int start, int count)
        {
            for (int i = start; i < start + count; ++i)
                Mesh.Verticies[VertexOffset + i].Position = Vector3.Transform(Mesh.Verticies[VertexOffset + i].Position, M);
            return this;
        }

        public MeshPart Transform(Matrix m)
        {
            return Transform(m, 0, VertexCount);
        }

        public MeshPart Scale(float X, float Y)
        {
            return Transform(Matrix.CreateScale(X, Y, 1.0f));
        }

        public MeshPart Translate(float X, float Y)
        {
            return Transform(Matrix.CreateTranslation(X, Y, 0.0f));
        }

        public MeshPart Transform(float X, float Y, float w, float h, float r)
        {
            return Transform(Matrix.CreateTranslation(-w * 0.5f, -h * 0.5f, 0) * Matrix.CreateRotationZ(r) * Matrix.CreateTranslation(w * 0.5f, h * 0.5f, 0) * Matrix.CreateTranslation(X, Y, 0));
        }

        public MeshPart Texture(Matrix m)
        {
            return Texture(m, 0, VertexCount);
        }

        public MeshPart Texture(Matrix M, int StartIndex, int Count)
        {
            for (int i = StartIndex; i < StartIndex + Count; ++i)
                Mesh.Verticies[VertexOffset + i].TextureCoordinate = Vector2.Transform(Mesh.Verticies[VertexOffset + i].TextureCoordinate, M);
            return this;
        }

        public MeshPart TileScaleAndTexture(ITileSheet Sheet, int T)
        {
            return this.Scale(Sheet.TileWidth, Sheet.TileHeight).Texture(Sheet.TileMatrix(T));
        }

        public MeshPart Colorize(Vector4 Color)
        {
            return Colorize(Color, 0, VertexCount);
        }

        public MeshPart Colorize(Vector4 Color, int StartIndex, int Count)
        {
            for (int i = StartIndex; i < StartIndex + Count; ++i)
                Mesh.Verticies[VertexOffset + i].Color = Color;
            return this;
        }

        public MeshPart Morph(Func<Vector3, Vector3> func)
        {
            for (int i = 0; i < VertexCount; ++i)
                Mesh.Verticies[VertexOffset + i].Position = func(Mesh.Verticies[VertexOffset + i].Position);
            return this;
        }

        public MeshPart MorphEx(Func<Vertex, Vertex> func)
        {
            for (int i = 0; i < VertexCount; ++i)
                Mesh.Verticies[VertexOffset + i] = func(Mesh.Verticies[VertexOffset + i]);
            return this;
        }

        public MeshPart ClipToBounds(Rectangle Rect)
        {
            for (int i = 0; i < VertexCount; i++)
                Mesh.Verticies[VertexOffset + i].Position = MathFunctions.Clamp(Mesh.Verticies[VertexOffset + i].Position, Rect);

            return this;
        }

        /// <summary>
        /// Sets a quad's texture coordinates back to the default values. Sure hope the mesh is actually a quad!
        /// </summary>
        public MeshPart ResetQuadTexture()
        {
            // Better be a fucking quad!

            Mesh.Verticies[VertexOffset + 0].TextureCoordinate = new Vector2(0.0f, 0.0f);
            Mesh.Verticies[VertexOffset + 1].TextureCoordinate = new Vector2(1.0f, 0.0f);
            Mesh.Verticies[VertexOffset + 2].TextureCoordinate = new Vector2(1.0f, 1.0f);
            Mesh.Verticies[VertexOffset + 3].TextureCoordinate = new Vector2(0.0f, 1.0f);

            return this;
        }
    }
}