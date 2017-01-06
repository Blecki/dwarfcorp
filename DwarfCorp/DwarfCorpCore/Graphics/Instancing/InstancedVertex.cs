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
    public struct InstancedVertex
    {
        public Matrix Transform;
        public Color Color;


        public InstancedVertex(Matrix world, Color colour)
        {
            Transform = world;
            Color = colour;
        }

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
            // World Matrix Data
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3),
            //Colour Data
            new VertexElement(64, VertexElementFormat.Color, VertexElementUsage.Color, 2)
            );
    }

}