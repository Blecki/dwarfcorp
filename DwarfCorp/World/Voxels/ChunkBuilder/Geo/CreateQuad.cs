﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Voxels.Geo
{
    public partial class Mesh
    {
        public static Mesh Quad()
        {
            var r = Mesh.EmptyMesh();
            r.QuadPart();
            return r;
        }

        private void AddIndicies(int BaseIndex, params short[] Indicies)
        {
            var indexBase = IndexCount;
            GrowIndicies(Indicies.Length);
            for (var i = 0; i < Indicies.Length; ++i)
                this.Indicies[indexBase + i] = (short)(BaseIndex + Indicies[i]);
        }

        public MeshPart QuadPart()
        {
            var result = new MeshPart
            {
                VertexOffset = VertexCount,
                VertexCount = 4,
                Mesh = this
            };

            var baseIndex = VertexCount;

            GrowVerticies(4);

            Verticies[baseIndex + 0] = new ExtendedVertex(new Vector3(0.0f, 0.0f, 0.0f), Color.White, Color.White, new Vector2(0.0f, 0.0f), new Vector4(0, 0, 1, 1));
            Verticies[baseIndex + 1] = new ExtendedVertex(new Vector3(1.0f, 0.0f, 0.0f), Color.White, Color.White, new Vector2(1.0f, 0.0f), new Vector4(0, 0, 1, 1));
            Verticies[baseIndex + 2] = new ExtendedVertex(new Vector3(1.0f, 1.0f, 0.0f), Color.White, Color.White, new Vector2(1.0f, 1.0f), new Vector4(0, 0, 1, 1));
            Verticies[baseIndex + 3] = new ExtendedVertex(new Vector3(0.0f, 1.0f, 0.0f), Color.White, Color.White, new Vector2(0.0f, 1.0f), new Vector4(0, 0, 1, 1));

            AddIndicies(baseIndex, 0, 1, 2, 3, 0, 2);

            return result;
        }


        public MeshPart QuadPart(Vector2 bottomLeft, Vector2 topLeft, Vector2 bottomRight, Vector2 topRight)
        {
            var result = new MeshPart
            {
                VertexOffset = VertexCount,
                VertexCount = 4,
                Mesh = this
            };

            var baseIndex = VertexCount;

            GrowVerticies(4); 

            Verticies[baseIndex + 0] = new ExtendedVertex(new Vector3(topLeft, 0.0f), Color.White, Color.White, new Vector2(0.0f, 0.0f), new Vector4(0, 0, 1, 1));
            Verticies[baseIndex + 1] = new ExtendedVertex(new Vector3(topRight, 0.0f), Color.White, Color.White, new Vector2(1.0f, 0.0f), new Vector4(0, 0, 1, 1));
            Verticies[baseIndex + 2] = new ExtendedVertex(new Vector3(bottomRight, 0.0f), Color.White, Color.White, new Vector2(1.0f, 1.0f), new Vector4(0, 0, 1, 1));
            Verticies[baseIndex + 3] = new ExtendedVertex(new Vector3(bottomLeft, 0.0f), Color.White, Color.White, new Vector2(0.0f, 1.0f), new Vector4(0, 0, 1, 1));

            AddIndicies(baseIndex, 0, 1, 2, 3, 0, 2);

            return result;
        }

    }
}