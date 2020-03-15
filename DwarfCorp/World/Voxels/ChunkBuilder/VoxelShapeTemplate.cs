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
    public class VoxelShapeTemplate
    {
        public List<VoxelFaceTemplate> Faces = new List<VoxelFaceTemplate>();

        public static VoxelShapeTemplate MakeCube()
        {
            Vector3 topWestSouth = new Vector3(1.0f, 1.0f, 0.0f);
            Vector3 topWestNorth = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 topEastSouth = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 topEastNorth = new Vector3(0.0f, 1.0f, 1.0f);
            Vector3 bottomWestSouth = new Vector3(1.0f, 0.0f, 0.0f);
            Vector3 bottomWestNorth = new Vector3(1.0f, 0.0f, 1.0f);
            Vector3 bottomEastSouth = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 bottomEastNorth = new Vector3(0.0f, 0.0f, 1.0f);

            return new VoxelShapeTemplate
            {
                Faces = new List<VoxelFaceTemplate>
                {
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.South,
                        Mesh = Geo.TemplateMesh.Quad(bottomWestSouth, topWestSouth, bottomEastSouth, topEastSouth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.North,
                        Mesh = Geo.TemplateMesh.Quad(bottomEastNorth, topEastNorth, bottomWestNorth, topWestNorth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.Top,
                        Mesh = Geo.TemplateMesh.Quad(topWestSouth, topWestNorth, topEastSouth, topEastNorth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.Bottom,
                        Mesh = Geo.TemplateMesh.Quad(bottomEastNorth, bottomEastSouth, bottomWestNorth, bottomWestSouth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.West,
                        Mesh = Geo.TemplateMesh.Quad(bottomWestNorth, topWestNorth, bottomWestSouth, topWestSouth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.East,
                        Mesh = Geo.TemplateMesh.Quad(bottomEastSouth, topEastSouth, bottomEastNorth, topEastNorth)
                    }
                }
            };
        }
    }
}