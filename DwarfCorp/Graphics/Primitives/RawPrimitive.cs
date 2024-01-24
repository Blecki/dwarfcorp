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
        public short[] Indexes = new short[6];
        public ExtendedVertex[] Vertices = new ExtendedVertex[4];

        public RawPrimitive Transform(Matrix M)
        {
            for (var x = 0; x < VertexCount; ++x)
                Vertices[x].Position = Vector3.Transform(Vertices[x].Position, M);                

            return this;
        }

        private static void EnsureSpace<T>(ref T[] In, int Size)
        {
            if (In == null)
            {
                In = new T[Size + 1
                    ];
                return;
            }

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

        public void AddTriangle(ExtendedVertex A, ExtendedVertex B, ExtendedVertex C)
        {
            var index = (short)VertexCount;
            AddVertex(A);
            AddVertex(B);
            AddVertex(C);
            AddIndex(index);
            AddIndex((short)(index + 1));
            AddIndex((short)(index + 2));
        }

        public void AddIndicies(short[] Indicies)
        {
            EnsureSpace(ref Indexes, IndexCount + Indicies.Length);
            for (var i = 0; i < Indicies.Length; ++i)
            {
                Indexes[IndexCount] = Indicies[i];
                IndexCount += 1;
            }
        }

        public void AddOffsetIndicies(short[] Indicies, int BaseIndex, int Count)
        {
            EnsureSpace(ref Indexes, IndexCount + Count);
            for (var i = 0; i < Count; ++i)
            {
                Indexes[IndexCount] = (short)(Indicies[i] + BaseIndex);
                IndexCount += 1;
            }
        }

        public void AddPolygon(List<ExtendedVertex> Verticies)
        {
            var pivot = 0;
            for (var x = 1; x < Verticies.Count - 1; ++x)
                AddTriangle(Verticies[pivot], Verticies[x], Verticies[x + 1]);
        }

        public void AddQuad(Matrix Transform, Color Color, Color VertColor, Vector2[] Uvs, Vector4 Bounds)
        {
            var baseIndex = VertexCount;
            AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(-0.5f, 0.0f, 0.5f), Transform), Color, VertColor, Uvs[0], Bounds));
            AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(0.5f, 0.0f, 0.5f), Transform), Color, VertColor, Uvs[1], Bounds));
            AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(0.5f, 0.0f, -0.5f), Transform), Color, VertColor, Uvs[2], Bounds));
            AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(-0.5f, 0.0f, -0.5f), Transform), Color, VertColor, Uvs[3], Bounds));

            AddOffsetIndicies(new short[] { 0, 1, 3, 1, 2, 3 }, baseIndex, 6);
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
                if (Vertices == null || VertexCount < 3 || IndexCount < 3)
                    return;

            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                Vertices, 0, VertexCount,
                Indexes, 0, IndexCount / 3);
        }

        public static RawPrimitive Concat(IEnumerable<RawPrimitive> Primitives)
        {
            return ConcatEx(Primitives);
        }

        public static RawPrimitive ConcatEx(IEnumerable<RawPrimitive> Primitives)
        {
            var totalVertexCount = Primitives.Select(p => p == null ? 0 : p.VertexCount).Sum();
            var totalIndexCount = Primitives.Select(p => p == null ? 0 : p.IndexCount).Sum();

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
                if (primitive == null)
                    continue;

                for (var i = 0; i < primitive.VertexCount; ++i)
                    r.Vertices[vBase + i] = primitive.Vertices[i];
                for (var i = 0; i < primitive.IndexCount; ++i)
                    r.Indexes[iBase + i] = (short)(primitive.Indexes[i] + vBase);

                vBase += primitive.VertexCount;
                iBase += primitive.IndexCount;
            }

            return r;
        }

        public static RawPrimitive Concat(params RawPrimitive[] prims)
        {
            return ConcatEx(prims);
        }

        public static RawPrimitive Cube(Color Color, Color VertColor, Vector2[] FaceUVs, Vector4 TextureBounds)
        {
            var r = new RawPrimitive();
            r.AddQuad(Matrix.CreateTranslation(0.0f, 0.5f, 0.0f), Color, VertColor, FaceUVs, TextureBounds);
            r.AddQuad(Matrix.CreateTranslation(0.0f, -0.5f, 0.0f), Color, VertColor, FaceUVs, TextureBounds);
            r.AddQuad(Matrix.CreateRotationX((float)Math.PI / 2) * Matrix.CreateTranslation(0.0f, 0.0f, 0.5f), Color, VertColor, FaceUVs, TextureBounds);
            r.AddQuad(Matrix.CreateRotationX((float)Math.PI / 2) * Matrix.CreateTranslation(0.0f, 0.0f, -0.5f), Color, VertColor, FaceUVs, TextureBounds);
            r.AddQuad(Matrix.CreateRotationX((float)Math.PI / 2) * Matrix.CreateRotationY((float)Math.PI / 2) * Matrix.CreateTranslation(0.5f, 0.0f, 0.0f), Color, VertColor, FaceUVs, TextureBounds);
            r.AddQuad(Matrix.CreateRotationX((float)Math.PI / 2) * Matrix.CreateRotationY((float)Math.PI / 2) * Matrix.CreateTranslation(-0.5f, 0.0f, 0.0f), Color, VertColor, FaceUVs, TextureBounds);
            return r;
        }

        public static RawPrimitive CreateFromCSGSolid(Csg.Solid Solid)
        {
            var r = new RawPrimitive();
            foreach (var poly in Solid.Polygons)
                r.AddPolygon(poly.Vertices.Select(v => new ExtendedVertex { Position = new Vector3((float)v.Pos.X, (float)v.Pos.Y, (float)v.Pos.Z) }).ToList());
            return r;
        } 

        public RawPrimitive TransformEx(Func<ExtendedVertex, ExtendedVertex> TFunc)
        {
            Vertices = Vertices.Select(v => TFunc(v)).ToArray();
            return this;
        }
    }
}