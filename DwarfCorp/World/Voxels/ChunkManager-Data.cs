using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// Has a collection of voxel chunks, and methods for accessing them.
    /// </summary>
    /// Todo: Add Y axis
    public partial class ChunkManager
    {
        public void InitializeChunkMap(Point3 MapOrigin, Point3 MapDimensions)
        {
            this.MapOrigin = MapOrigin;
            this.MapDimensions = MapDimensions;
            this.ChunkMap = new VoxelChunk[MapDimensions.X * MapDimensions.Y * MapDimensions.Z];
        }

        // These have to be public so that VoxelHandle can access them effeciently. ;_;
        public VoxelChunk[] ChunkMap;
        public Point3 MapDimensions;
        public Point3 MapOrigin;

        public VoxelChunk GetChunk(GlobalChunkCoordinate Coordinate)
        {
            if (!CheckBounds(Coordinate)) throw new IndexOutOfRangeException();
            return ChunkMap[GetChunkIndex(Coordinate)];
        }

        public bool CheckBounds(GlobalChunkCoordinate Coordinate)
        {
            if (Coordinate.X < MapOrigin.X || Coordinate.X >= MapOrigin.X + MapDimensions.X) return false;
            if (Coordinate.Y < MapOrigin.Y || Coordinate.Y >= MapOrigin.Y + MapDimensions.Y) return false;
            if (Coordinate.Z < MapOrigin.Z || Coordinate.Z >= MapOrigin.Z + MapDimensions.Z) return false;
            return true;
        }

        public GlobalChunkCoordinate ConfineToBounds(GlobalChunkCoordinate Coordinate)
        {
            var x = (Coordinate.X < MapOrigin.X) ? MapOrigin.X : (Coordinate.X >= (MapOrigin.X + MapDimensions.X) ? (MapOrigin.X + MapDimensions.X - 1) : Coordinate.X);
            var y = (Coordinate.Y < MapOrigin.Y) ? MapOrigin.Y : (Coordinate.Y >= (MapOrigin.Y + MapDimensions.Y) ? (MapOrigin.Y + MapDimensions.Y - 1) : Coordinate.Y);
            var z = (Coordinate.Z < MapOrigin.Z) ? MapOrigin.Z : (Coordinate.Z >= (MapOrigin.Z + MapDimensions.Z) ? (MapOrigin.Z + MapDimensions.Z - 1) : Coordinate.Z);
            return new GlobalChunkCoordinate(x, y, z);
        }

        public IEnumerable<VoxelChunk> GetChunkEnumerator()
        {
            return ChunkMap;
        }

        public int GetChunkIndex(GlobalChunkCoordinate ID)
        {
            return (ID.Y - MapOrigin.Y) * MapDimensions.X * MapDimensions.Z
                + (ID.Z - MapOrigin.Z) * MapDimensions.X
                + (ID.X - MapOrigin.X);
        }

        public bool AddChunk(VoxelChunk Chunk)
        {
            if (!CheckBounds(Chunk.ID)) throw new IndexOutOfRangeException();

            ChunkMap[GetChunkIndex(Chunk.ID)] = Chunk;
            return true;
        }

        public void LoadChunks(List<ChunkFile> Chunks, ChunkManager Manager)
        {
            if (Chunks.Count == 0)
                throw new Exception("Game file corrupt. It has no chunk files.");

            var maxChunkX = Chunks.Max(c => c.ID.X) + 1;
            var maxChunkY = Chunks.Max(c => c.ID.Y) + 1;
            var maxChunkZ = Chunks.Max(c => c.ID.Z) + 1;

            var minChunkX = Chunks.Min(c => c.ID.X);
            var minChunkY = Chunks.Min(c => c.ID.Y);
            var minChunkZ = Chunks.Min(c => c.ID.Z);

            MapOrigin = new Point3(minChunkX, minChunkY, minChunkZ);
            MapDimensions = new Point3(maxChunkX - minChunkX, maxChunkY - minChunkY, maxChunkZ - minChunkZ);

            ChunkMap = new VoxelChunk[MapDimensions.X * MapDimensions.Y * MapDimensions.Z];

            foreach (VoxelChunk chunk in Chunks.Select(file => file.ToChunk(Manager)))
                AddChunk(chunk);

            Manager.UpdateBounds();
        }
    }
}
