using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")] public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
            //Position
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            // Texture Coordinate
            new VertexElement(SizeOf(Vector3.Zero), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            // Lightmap coordinate
            new VertexElement(SizeOf(Vector3.Zero) + SizeOf(Vector2.Zero), VertexElementFormat.Vector2,
                VertexElementUsage.TextureCoordinate, 2),
            // Color
            new VertexElement(SizeOf(Vector3.Zero) + SizeOf(Vector2.Zero)*2, VertexElementFormat.Color,
                VertexElementUsage.Color, 0),
            // Vertex tint
            new VertexElement(SizeOf(Vector3.Zero) + SizeOf(Vector2.Zero)*2 + SizeOf(Color.White),
                VertexElementFormat.Color, VertexElementUsage.Color, 1),
            // Texture bounds
            new VertexElement(SizeOf(Vector3.Zero) + SizeOf(Vector2.Zero)*2 + SizeOf(Color.White)*2,
                VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
            // Lightmap bounds
            new VertexElement(
                SizeOf(Vector3.Zero) + SizeOf(Vector2.Zero)*2 + SizeOf(Color.White)*2 + SizeOf(Vector4.Zero),
                VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3)
            );

        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Color Color;
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Vector4 LightmapBounds;

        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Vector2
            LightmapCoordinate;

        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Vector3 Position;
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Vector4 TextureBounds;
        //
        // Summary:
        //     UV texture coordinates.
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Vector2
            TextureCoordinate;

        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")] public Color VertColor;

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
        public ExtendedVertex(Vector3 position, Color color, Color vertColor, Vector2 textureCoordinate,
            Vector4 textureBounds)
        {
            Position = position;
            Color = color;
            VertColor = vertColor;
            TextureCoordinate = textureCoordinate;
            TextureBounds = textureBounds;
            LightmapCoordinate = Vector2.Zero;
            LightmapBounds = Vector4.Zero;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        public static int SizeOf<T>(T obj)
        {
            return Marshal.SizeOf(obj);
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
                     && left.VertColor == right.VertColor
                     && left.LightmapCoordinate == right.LightmapCoordinate
                     && left.LightmapBounds == right.LightmapBounds);
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
            if (obj is ExtendedVertex)
            {
                return this == (ExtendedVertex) obj;
            }
            return false;
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
            return "Extended Vertex: " + Position;
        }

        public void Set(Vector3 position,
            Color color,
            Color vertColor,
            Vector2 textureCoordinate,
            Vector4 textureBounds,
            Vector2 lightmapCoordinates,
            Vector4 lightmapBounds)
        {
            Position = position;
            Color = color;
            VertColor = vertColor;
            TextureCoordinate = textureCoordinate;
            TextureBounds = textureBounds;
            LightmapCoordinate = lightmapCoordinates;
            LightmapBounds = lightmapBounds;
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