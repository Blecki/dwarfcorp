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
    public class VoxelPrimitive
    {
        public List<Face> Faces = new List<Face>();

        public static VoxelPrimitive MakeCube()
        {
            return new VoxelPrimitive
            {
                Faces = new List<Face>
                {
                    new Face
                    {
                        Orientation = FaceOrientation.Top,
                        Mesh = Geo.Mesh.Quad()
                    }
                }
            };
        }
    }
}