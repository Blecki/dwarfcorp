using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;


namespace DwarfCorp
{
    public class RawPrimitive
    {
        public int IndexCount = 0;
        public int VertexCount = 0;
        public short[] Indexes = null;
        public ExtendedVertex[] Vertices = null;

        private static void EnsureSpace<T>(ref T[] In, int Size)
        {
            if (Size >= In.Length)
            {
                var r = new T[In.Length * 2];
                In.CopyTo(r, 0);
                In = r;
            }
        }

        public void AddVertex(ExtendedVertex Vertex)
        {
            EnsureSpace(ref Vertices, VertexCount);
            Vertices[VertexCount] = Vertex;
            VertexCount += 1;
        }

        public void AddIndex(short Index)
        {
            EnsureSpace(ref Indexes, IndexCount);
            Indexes[IndexCount] = Index;
            IndexCount += 1;
        }

        public void Reset()
        {
            IndexCount = 0;
            VertexCount = 0;
        }

        public virtual void Render(GraphicsDevice device)
        {
#if MONOGAME_BUILD
                device.SamplerStates[0].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[1].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[2].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[3].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[4].Filter = TextureFilter.MinLinearMagPointMipLinear;
#endif
                if (Vertices == null || Vertices.Length < 3)
                    return;

            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                Vertices, 0, VertexCount,
                Indexes, 0, IndexCount / 3);
        }

        public static RawPrimitive Concat(IEnumerable<RawPrimitive> Primitives)
        {
            var totalVertexCount = Primitives.Select(p => p.VertexCount).Sum();
            var totalIndexCount = Primitives.Select(p => p.IndexCount).Sum();

            var r = new RawPrimitive
            {
                VertexCount = totalVertexCount,
                Vertices = new ExtendedVertex[totalVertexCount],
                IndexCount = totalIndexCount,
                Indexes = new short[totalIndexCount]
            };

            var vBase = 0;
            var iBase = 0;

            foreach (var primitive in Primitives)
            {
                for (var i = 0; i < primitive.VertexCount; ++i)
                    r.Vertices[vBase + i] = primitive.Vertices[i];
                for (var i = 0; i < primitive.IndexCount; ++i)
                    r.Indexes[iBase + i] = (short)(primitive.Indexes[i] + vBase);

                vBase += primitive.VertexCount;
                iBase += primitive.IndexCount;
            }

            return r;
        }
    }
}