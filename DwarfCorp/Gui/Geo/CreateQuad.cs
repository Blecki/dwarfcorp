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
        public static Mesh Quad(Vector2 bottomLeft, Vector2 topLeft, Vector2 bottomRight, Vector2 topRight)
        {
            var result = new Mesh();
            result.verticies = new Vertex[4];

            result.verticies[0].Position = new Vector3(topLeft.X, topLeft.Y, 0);
            result.verticies[1].Position = new Vector3(topRight.X, topRight.Y, 0);
            result.verticies[2].Position = new Vector3(bottomRight.X, bottomRight.Y, 0);
            result.verticies[3].Position = new Vector3(bottomLeft.X, bottomLeft.Y, 0);

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

        /// <summary>
        /// Sets a quad's texture coordinates back to the default values. Sure hope the mesh is actually a quad!
        /// </summary>
        public void ResetQuadTexture()
        {
            // Better be a fucking quad!

            verticies[0].TextureCoordinate = new Vector2(0.0f, 0.0f);
            verticies[1].TextureCoordinate = new Vector2(1.0f, 0.0f);
            verticies[2].TextureCoordinate = new Vector2(1.0f, 1.0f);
            verticies[3].TextureCoordinate = new Vector2(0.0f, 1.0f);
        }
    }
}