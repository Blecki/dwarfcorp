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
    public static class GeometryBuilder
    {
        public static VoxelPrimitive Cube = null;

        public static GeometricPrimitive CreateFromChunk(VoxelChunk Chunk, WorldManager World)
        {
            if (Cube == null)
            {
                var terrainSheet = new Gui.TileSheet(16 * 32, 16 * 32, new Rectangle(0, 0, 16 * 32, 16 * 32), 32, 32, false);
                Cube = VoxelPrimitive.MakeCube();
                foreach (var face in Cube.Faces)
                {
                    if (face.Orientation == FaceOrientation.East)
                        face.Mesh.EntireMeshAsPart().Texture(terrainSheet.TileMatrix(5)); // Need to set texture bounds as well.

                    else if (face.Orientation == FaceOrientation.South)
                        face.Mesh.EntireMeshAsPart().Texture(terrainSheet.TileMatrix(1)); // Need to set texture bounds as well.
                    else
                        face.Mesh.EntireMeshAsPart().Texture(terrainSheet.TileMatrix(2)); // Need to set texture bounds as well.

                }
            }


            DebugHelper.AssertNotNull(Chunk);
            DebugHelper.AssertNotNull(World);

            var sliceStack = new List<Geo.Mesh>();

            int maxViewingLevel = World.Renderer.PersistentSettings.MaxViewingLevel;

            for (var localY = 0; localY < maxViewingLevel - Chunk.Origin.Y && localY < VoxelConstants.ChunkSizeY; ++localY)
            {
                Geo.Mesh sliceGeometry = null;

                lock (Chunk.Data.SliceCache)
                {
                    var cachedSlice = Chunk.Data.SliceCache[localY];

                    if (cachedSlice != null)
                    {
                        sliceStack.Add(Geo.Mesh.FromRawPrimitive(cachedSlice)); // Todo: Get rid of the raw primitive / geometric primitive bullshit entirely

                        if (GameSettings.Current.GrassMotes)
                            Chunk.RebuildMoteLayerIfNull(localY);

                        continue;
                    }

                    sliceGeometry = Geo.Mesh.EmptyMesh();

                    Chunk.Data.SliceCache[localY] = sliceGeometry.AsRawPrimitive(); // Copying it in means our additions later won't take, doesn't it?
                }

                if (GameSettings.Current.GrassMotes)
                    Chunk.RebuildMoteLayer(localY);

                DebugHelper.AssertNotNull(sliceGeometry);
                GenerateSliceGeometry(sliceGeometry, Chunk, localY, World);

                sliceStack.Add(sliceGeometry);
            }

            var chunkGeo = Geo.Mesh.Merge(sliceStack.ToArray());

            var r = new GeometricPrimitive();
            r.Vertices = chunkGeo.Verticies;
            r.VertexCount = chunkGeo.VertexCount;
            r.Indexes = chunkGeo.Indicies.Select(c => (ushort)c).ToArray();
            r.IndexCount = chunkGeo.IndexCount;

            return r;
        }

        private static void GenerateSliceGeometry(
            Geo.Mesh Into,
            VoxelChunk Chunk,
            int LocalY,
            WorldManager World)
        {
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                    GenerateVoxelGeometry(Into, VoxelHandle.UnsafeCreateLocalHandle(Chunk, new LocalVoxelCoordinate(x, LocalY, z)), World);
        }

        public static void GenerateVoxelGeometry(Geo.Mesh Into, VoxelHandle Voxel, WorldManager World)
        {
            if (Voxel.IsEmpty) return;

            var voxelTransform = Matrix.CreateTranslation(Voxel.Coordinate.ToVector3());
            var primitive = Cube; //Lookup the primitive;

            foreach (var face in Cube.Faces)
                GenerateFaceGeometry(Into, Voxel, face, voxelTransform, World);
        }

        public static void GenerateFaceGeometry(Geo.Mesh Into, VoxelHandle Voxel, Face Face, Matrix VoxelTransform, WorldManager World)
        {
            if (Face.CullType == FaceCullType.Cull)
            {
                var neighborVoxel = World.ChunkManager.CreateVoxelHandle(Voxel.Coordinate + OrientationHelper.GetFaceNeighborOffset(Face.Orientation));
                if (neighborVoxel.IsValid && !neighborVoxel.IsEmpty)
                    return;
            }

            var facePart = Into.Concat(Face.Mesh);
            facePart.Transform(VoxelTransform);
        }
    }
}