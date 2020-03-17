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
        public static VoxelShapeTemplate Cube;
        public static Point BlackTile = new Point(12, 0);

        public static GeometricPrimitive CreateFromChunk(VoxelChunk Chunk, WorldManager World)
        {
            if (Cube == null)
                Cube = VoxelShapeTemplate.MakeCube();

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

                    Chunk.Data.SliceCache[localY] = sliceGeometry; // Copying it in means our additions later won't take, doesn't it?
                }

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
                    GenerateVoxelGeometry(Into, VoxelHandle.UnsafeCreateLocalHandle(Chunk, new LocalVoxelCoordinate(x, LocalY, z)), TileSheet, World, Cache, true);
        }

        public static void GenerateVoxelGeometry(
            RawPrimitive Into, 
            VoxelHandle Voxel, 
            TerrainTileSheet TileSheet,
            WorldManager World,
            SliceCache Cache,
            bool ApplyLighting)
        {
            if (Voxel.IsEmpty && Voxel.IsExplored) return;

            Cache.ClearVoxelCache();

            var voxelTransform = Matrix.CreateTranslation(Voxel.Coordinate.ToVector3());

            foreach (var face in Cube.Faces)
                if (face.Orientation == FaceOrientation.Top)
                    GenerateTopFaceGeometry(Into, Voxel, face, TileSheet, voxelTransform, World, Cache, ApplyLighting);
                else
                    GenerateFaceGeometry(Into, Voxel, face, TileSheet, voxelTransform, World, Cache, ApplyLighting);
        }

        public static void GenerateFaceGeometry(
            RawPrimitive Into,
            VoxelHandle Voxel,
            VoxelFaceTemplate Face,
            TerrainTileSheet TileSheet,
            Matrix VoxelTransform,
            WorldManager World,
            SliceCache Cache,
            bool ApplyLighting)
        {
            if (Face.CullType == FaceCullType.Cull && !IsFaceVisible(Voxel, Face, World.ChunkManager, out var neighbor))
                return;

            AddFaceGeometry(Into, World, Voxel, Face, Cache, GetVoxelVertexExploredNeighbors(Voxel, Face, Cache),
                TileSheet, SelectTile(Voxel.Type, Face.Orientation), ApplyLighting);
        }

        public static void GenerateTopFaceGeometry(
            RawPrimitive Into,
            VoxelHandle Voxel,
            VoxelFaceTemplate Face,
            TerrainTileSheet TileSheet,
            Matrix VoxelTransform,
            WorldManager World,
            SliceCache Cache,
            bool ApplyLighting)
        {
            if (Face.CullType == FaceCullType.Cull && !IsFaceVisible(Voxel, Face, World.ChunkManager, out var neighbor))
                return;

            if (Voxel.GrassType != 0)
            {
                var decalType = Library.GetGrassType(Voxel.GrassType);
                AddFaceGeometry(Into, World, Voxel, Face, Cache, GetVoxelVertexExploredNeighbors(Voxel, Face, Cache),
                    TileSheet, decalType.Tile, ApplyLighting);

                // Add grass fringe.
            }
            else
                AddFaceGeometry(Into, World, Voxel, Face, Cache, GetVoxelVertexExploredNeighbors(Voxel, Face, Cache),
                    TileSheet, SelectTile(Voxel.Type, Face.Orientation), ApplyLighting);
        }

        private static void AddFaceGeometry(
            RawPrimitive Into,
            WorldManager World,
            VoxelHandle Voxel,
            VoxelFaceTemplate Face,
            SliceCache Cache,
            int ExploredVertexCount,
            TerrainTileSheet TileSheet,
            Point Tile,
            bool ApplyLighting)
        {
            var indexBase = Into.VertexCount;
            var exploredVerts = GetVoxelVertexExploredNeighbors(Voxel, Face, Cache);

            for (var vertex = 0; vertex < Face.Mesh.VertexCount; ++vertex)
            {
                var lighting = new VertexLighting.VertexColorInfo { AmbientColor = 255, DynamicColor = 255, SunColor = 255 };

                if (ApplyLighting)
                    lighting = VertexLighting.CalculateVertexLight(Voxel, Face.Mesh.Verticies[vertex].LogicalVertex, World.ChunkManager, Cache);

                var slopeOffset = Vector3.Zero;
                if (Face.Mesh.Verticies[vertex].ApplySlope && ShouldVoxelVertexSlope(World.ChunkManager, Voxel, Face.Mesh.Verticies[vertex].LogicalVertex, Cache))
                    slopeOffset = new Vector3(0.0f, -0.5f, 0.0f);

                Cache.AmbientValues[vertex] = lighting.AmbientColor; // Todo: Use ambient to flip indicies.

                if (ExploredVertexCount == 0)
                {
                    Into.AddVertex(new ExtendedVertex
                    {
                        Position = Face.Mesh.Verticies[vertex].Position + slopeOffset + Voxel.WorldPosition,
                        TextureCoordinate = TileSheet.MapTileUVs(Face.Mesh.Verticies[vertex].TextureCoordinate, BlackTile),
                        TextureBounds = TileSheet.GetTileBounds(BlackTile),
                        VertColor = new Color(0.0f, 0.0f, 0.0f, 1.0f),
                        Color = new Color(0.0f, 0.0f, 0.0f, 1.0f)
                    });
                }
                else
                {
                    var anyNeighborExplored = true;
                    if (!Cache.ExploredCache.TryGetValue(SliceCache.GetCacheKey(Voxel, Face.Mesh.Verticies[vertex].LogicalVertex), out anyNeighborExplored))
                        anyNeighborExplored = true;

                    Into.AddVertex(new ExtendedVertex
                    {
                        Position = Face.Mesh.Verticies[vertex].Position + slopeOffset + Voxel.WorldPosition,
                        TextureCoordinate = TileSheet.MapTileUVs(Face.Mesh.Verticies[vertex].TextureCoordinate, Tile),
                        TextureBounds = TileSheet.GetTileBounds(Tile),
                        VertColor = anyNeighborExplored ? new Color(1.0f, 1.0f, 1.0f, 1.0f) : new Color(0.0f, 0.0f, 0.0f, 1.0f),
                        Color = lighting.AsColor()
                    });
                }
            }

            // Todo: Flip indicies based on cached ambient.
            Into.AddOffsetIndicies(Face.Mesh.Indicies, indexBase, Face.Mesh.IndexCount);
        }

    }
}