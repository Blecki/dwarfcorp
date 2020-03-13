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
                        Mesh = Geo.Mesh.Quad(bottomWestSouth, topWestSouth, bottomEastSouth, topEastSouth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.North,
                        Mesh = Geo.Mesh.Quad(bottomEastNorth, topEastNorth, bottomWestNorth, topWestNorth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.Top,
                        Mesh = Geo.Mesh.Quad(topWestSouth, topWestNorth, topEastSouth, topEastNorth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.Bottom,
                        Mesh = Geo.Mesh.Quad(bottomEastNorth, bottomEastSouth, bottomWestNorth, bottomWestSouth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.West,
                        Mesh = Geo.Mesh.Quad(bottomWestNorth, topWestNorth, bottomWestSouth, topWestSouth)
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.East,
                        Mesh = Geo.Mesh.Quad(bottomEastSouth, topEastSouth, bottomEastNorth, topEastNorth)
                    }
                }
            };
        }

        public static VoxelShapeTemplate MakeVoxelCube(Point TopTile, Point SideTile, Point BottomTile, Gui.TileSheet Sheet)
        {
            var r = VoxelShapeTemplate.MakeCube();

            foreach (var face in r.Faces)
                if (face.Orientation == FaceOrientation.Top)
                    face.Mesh.EntireMeshAsPart().Texture(Sheet.TileMatrix(TopTile.X, TopTile.Y)); // Need to set texture bounds as well.
                else if (face.Orientation == FaceOrientation.Bottom)
                    face.Mesh.EntireMeshAsPart().Texture(Sheet.TileMatrix(BottomTile.X, BottomTile.Y)); // Need to set texture bounds as well.
                else
                    face.Mesh.EntireMeshAsPart().Texture(Sheet.TileMatrix(SideTile.X, SideTile.Y)); // Need to set texture bounds as well.

            return r;
        }
    }
}