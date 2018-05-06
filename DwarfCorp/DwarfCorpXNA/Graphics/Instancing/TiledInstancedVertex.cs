using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// Extends the vertex declaration to support which instance the vertex
    /// refers to. The GPU draws instances based on this information.
    /// </summary>
    public struct TiledInstancedVertex
    {
        public Matrix Transform;
        public Color Color;
        public Color SelectionBufferColor;
        public Vector4 TileBounds;

        public TiledInstancedVertex(Matrix world, Color colour, Color selectionColor, Vector4 TileBounds)
        {
            Transform = world;
            Color = colour;
            SelectionBufferColor = selectionColor;
            this.TileBounds = TileBounds;
        }

        public static int SizeOf<T>(T obj)
        {
            return System.Runtime.InteropServices.Marshal.SizeOf(obj);
        }


        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
            // World Matrix Data
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(SizeOf(Vector4.One), VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(2 * SizeOf(Vector4.One), VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(3 * SizeOf(Vector4.One), VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3),
            //Colour Data
            new VertexElement(4 * SizeOf(Vector4.One), VertexElementFormat.Color, VertexElementUsage.Color, 2),
            new VertexElement(4 * SizeOf(Vector4.One) + SizeOf(Microsoft.Xna.Framework.Color.White), VertexElementFormat.Color, VertexElementUsage.Color, 3),

            new VertexElement(4 * SizeOf(Vector4.One) + SizeOf(Color.White) + SizeOf(Color.White), VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 5)
            );
    }

}