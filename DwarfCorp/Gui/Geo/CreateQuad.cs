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
        public static Mesh Quad()
        {
            var result = new Mesh();
            result.Verticies = new Vertex[4];
            result.VertexCount = 4;

            result.Verticies[0].Position = new Vector3(0.0f, 0.0f, 0);
            result.Verticies[1].Position = new Vector3(1.0f, 0.0f, 0);
            result.Verticies[2].Position = new Vector3(1.0f, 1.0f, 0);
            result.Verticies[3].Position = new Vector3(0.0f, 1.0f, 0);

            result.Verticies[0].TextureCoordinate = new Vector2(0.0f, 0.0f);
            result.Verticies[1].TextureCoordinate = new Vector2(1.0f, 0.0f);
            result.Verticies[2].TextureCoordinate = new Vector2(1.0f, 1.0f);
            result.Verticies[3].TextureCoordinate = new Vector2(0.0f, 1.0f);

            result.Verticies[0].Color = Vector4.One;
            result.Verticies[1].Color = Vector4.One;
            result.Verticies[2].Color = Vector4.One;
            result.Verticies[3].Color = Vector4.One;

            result.indicies = new short[] { 0, 1, 2, 3, 0, 2 };
            return result;
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

            GrowVerticies(4); // Todo: Need to implement a better growth strategy.

            Verticies[baseIndex + 0] = new Vertex { Position = new Vector3(0.0f, 0.0f, 0), TextureCoordinate = new Vector2(0.0f, 0.0f), Color = Vector4.One };
            Verticies[baseIndex + 1] = new Vertex { Position = new Vector3(1.0f, 0.0f, 0), TextureCoordinate = new Vector2(1.0f, 0.0f), Color = Vector4.One };
            Verticies[baseIndex + 2] = new Vertex { Position = new Vector3(1.0f, 1.0f, 0), TextureCoordinate = new Vector2(1.0f, 1.0f), Color = Vector4.One };
            Verticies[baseIndex + 3] = new Vertex { Position = new Vector3(0.0f, 1.0f, 0), TextureCoordinate = new Vector2(0.0f, 1.0f), Color = Vector4.One };

            var indexBase = indicies.Length;
            GrowIndicies(6);
            indicies[indexBase + 0] = (short)(baseIndex + 0);
            indicies[indexBase + 1] = (short)(baseIndex + 1);
            indicies[indexBase + 2] = (short)(baseIndex + 2);
            indicies[indexBase + 3] = (short)(baseIndex + 3);
            indicies[indexBase + 4] = (short)(baseIndex + 0);
            indicies[indexBase + 5] = (short)(baseIndex + 2);

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

            GrowVerticies(4); // Todo: Need to implement a better growth strategy.

            Verticies[baseIndex + 0] = new Vertex { Position = new Vector3(topLeft, 0.0f),     TextureCoordinate = new Vector2(0.0f, 0.0f), Color = Vector4.One };
            Verticies[baseIndex + 1] = new Vertex { Position = new Vector3(topRight, 0.0f),    TextureCoordinate = new Vector2(1.0f, 0.0f), Color = Vector4.One };
            Verticies[baseIndex + 2] = new Vertex { Position = new Vector3(bottomRight, 0.0f), TextureCoordinate = new Vector2(1.0f, 1.0f), Color = Vector4.One };
            Verticies[baseIndex + 3] = new Vertex { Position = new Vector3(bottomLeft, 0.0f),  TextureCoordinate = new Vector2(0.0f, 1.0f), Color = Vector4.One };

            var indexBase = indicies.Length;
            GrowIndicies(6);
            indicies[indexBase + 0] = (short)(baseIndex + 0);
            indicies[indexBase + 1] = (short)(baseIndex + 1);
            indicies[indexBase + 2] = (short)(baseIndex + 2);
            indicies[indexBase + 3] = (short)(baseIndex + 3);
            indicies[indexBase + 4] = (short)(baseIndex + 0);
            indicies[indexBase + 5] = (short)(baseIndex + 2);

            return result;
        }

    }
}