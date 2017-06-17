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
        public Vertex[] verticies;
        public short[] indicies;

        public void Render(GraphicsDevice Device)
        {
            if (verticies.Length != 0)
                Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, verticies, 0, verticies.Length,
                    indicies, 0, indicies.Length / 3);
        }

        public static Mesh EmptyMesh()
        {
            return new Mesh() { indicies = new short[0], verticies = new Vertex[0] };
        }
    }
}