using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    /// <summary>
    /// A simple vertex type with position, color, and texture.
    /// </summary>
    public struct Vertex : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector4 Color;

        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(20, VertexElementFormat.Vector4, VertexElementUsage.Color, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }

    /// <summary>
    /// A simple indexed triangle mesh.
    /// </summary>
    public partial class Mesh
    {
        public Vertex[] Verticies;
        public int VertexCount { get; private set; }

        public short[] indicies;

        public void Render(GraphicsDevice Device)
        {
            if (VertexCount != 0)
                Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Verticies, 0, VertexCount,
                    indicies, 0, indicies.Length / 3);
        }

        public static Mesh EmptyMesh()
        {
            return new Mesh() { indicies = new short[0], Verticies = new Vertex[0], VertexCount = 0 };
        }

        public void GrowVerticies(int by)
        {
            VertexCount += by;
            if (Verticies.Length < VertexCount)
            {
                var newVerts = new Vertex[(int)Math.Ceiling(VertexCount * 1.5)];
                Verticies.CopyTo(newVerts, 0);
                Verticies = newVerts;
            }
        }

        public void GrowIndicies(int by)
        {
            var newIndicies = new short[indicies.Length + by];
            indicies.CopyTo(newIndicies, 0);
            indicies = newIndicies;
        }

        public MeshPart BeginPart()
        {
            return GetPart(VertexCount, 0);
        }

        public MeshPart EntireMeshAsPart()
        {
            return GetPart(0, VertexCount);
        }

        public MeshPart GetPart(int VertexOffset, int VertexCount)
        {
            return new MeshPart
            {
                Mesh = this,
                VertexOffset = VertexOffset,
                VertexCount = VertexCount
            };
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