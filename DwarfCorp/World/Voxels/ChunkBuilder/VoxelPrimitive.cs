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
            Vector3 topWestSouth = new Vector3(1.0f, 1.0f, 0.0f);
            Vector3 topWestNorth = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 topEastSouth = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 topEastNorth = new Vector3(0.0f, 1.0f, 1.0f);
            Vector3 bottomWestSouth = new Vector3(1.0f, 0.0f, 0.0f);
            Vector3 bottomWestNorth = new Vector3(1.0f, 0.0f, 1.0f);
            Vector3 bottomEastSouth = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 bottomEastNorth = new Vector3(0.0f, 0.0f, 1.0f);

            return new VoxelPrimitive
            {
                Faces = new List<Face>
                {
                    new Face
                    {
                        Orientation = FaceOrientation.South,
                        Mesh = Geo.Mesh.Quad(bottomWestSouth, topWestSouth, bottomEastSouth, topEastSouth)
                    },
                    new Face
                    {
                        Orientation = FaceOrientation.North,
                        Mesh = Geo.Mesh.Quad(bottomEastNorth, topEastNorth, bottomWestNorth, topWestNorth)
                    },
                    new Face
                    {
                        Orientation = FaceOrientation.Top,
                        Mesh = Geo.Mesh.Quad(topWestSouth, topWestNorth, topEastSouth, topEastNorth)
                    },
                    new Face
                    {
                        Orientation = FaceOrientation.Bottom,
                        Mesh = Geo.Mesh.Quad(bottomEastNorth, bottomEastSouth, bottomWestNorth, bottomWestSouth)
                    },
                    new Face
                    {
                        Orientation = FaceOrientation.West,
                        Mesh = Geo.Mesh.Quad(bottomWestNorth, topWestNorth, bottomWestSouth, topWestSouth)
                    },
                    new Face
                    {
                        Orientation = FaceOrientation.East,
                        Mesh = Geo.Mesh.Quad(bottomEastSouth, topEastSouth, bottomEastNorth, topEastNorth)
                    }
                }
            };
        }
    }
}