using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Voxels
{
    public enum FaceCullType
    {
        Cull,
        CannotCull
    }

    public class Face
    {
        public Geo.Mesh Mesh;
        public FaceOrientation Orientation;
        public FaceCullType CullType = FaceCullType.Cull;
    }
}