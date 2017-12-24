using Microsoft.Xna.Framework;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Xna.Framework.Graphics
{

    // Summary:
    //     Describes a custom vertex format structure that contains position, color,
    //     and one set of texture coordinates.
    [Serializable]
    public struct ExtendedVertex : IVertexType
    {
        //
        // Summary:
        //     XYZ position.
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Vector3 Position;
        //
        // Summary:
        //     UV texture coordinates.
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Vector2 TextureCoordinate;

        // Summary:
        //     The vertex color.
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Color Color;
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Color VertColor;
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Vector4 TextureBounds;

        //
        // Summary:
        //     Vertex declaration, which defines per-vertex data.
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")] public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
            //Position
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            // Texture Coordinate
            new VertexElement(SizeOf(Vector3.Zero), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            // Color
            new VertexElement(SizeOf(Vector3.Zero) + SizeOf(Vector2.Zero), VertexElementFormat.Color, VertexElementUsage.Color, 0),
            // Vertex tint
            new VertexElement(SizeOf(Vector3.Zero) + SizeOf(Vector2.Zero) + SizeOf(Color.White), VertexElementFormat.Color, VertexElementUsage.Color, 1),
            // Texture bounds
            new VertexElement(SizeOf(Vector3.Zero) + SizeOf(Vector2.Zero) + SizeOf(Color.White) * 2, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1)
            );


        public static int SizeOf<T>(T obj)
        {
            return System.Runtime.InteropServices.Marshal.SizeOf(obj);
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        //
        // Summary:
        //     Initializes a new instance of the VertexPositionColorTexture class.
        //
        // Parameters:
        //   position:
        //     Position of the vertex.
        //
        //   color:
        //     Color of the vertex.
        //
        //   textureCoordinate:
        //     Texture coordinate of the vertex.
        public ExtendedVertex(Vector3 position, Color color, Color vertColor, Vector2 textureCoordinate, Vector4 textureBounds)
        {
            Position = position;
            Color = color;
            VertColor = vertColor;
            TextureCoordinate = textureCoordinate;
            TextureBounds = textureBounds;
        }

        // Summary:
        //     Compares two objects to determine whether they are different.
        //
        // Parameters:
        //   left:
        //     Object to the left of the inequality operator.
        //
        //   right:
        //     Object to the right of the inequality operator.
        public static bool operator !=(ExtendedVertex left, ExtendedVertex right)
        {
            return !(left.Color == right.Color && left.Position == right.Position 
                && left.TextureBounds == right.TextureBounds 
                && left.TextureCoordinate == right.TextureCoordinate 
                && left.VertColor == right.VertColor);
        }

        //
        // Summary:
        //     Compares two objects to determine whether they are the same.
        //
        // Parameters:
        //   left:
        //     Object to the left of the equality operator.
        //
        //   right:
        //     Object to the right of the equality operator.
        public static bool operator ==(ExtendedVertex left, ExtendedVertex right)
        {
            return !(left != right);
        }

        // Summary:
        //     Returns a value that indicates whether the current instance is equal to a
        //     specified object.
        //
        // Parameters:
        //   obj:
        //     The Object to compare with the current VertexPositionColorTexture.
        public override bool Equals(object obj)
        {
            if(obj is ExtendedVertex)
            {
                return this == (ExtendedVertex) obj;
            }
            else
            {
                return false;
            }
        }

        //
        // Summary:
        //     Gets the hash code for this instance.
        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        //
        // Summary:
        //     Retrieves a string representation of this object.
        public override string ToString()
        {
            return "Extended Vertex: " + Position.ToString();
        }

        public void Set(Vector3 position, 
                       Color color, 
                       Color vertColor, 
                       Vector2 textureCoordinate, 
                       Vector4 textureBounds)
        {
            Position = position;
            Color = color;
            VertColor = vertColor;
            TextureCoordinate = textureCoordinate;
            TextureBounds = textureBounds;
        }
    }

}