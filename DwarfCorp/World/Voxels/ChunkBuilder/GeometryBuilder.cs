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
        public static Point BlackTile = new Point(12, 0);
        private static short[] QuadIndicies = { 0, 1, 2, 3, 0, 2 };
        private static short[] FlippedQuadIndicies = { 0, 1, 3, 3, 1, 2 };

        public static GeometricPrimitive CreateFromChunk(VoxelChunk Chunk, WorldManager World)
        {
            DebugHelper.AssertNotNull(Chunk);
            DebugHelper.AssertNotNull(World);

            var sliceStack = new List<RawPrimitive>();
            var sliceCache = new SliceCache();

            int maxViewingLevel = World.Renderer.PersistentSettings.MaxViewingLevel;
            var terrainTileSheet = new TerrainTileSheet(512, 512, 32, 32);

            for (var localY = 0; localY < maxViewingLevel - Chunk.Origin.Y && localY < VoxelConstants.ChunkSizeY; ++localY)
            {
                RawPrimitive sliceGeometry = null;
                sliceCache.ClearSliceCache();

                lock (Chunk.Data.SliceCache)
                {
                    var cachedSlice = Chunk.Data.SliceCache[localY];

                    if (cachedSlice != null)
                    {
                        sliceStack.Add(cachedSlice); // Todo: Get rid of the raw primitive / geometric primitive bullshit entirely

                        if (GameSettings.Current.GrassMotes)
                            Chunk.RebuildMoteLayerIfNull(localY);

                        continue;
                    }

                    sliceGeometry = new RawPrimitive();

                    Chunk.Data.SliceCache[localY] = sliceGeometry;
                }

                if (GameSettings.Current.CalculateRamps)
                    PrecomputeVoxelSlopesSlice(World.ChunkManager, Chunk, localY);

                if (GameSettings.Current.GrassMotes)
                    Chunk.RebuildMoteLayer(localY);

                DebugHelper.AssertNotNull(sliceGeometry);
                GenerateSliceGeometry(sliceGeometry, Chunk, localY, terrainTileSheet, World, sliceCache);

                sliceStack.Add(sliceGeometry);
            }

            var chunkGeo = RawPrimitive.Concat(sliceStack);

            var r = new GeometricPrimitive();
            r.Vertices = chunkGeo.Vertices;
            r.VertexCount = chunkGeo.VertexCount;
            r.Indexes = chunkGeo.Indexes.Select(c => (ushort)c).ToArray();
            r.IndexCount = chunkGeo.IndexCount;

            return r;
        }

        private static void GenerateSliceGeometry(
            RawPrimitive Into,
            VoxelChunk Chunk,
            int LocalY,
            TerrainTileSheet TileSheet,
            WorldManager World,
            SliceCache Cache)
        {
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                    GenerateVoxelGeometry(Into, VoxelHandle.UnsafeCreateLocalHandle(Chunk, new LocalVoxelCoordinate(x, LocalY, z)), TileSheet, World, Cache);
        }

        public static void GenerateVoxelGeometry(
            RawPrimitive Into, 
            VoxelHandle Voxel, 
            TerrainTileSheet TileSheet,
            WorldManager World,
            SliceCache Cache)
        {
            BuildDesignationGeometry(Into, Voxel, Cache, TileSheet, World);
            if (Voxel.IsEmpty && Voxel.IsExplored) return;

            var templateSolid = TemplateSolidLibrary.GetTemplateSolid(Voxel.Type.TemplateSolid);

            foreach (var face in templateSolid.Faces)
                GenerateFaceGeometry(Into, Voxel, face, TileSheet, World, Cache);
        }

        public static void GenerateFaceGeometry(
            RawPrimitive Into,
            VoxelHandle Voxel,
            Geo.TemplateFace Face,
            TerrainTileSheet TileSheet,
            WorldManager World,
            SliceCache Cache)
        {
            if (Face.CullType == Geo.FaceCullType.Cull && !IsFaceVisible(Voxel, Face, World.ChunkManager, out var neighbor))
                return;

            if (Face.Orientation == FaceOrientation.Top)
            {
                if (Voxel.GrassType != 0)
                {
                    var decalType = Library.GetGrassType(Voxel.GrassType);
                    PrepVerticies(World, Voxel, Face, Cache, GetVoxelVertexExploredNeighbors(Voxel, Face, Cache), TileSheet, decalType.Tile, true, 0.0f);

                    GenerateGrassFringe(Into, Voxel, Face, TileSheet, Cache, decalType);
                }
                else
                    PrepVerticies(World, Voxel, Face, Cache, GetVoxelVertexExploredNeighbors(Voxel, Face, Cache), TileSheet, SelectTile(Voxel.Type, Face.Orientation), true, 0.0f);

                AddQuad(Into, Cache.FaceGeometry, QuadIndicies);

                if (Voxel.DecalType != 0)
                {
                    var decalType = Library.GetDecalType(Voxel.DecalType);
                    PrepVerticies(World, Voxel, Face, Cache, GetVoxelVertexExploredNeighbors(Voxel, Face, Cache), TileSheet, decalType.Tile, true, 0.02f);
                    AddQuad(Into, Cache.FaceGeometry, QuadIndicies);
                }
            }
            else
            {
                PrepVerticies(World, Voxel, Face, Cache, GetVoxelVertexExploredNeighbors(Voxel, Face, Cache), TileSheet, SelectTile(Voxel.Type, Face.Orientation), true, 0.0f);
                AddQuad(Into, Cache.FaceGeometry, QuadIndicies);
            }
        }

        private static void SetVertexTint(ExtendedVertex[] Verticies, Color Tint)
        {
            for (var i = 0; i < Verticies.Length; ++i)
                Verticies[i].VertColor = Tint;
        }

        private static void BuildDesignationGeometry(
            RawPrimitive Into,
            VoxelHandle Voxel,
            SliceCache Cache,
            TerrainTileSheet TileSheet,
            WorldManager World)
        {
            // Todo: Store designations per chunk.
            foreach (var designation in World.PersistentData.Designations.EnumerateDesignations(Voxel).ToList())
            {
                if ((designation.Type & World.Renderer.PersistentSettings.VisibleTypes) != designation.Type) // If hidden by player, do not draw.
                    return;

                var designationProperties = Library.GetDesignationTypeProperties(designation.Type).Value;
                var designationVisible = false;

                if (designationProperties.DrawType == DesignationDrawType.PreviewVoxel)
                    designationVisible = Voxel.Coordinate.Y < World.Renderer.PersistentSettings.MaxViewingLevel;
                else
                    designationVisible = VoxelHelpers.DoesVoxelHaveVisibleSurface(World, Voxel);

                if (designationVisible
                    && Library.GetVoxelPrimitive(Library.DesignationVoxelType).HasValue(out BoxPrimitive designationPrimitive))
                {
                    switch (designationProperties.DrawType)
                    {
                        case DesignationDrawType.FullBox:
                            {
                                var solid = TemplateSolidLibrary.GetTemplateSolid(Voxel.Type.TemplateSolid);
                                for (var f = 0; f < solid.Faces.Count; ++f)
                                {
                                    var face = solid.Faces[f];
                                    if (face.CullType == Geo.FaceCullType.Cull && !IsFaceVisible(Voxel, face, World.ChunkManager, out var neighbor))
                                        continue;

                                    PrepVerticies(World, Voxel, face, Cache, GetVoxelVertexExploredNeighbors(Voxel, face, Cache), TileSheet, new Point(0, 0), false, 0.02f);
                                    SetVertexTint(Cache.FaceGeometry, designationProperties.Color);
                                    AddQuad(Into, Cache.FaceGeometry, QuadIndicies);
                                }
                            }
                            break;

                        case DesignationDrawType.TopBox:
                            {
                                var solid = TemplateSolidLibrary.GetTemplateSolid(Voxel.Type.TemplateSolid);
                                for (var f = 0; f < solid.Faces.Count; ++f)
                                {
                                    var face = solid.Faces[f];
                                    if (face.Orientation != FaceOrientation.Top)
                                        continue;
                                    if (face.CullType == Geo.FaceCullType.Cull && !IsFaceVisible(Voxel, face, World.ChunkManager, out var neighbor))
                                        continue;

                                    PrepVerticies(World, Voxel, face, Cache, GetVoxelVertexExploredNeighbors(Voxel, face, Cache), TileSheet, new Point(0, 0), false, 0.02f);
                                    SetVertexTint(Cache.FaceGeometry, designationProperties.Color);
                                    AddQuad(Into, Cache.FaceGeometry, QuadIndicies);
                                }
                            }
                            break;

                        case DesignationDrawType.PreviewVoxel:
                            {
                                if (Library.GetVoxelType(designation.Tag.ToString()).HasValue(out VoxelType voxelType))
                                {
                                    var solid = TemplateSolidLibrary.GetTemplateSolid(voxelType.TemplateSolid);
                                    for (var f = 0; f < solid.Faces.Count; ++f)
                                    {
                                        var face = solid.Faces[f];
                                        if (face.CullType == Geo.FaceCullType.Cull && !IsFaceVisible(Voxel, face, World.ChunkManager, out var neighbor))
                                            continue;

                                        PrepVerticies(World, Voxel, face, Cache, GetVoxelVertexExploredNeighbors(Voxel, face, Cache), TileSheet, SelectTile(voxelType, face.Orientation), false, 0.0f);
                                        SetVertexTint(Cache.FaceGeometry, designationProperties.Color);
                                        AddQuad(Into, Cache.FaceGeometry, QuadIndicies);
                                    }
                                }
                            }
                            break;
                    }

                }
            }
        }

        private static void PrepVerticies(
            WorldManager World,
            VoxelHandle Voxel,
            Geo.TemplateFace Face,
            SliceCache Cache,
            int ExploredVertexCount,
            TerrainTileSheet TileSheet,
            Point Tile,
            bool ApplyLighting,
            float explodeOffset)
        {
            for (var vertex = 0; vertex < Face.Mesh.VertexCount; ++vertex) // Blows up if face has more than 4 verticies.
            {
                var lighting = new VertexLighting.VertexColorInfo { AmbientColor = 255, DynamicColor = 255, SunColor = 255 };

                if (ApplyLighting)
                    lighting = VertexLighting.CalculateVertexLight(Voxel, Face.Mesh.Verticies[vertex].LogicalVertex, World.ChunkManager, Cache);

                var slopeOffset = Vector3.Zero;
                if (Face.Mesh.Verticies[vertex].ApplySlope && ShouldSlope(Face.Mesh.Verticies[vertex].LogicalVertex, Voxel))
                    slopeOffset = new Vector3(0.0f, -0.5f, 0.0f);

                var voxelPosition = Face.Mesh.Verticies[vertex].Position + slopeOffset + Voxel.WorldPosition;
                voxelPosition += VertexNoise.GetNoiseVectorFromRepeatingTexture(voxelPosition);
                voxelPosition += explodeOffset * OrientationHelper.GetFaceNeighborOffset(Face.Orientation).AsVector3();

                if (ExploredVertexCount == 0)
                {
                    Cache.FaceGeometry[vertex] = new ExtendedVertex
                    {
                        Position = voxelPosition,
                        TextureCoordinate = TileSheet.MapTileUVs(Face.Mesh.Verticies[vertex].TextureCoordinate, BlackTile),
                        TextureBounds = TileSheet.GetTileBounds(BlackTile),
                        VertColor = new Color(0.0f, 0.0f, 0.0f, 1.0f),
                        Color = new Color(0.0f, 0.0f, 0.0f, 1.0f)
                    };
                }
                else
                {
                    var anyNeighborExplored = true;
                    if (!Cache.ExploredCache.TryGetValue(SliceCache.GetCacheKey(Voxel, Face.Mesh.Verticies[vertex].LogicalVertex), out anyNeighborExplored))
                        anyNeighborExplored = true;

                    Cache.FaceGeometry[vertex] = new ExtendedVertex
                    {
                        Position = voxelPosition,
                        TextureCoordinate = TileSheet.MapTileUVs(Face.Mesh.Verticies[vertex].TextureCoordinate, Tile),
                        TextureBounds = TileSheet.GetTileBounds(Tile),
                        VertColor = anyNeighborExplored ? new Color(1.0f, 1.0f, 1.0f, 1.0f) : new Color(0.0f, 0.0f, 0.0f, 1.0f),
                        Color = lighting.AsColor()
                    };
                }
            }
        }

        private static void AddQuad(
            RawPrimitive Into,
            ExtendedVertex[] Verticies,
            short[] Indicies)
        {
            Into.AddOffsetIndicies(Indicies, Into.VertexCount, 6);
            for (var vertex = 0; vertex < 4; ++vertex)
                Into.AddVertex(Verticies[vertex]);
        }
    }
}