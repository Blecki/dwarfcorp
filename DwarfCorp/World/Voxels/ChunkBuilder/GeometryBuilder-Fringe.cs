using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp.Voxels
{
    public static partial class GeometryBuilder
    {
        private static void GenerateGrassFringe(RawPrimitive Into, VoxelHandle Voxel, Geo.TemplateFace Face, TerrainTileSheet TileSheet, SliceCache Cache, GrassType decalType)
        {
            for (var i = 0; i < 4; ++i)
                Cache.EdgeFringeTempVerticies[i].Set = false;

            if (Face.Edges != null)
                for (var e = 0; e < Face.Edges.Length; ++e)
                {
                    var fringeNeighbor = VoxelHelpers.GetNeighbor(Voxel, OrientationHelper.GetFaceNeighborOffset(Face.Edges[e].Orientation));
                    if (fringeNeighbor.IsValid)
                    {
                        if (fringeNeighbor.IsEmpty)
                            GenerateEdgeFringe(Into, Face, e, TileSheet, Cache, decalType, new Vector3(0.0f, -0.5f, 0.0f), 0.5f);
                        else
                        {
                            var above = VoxelHelpers.GetVoxelAbove(fringeNeighbor);
                            if (above.IsValid && !above.IsEmpty)
                                GenerateEdgeFringe(Into, Face, e, TileSheet, Cache, decalType, new Vector3(0.0f, 0.5f, 0.0f), -0.1f);
                            else if (fringeNeighbor.GrassType == 0 || (fringeNeighbor.GrassType != Voxel.GrassType && Library.GetGrassType(fringeNeighbor.GrassType).FringePrecedence < decalType.FringePrecedence))
                                GenerateEdgeFringe(Into, Face, e, TileSheet, Cache, decalType, new Vector3(0.0f, 0.1f, 0.0f), 0.5f);
                        }
                    }
                }

            if (Face.Corners != null)
                for (var c = 0; c < Face.Corners.Length; ++c)
                {
                    var cornerNeighbor = VoxelHelpers.GetNeighbor(Voxel, OrientationHelper.GetFaceNeighborOffset(Face.Edges[Face.Corners[c].EdgeA].Orientation) + OrientationHelper.GetFaceNeighborOffset(Face.Edges[Face.Corners[c].EdgeB].Orientation));
                    if (cornerNeighbor.IsValid)
                    {
                        if (cornerNeighbor.IsEmpty) // Todo: Do not generate fringe if horizontal neighbors are occupied. 
                            // Todo: Also - needs to use corners set by edge fringe when they are at different heights. Maybe specify which edges the corner is in between?
                            GenerateHangingCornerFringe(Into, Face, TileSheet, Cache, decalType, Face.Corners[c], new Vector3(0.0f, -0.5f, 0.0f));
                        else
                        {
                            var above = VoxelHelpers.GetVoxelAbove(cornerNeighbor);
                            if (above.IsValid && !above.IsEmpty)
                            { }
                            else if (cornerNeighbor.GrassType == 0 || (cornerNeighbor.GrassType != Voxel.GrassType && Library.GetGrassType(cornerNeighbor.GrassType).FringePrecedence < decalType.FringePrecedence))
                                GenerateHangingCornerFringe(Into, Face, TileSheet, Cache, decalType, Face.Corners[c], new Vector3(0.0f, 0.1f, 0.0f));
                        }
                    }
                }
        }

        private static void GenerateEdgeFringe(
            RawPrimitive Into, 
            Geo.TemplateFace Face, 
            int EdgeIndex,
            TerrainTileSheet TileSheet, 
            SliceCache Cache, 
            GrassType decalType, 
            Vector3 Sag,
            float Scale)
        {
            var start = Cache.FaceGeometry[Face.Edges[EdgeIndex].Start];
            var end = Cache.FaceGeometry[Face.Edges[EdgeIndex].End];
            Cache.EdgeFringeTempVerticies[EdgeIndex].Set = true;

            Cache.TempVerticies[0] = new ExtendedVertex(
                start.Position,
                start.Color, start.VertColor,
                TileSheet.MapTileUVs(new Vector2(0.0f, 0.0f), decalType.FringeTiles[0]), TileSheet.GetTileBounds(decalType.FringeTiles[0]));

            Cache.TempVerticies[1] = new ExtendedVertex(
                end.Position,
                end.Color, end.VertColor,
                TileSheet.MapTileUVs(new Vector2(1.0f, 0.0f), decalType.FringeTiles[0]), TileSheet.GetTileBounds(decalType.FringeTiles[0]));

            Cache.EdgeFringeTempVerticies[EdgeIndex].End = end.Position + OrientationHelper.GetFaceNeighborOffset(Face.Edges[EdgeIndex].Orientation).AsVector3() * Scale + Sag;
            Cache.TempVerticies[2] = new ExtendedVertex(
                Cache.EdgeFringeTempVerticies[EdgeIndex].End,
                end.Color, end.VertColor,
                TileSheet.MapTileUVs(new Vector2(1.0f, 0.5f), decalType.FringeTiles[0]), TileSheet.GetTileBounds(decalType.FringeTiles[0]));

            Cache.EdgeFringeTempVerticies[EdgeIndex].Start = start.Position + OrientationHelper.GetFaceNeighborOffset(Face.Edges[EdgeIndex].Orientation).AsVector3() * Scale + Sag;
            Cache.TempVerticies[3] = new ExtendedVertex(
                Cache.EdgeFringeTempVerticies[EdgeIndex].Start,
                start.Color, start.VertColor,
                TileSheet.MapTileUVs(new Vector2(0.0f, 0.5f), decalType.FringeTiles[0]), TileSheet.GetTileBounds(decalType.FringeTiles[0]));

            AddQuad(Into, Cache.TempVerticies, QuadIndicies);
        }

        private static void GenerateHangingCornerFringe(
            RawPrimitive Into, 
            Geo.TemplateFace Face, 
            TerrainTileSheet TileSheet, 
            SliceCache Cache, 
            GrassType decalType, 
            Geo.TemplateCorner Corner, 
            Vector3 Sag)
        {
            var vertex = Cache.FaceGeometry[Corner.Vertex];

            Cache.TempVerticies[0] = new ExtendedVertex(
                vertex.Position,
                vertex.Color, vertex.VertColor,
                TileSheet.MapTileUVs(new Vector2(0.0f, 0.0f), decalType.FringeTiles[2]), TileSheet.GetTileBounds(decalType.FringeTiles[2]));

            Cache.TempVerticies[1] = new ExtendedVertex(
                Cache.EdgeFringeTempVerticies[Corner.EdgeA].Set ? Cache.EdgeFringeTempVerticies[Corner.EdgeA].End : 
                    (vertex.Position 
                        + OrientationHelper.GetFaceNeighborOffset(Face.Edges[Corner.EdgeA].Orientation).AsVector3() * 0.5f
                        + Sag),
                vertex.Color, vertex.VertColor,
                TileSheet.MapTileUVs(new Vector2(0.5f, 0.0f), decalType.FringeTiles[2]), TileSheet.GetTileBounds(decalType.FringeTiles[2]));

            Cache.TempVerticies[2] = new ExtendedVertex(
                vertex.Position 
                    + OrientationHelper.GetFaceNeighborOffset(Face.Edges[Corner.EdgeA].Orientation).AsVector3() * 0.5f
                    + OrientationHelper.GetFaceNeighborOffset(Face.Edges[Corner.EdgeB].Orientation).AsVector3() * 0.5f
                    + Sag,
                vertex.Color, vertex.VertColor,
                TileSheet.MapTileUVs(new Vector2(0.5f, 0.5f), decalType.FringeTiles[2]), TileSheet.GetTileBounds(decalType.FringeTiles[2]));

            Cache.TempVerticies[3] = new ExtendedVertex(
                Cache.EdgeFringeTempVerticies[Corner.EdgeB].Set ? Cache.EdgeFringeTempVerticies[Corner.EdgeB].Start :
                    (vertex.Position
                        + OrientationHelper.GetFaceNeighborOffset(Face.Edges[Corner.EdgeB].Orientation).AsVector3() * 0.5f
                        + Sag),
                vertex.Color, vertex.VertColor,
                TileSheet.MapTileUVs(new Vector2(0.0f, 0.5f), decalType.FringeTiles[2]), TileSheet.GetTileBounds(decalType.FringeTiles[2]));

            AddQuad(Into, Cache.TempVerticies, QuadIndicies);
        }

    }
}