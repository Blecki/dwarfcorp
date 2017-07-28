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
        public ushort[] Indexes = null;
        public ExtendedVertex[] Vertices = null;

        public static RawPrimitive Concat(IEnumerable<RawPrimitive> Primitives)
        {
            var totalVertexCount = Primitives.Select(p => p.VertexCount).Sum();
            var totalIndexCount = Primitives.Select(p => p.IndexCount).Sum();

            var r = new RawPrimitive
            {
                VertexCount = totalVertexCount,
                Vertices = new ExtendedVertex[totalVertexCount],
                IndexCount = totalIndexCount,
                Indexes = new ushort[totalIndexCount]
            };

            var vBase = 0;
            var iBase = 0;

            foreach (var primitive in Primitives)
            {
                primitive.Vertices.CopyTo(r.Vertices, vBase);
                for (var i = 0; i < primitive.IndexCount; ++i)
                    r.Indexes[iBase + i] = (ushort)(primitive.Indexes[i] + vBase);

                vBase += primitive.VertexCount;
                iBase += primitive.IndexCount;
            }

            return r;
        }
    }
}