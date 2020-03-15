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

        public static GeometricPrimitive CreateFromChunk(VoxelChunk Chunk, WorldManager World)
        {
            if (Cube == null)
                Cube = VoxelShapeTemplate.MakeCube();

            DebugHelper.AssertNotNull(Chunk);
            DebugHelper.AssertNotNull(World);

            var sliceStack = new List<RawPrimitive>();

            int maxViewingLevel = World.Renderer.PersistentSettings.MaxViewingLevel;
            var terrainTileSheet = new TerrainTileSheet(512, 512, 32, 32);

            for (var localY = 0; localY < maxViewingLevel - Chunk.Origin.Y && localY < VoxelConstants.ChunkSizeY; ++localY)
            {
                RawPrimitive sliceGeometry = null;

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
                GenerateSliceGeometry(sliceGeometry, Chunk, localY, terrainTileSheet, World);

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
            WorldManager World)
        {
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                    GenerateVoxelGeometry(Into, VoxelHandle.UnsafeCreateLocalHandle(Chunk, new LocalVoxelCoordinate(x, LocalY, z)), TileSheet, World);
        }

        public static void GenerateVoxelGeometry(
            RawPrimitive Into, 
            VoxelHandle Voxel, 
            TerrainTileSheet TileSheet,
            WorldManager World)
        {
            if (Voxel.IsEmpty) return;

            var voxelTransform = Matrix.CreateTranslation(Voxel.Coordinate.ToVector3());

                foreach (var face in Cube.Faces)
                    GenerateFaceGeometry(Into, Voxel, face, TileSheet, voxelTransform, World);
        }

        public static void GenerateFaceGeometry(
            RawPrimitive Into,
            VoxelHandle Voxel,
            VoxelFaceTemplate Face,
            TerrainTileSheet TileSheet,
            Matrix VoxelTransform,
            WorldManager World)
        {
            if (Face.CullType == FaceCullType.Cull)
            {
                var neighborVoxel = World.ChunkManager.CreateVoxelHandle(Voxel.Coordinate + OrientationHelper.GetFaceNeighborOffset(Face.Orientation));
                if (neighborVoxel.IsValid && !neighborVoxel.IsEmpty)
                    return;
            }

            Into.AddOffsetIndicies(Face.Mesh.Indicies, Into.VertexCount, Face.Mesh.IndexCount);
            var tile = SelectTile(Voxel.Type, Face.Orientation);

            for (var vertex = 0; vertex < Face.Mesh.VertexCount; ++vertex)
                Into.AddVertex(new ExtendedVertex
                {
                    Position = Vector3.Transform(Face.Mesh.Verticies[vertex].Position, VoxelTransform),
                    TextureCoordinate = TileSheet.MapTileUVs(Face.Mesh.Verticies[vertex].TextureCoordinate, tile),
                    TextureBounds = TileSheet.GetTileBounds(tile)
                });
        }
    }
}