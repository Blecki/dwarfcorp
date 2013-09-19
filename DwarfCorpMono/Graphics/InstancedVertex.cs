using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class InstanceData
    {
        public Matrix Transform { get; set; }
        public Color Color { get; set; }
        public  uint ID {  get;  set; }
        private static uint maxID = 0;
        public bool ShouldDraw { get; set; }
        public float Depth { get; set; }

        public InstanceData(Matrix world, Color colour, bool shouldDraw)
        {
            Transform = world;
            Color = colour;
            ID = maxID;
            maxID++;
            ShouldDraw = shouldDraw;
            Depth = 0.0f;
        }
    }

    public struct InstancedVertex
    {
        public Matrix Transform;
        public Color Color;


        public InstancedVertex(Matrix world, Color colour)
        {
            Transform = world;
            Color = colour;
        }

        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
            // World Matrix Data
                 new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
                 new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
                 new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
                 new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3),
            //Colour Data
                 new VertexElement(64, VertexElementFormat.Color, VertexElementUsage.Color, 1)


            );
    }
}
