using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public partial class WorldManager
    {
        public void RemoveGameObject(GameComponent GameObject, BoundingBox LastBounds)
        {
            var minChunkID = GlobalVoxelCoordinate.FromVector3(LastBounds.Min).GetGlobalChunkCoordinate();
            var maxChunkID = GlobalVoxelCoordinate.FromVector3(LastBounds.Max).GetGlobalChunkCoordinate();

            for (var x = minChunkID.X; x <= maxChunkID.X; ++x)
                for (var y = minChunkID.Y; y <= maxChunkID.Y; ++y)
                    for (var z = minChunkID.Z; z <= maxChunkID.Z; ++z)
                    {
                        var coord = new GlobalChunkCoordinate(x, y, z);
                        if (ChunkManager.CheckBounds(coord))
                        {
                            var chunk = ChunkManager.GetChunk(coord);
                            lock (chunk)
                                chunk.Entities.Remove(GameObject);
                        }
                    }
        }

        public void AddGameObject(GameComponent GameObject, BoundingBox LastBounds)
        {
            var minChunkID = GlobalVoxelCoordinate.FromVector3(LastBounds.Min).GetGlobalChunkCoordinate();
            var maxChunkID = GlobalVoxelCoordinate.FromVector3(LastBounds.Max).GetGlobalChunkCoordinate();

            for (var x = minChunkID.X; x <= maxChunkID.X; ++x)
                for (var y = minChunkID.Y; y <= maxChunkID.Y; ++y)
                    for (var z = minChunkID.Z; z <= maxChunkID.Z; ++z)
                    {
                        var coord = new GlobalChunkCoordinate(x, y, z);
                        if (ChunkManager.CheckBounds(coord))
                        {
                            var chunk = ChunkManager.GetChunk(coord);
                            lock (chunk)
                                chunk.Entities.Add(GameObject);
                        }
                    }
        }

        public IEnumerable<GameComponent> EnumerateIntersectingObjects(BoundingBox box, CollisionType queryType)
        {
            PerformanceMonitor.PushFrame("CollisionManager.EnumerateIntersectingObjects");
            var r = EnumerateIntersectingObjects(box, t => (t.CollisionType & queryType) == t.CollisionType);
            PerformanceMonitor.PopFrame();
            return r;
        }

        public IEnumerable<GameComponent> EnumerateIntersectingObjects(BoundingFrustum Frustum, Func<GameComponent, bool> Filter = null)
        {
            PerformanceMonitor.PushFrame("CollisionManager.EnumerateFrustum");
            var hash = new HashSet<GameComponent>();
            foreach (var chunk in EnumerateChunksInBounds(Frustum))
                lock (chunk)
                {
                    foreach (var entity in chunk.Entities)
                        if (Frustum.Contains(entity.BoundingBox) != ContainmentType.Disjoint)
                            if (Filter == null || Filter(entity))
                                hash.Add(entity);
                }
            PerformanceMonitor.PopFrame();
            return hash;
        }

        public IEnumerable<GameComponent> EnumerateIntersectingObjects(BoundingBox box, Func<GameComponent, bool> Filter = null)
        {
            PerformanceMonitor.PushFrame("CollisionManager.EnumerateIntersectingObjects w/ Filter");
            var hash = new HashSet<GameComponent>();
            EnumerateIntersectingObjects(box, hash, Filter);
            PerformanceMonitor.PopFrame();
            return hash;
        }

        public HashSet<GameComponent> EnumerateIntersectingObjectsLoose(BoundingBox box, Func<GameComponent, bool> Filter = null)
        {
            PerformanceMonitor.PushFrame("CollisionManager.EnumerateIntersectingObjects w/ Filter");
            var hash = new HashSet<GameComponent>();
            EnumerateIntersectingObjects(box, hash, Filter);
            PerformanceMonitor.PopFrame();
            return hash;
        }

        public void EnumerateIntersectingObjects(BoundingBox Box, HashSet<GameComponent> Into, Func<GameComponent, bool> Filter = null)
        {
            var minChunkID = GlobalVoxelCoordinate.FromVector3(Box.Min).GetGlobalChunkCoordinate();
            var maxChunkID = GlobalVoxelCoordinate.FromVector3(Box.Max).GetGlobalChunkCoordinate();

            for (var x = minChunkID.X; x <= maxChunkID.X; ++x)
                for (var y = minChunkID.Y; y <= maxChunkID.Y; ++y)
                    for (var z = minChunkID.Z; z <= maxChunkID.Z; ++z)
                    {
                        var coord = new GlobalChunkCoordinate(x, y, z);
                        if (ChunkManager.CheckBounds(coord))
                        {
                            var chunk = ChunkManager.GetChunk(coord);
                            lock (chunk)
                            {
                                foreach (var entity in chunk.Entities)
                                    if (Box.Contains(entity.BoundingBox) != ContainmentType.Disjoint)
                                        if (Filter == null || Filter(entity))
                                            Into.Add(entity);
                            }
                        }
                    }
        }

        public void EnumerateIntersectingObjectsLoose(BoundingBox box, HashSet<GameComponent> Into, Func<GameComponent, bool> Filter = null)
        {
            PerformanceMonitor.PushFrame("CollisionManager.EnumerateIntersectingObjects w/ Filter");
            foreach (var chunk in EnumerateChunksInBounds(box))
                lock (chunk)
                {
                    foreach (var entity in chunk.Entities)
                        if (Filter == null || Filter(entity))
                            Into.Add(entity);
                }
            PerformanceMonitor.PopFrame();
        }

        public IEnumerable<GlobalChunkCoordinate> EnumerateChunkIDsInBounds(BoundingBox Box)
        {
            var minChunkID = GlobalVoxelCoordinate.FromVector3(Box.Min).GetGlobalChunkCoordinate();
            var maxChunkID = GlobalVoxelCoordinate.FromVector3(Box.Max).GetGlobalChunkCoordinate();

            for (var x = minChunkID.X; x <= maxChunkID.X; ++x)
                for (var y = minChunkID.Y; y <= maxChunkID.Y; ++y)
                    for (var z = minChunkID.Z; z <= maxChunkID.Z; ++z)
                        yield return new GlobalChunkCoordinate(x, y, z);
        }

        public IEnumerable<VoxelChunk> EnumerateChunksInBounds(BoundingBox Box)
        {
            return EnumerateChunkIDsInBounds(Box)
                .Where(id => ChunkManager.CheckBounds(id))
                .Select(id => ChunkManager.GetChunk(id));
        }

        public IEnumerable<VoxelChunk> EnumerateChunksInBounds(BoundingFrustum Frustum)
        {
            return EnumerateChunksInBounds(MathFunctions.GetBoundingBox(Frustum.GetCorners()))
                .Where(c =>
                {
                    var min = new GlobalVoxelCoordinate(c.ID, new LocalVoxelCoordinate(0, 0, 0));
                    var box = new BoundingBox(min.ToVector3(), min.ToVector3() + new Vector3(VoxelConstants.ChunkSizeX, VoxelConstants.ChunkSizeY, VoxelConstants.ChunkSizeZ));
                    var r = Frustum.Contains(box) != ContainmentType.Disjoint;
                    return r;
                });
        }
    }
}
