using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Voxels.Geo
{
    /// <summary>
    /// Abstract 'slice' of a mesh - allows manipulation of a certain range of verticies.
    /// </summary>
    public partial class TemplateMeshPart
    {
        public int VertexOffset = 0;
        public int VertexCount = 0;
        public TemplateMesh Mesh = null;

        public void End()
        {
            this.VertexCount = Mesh.VertexCount - this.VertexOffset;
        }
    }
}