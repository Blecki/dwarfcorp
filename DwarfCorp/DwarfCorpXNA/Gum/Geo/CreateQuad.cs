using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Gum
{
    public partial class Mesh
    {
        public static Mesh Quad()
        {
            var result = new Mesh();
            result.verticies = new Vertex[4];

            result.verticies[0].Position = new Vector3(0.0f, 0.0f, 0);
            result.verticies[1].Position = new Vector3(1.0f, 0.0f, 0);
            result.verticies[2].Position = new Vector3(1.0f, 1.0f, 0);
            result.verticies[3].Position = new Vector3(0.0f, 1.0f, 0);

            result.verticies[0].TextureCoordinate = new Vector2(0.0f, 0.0f);
            result.verticies[1].TextureCoordinate = new Vector2(1.0f, 0.0f);
            result.verticies[2].TextureCoordinate = new Vector2(1.0f, 1.0f);
            result.verticies[3].TextureCoordinate = new Vector2(0.0f, 1.0f);

            result.verticies[0].Color = Vector4.One;
            result.verticies[1].Color = Vector4.One;
            result.verticies[2].Color = Vector4.One;
            result.verticies[3].Color = Vector4.One;

            result.indicies = new short[] { 0, 1, 2, 3, 0, 2 };
            return result;
        }
    }
}