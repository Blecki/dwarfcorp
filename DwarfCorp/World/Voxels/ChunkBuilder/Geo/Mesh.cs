using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Voxels.Geo
{
    public partial class Mesh // Todo - move entire geo bit over to the graphics namespace. Merge with raw primitive.
    {
        public ExtendedVertex[] Verticies;
        public int VertexCount { get; private set; }

        public short[] Indicies;
        public int IndexCount { get; private set; }

        private Mesh() { }

        public void Render(GraphicsDevice Device)
        {
            if (VertexCount != 0)
                Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Verticies, 0, VertexCount, Indicies, 0, IndexCount / 3);
        }

        public static Mesh EmptyMesh()
        {
            return new Mesh()
            {
                Verticies = new ExtendedVertex[0],
                VertexCount = 0,
                Indicies = new short[0],
                IndexCount = 0
            };
        }

        public void ResetCounts()
        {
            VertexCount = 0;
            IndexCount = 0;
        }

        public void GrowVerticies(int by)
        {
            VertexCount += by;
            if (Verticies.Length < VertexCount)
            {
                var newVerts = new ExtendedVertex[(int)Math.Ceiling(VertexCount * 1.5)];
                Verticies.CopyTo(newVerts, 0);
                Verticies = newVerts;
            }
        }

        public void GrowIndicies(int by)
        {
            IndexCount += by;
            if (Indicies.Length < IndexCount)
            {
                var newIndicies = new short[(int)Math.Ceiling(IndexCount * 1.5)];
                Indicies.CopyTo(newIndicies, 0);
                Indicies = newIndicies;
            }
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

            result.Verticies = new ExtendedVertex[parts.Sum((p) => p.VertexCount)];
            result.Indicies = new short[parts.Sum((p) => p.IndexCount)];
            result.VertexCount = result.Verticies.Length;
            result.IndexCount = result.Indicies.Length;

            int vCount = 0;
            int iCount = 0;
            foreach (var part in parts)
            {
                for (int i = 0; i < part.VertexCount; ++i) result.Verticies[i + vCount] = part.Verticies[i];
                for (int i = 0; i < part.IndexCount; ++i) result.Indicies[i + iCount] = (short)(part.Indicies[i] + vCount);
                vCount += part.VertexCount;
                iCount += part.IndexCount;
            }
            return result;
        }

        public MeshPart Concat(Mesh Other)
        {
            var result = this.BeginPart();
            result.VertexCount = Other.VertexCount;
            var indexStart = IndexCount;

            this.GrowVerticies(Other.VertexCount);
            this.GrowIndicies(Other.IndexCount);

            for (int i = 0; i < Other.VertexCount; ++i) this.Verticies[i + result.VertexOffset] = Other.Verticies[i];
            for (int i = 0; i < Other.IndexCount; ++i) this.Indicies[i + indexStart] = (short)(Other.Indicies[i] + result.VertexOffset);

            return result;
        }

        public RawPrimitive AsRawPrimitive()
        {
            return new RawPrimitive
            {
                Vertices = Verticies,
                VertexCount = VertexCount,
                Indexes = Indicies,
                IndexCount = IndexCount
            };
        }

        public static Mesh FromRawPrimitive(RawPrimitive Prim)
        {
            return new Mesh
            {
                Verticies = Prim.Vertices,
                VertexCount = Prim.VertexCount,
                Indicies = Prim.Indexes,
                IndexCount = Prim.IndexCount
            };
        }
    }
}