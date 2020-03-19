using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Voxels.Geo
{
    public enum FaceCullType
    {
        Cull,
        CannotCull
    }

    public enum EdgeType
    {
        Interior,
        Exterior
    }

    public class TemplateEdge
    {
        public int Start;
        public int End;
        public EdgeType EdgeType;
        public FaceOrientation Orientation;
        public int FringeUVs = 0;
    }

    public class TemplateFace
    {
        public TemplateMesh Mesh;
        public FaceOrientation Orientation;
        public FaceCullType CullType = FaceCullType.Cull;
        public TemplateEdge[] Edges;
    }
}