using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public partial class WorldManager
    {
        [JsonIgnore]
        public OctTreeNode<GameComponent> OctTree = null;

        public void RemoveGameObject(GameComponent GameObject, BoundingBox LastBounds)
        {
            OctTree.Remove(GameObject, LastBounds);
        }

        public OctTreeNode<GameComponent> AddGameObject(GameComponent GameObject, BoundingBox LastBounds)
        {
            return OctTree.Add(GameObject, LastBounds);
        }

        public IEnumerable<GameComponent> EnumerateIntersectingObjects(BoundingBox box, CollisionType queryType)
        {
            PerformanceMonitor.PushFrame("CollisionManager.EnumerateIntersectingObjects");
            var hash = new HashSet<GameComponent>();
            OctTree.EnumerateItems(box, hash, t => (t.CollisionType & queryType) == t.CollisionType);
            PerformanceMonitor.PopFrame();
            return hash;
        }

        public IEnumerable<GameComponent> EnumerateIntersectingObjects(BoundingFrustum Frustum, Func<GameComponent, bool> Filter = null)
        {
            PerformanceMonitor.PushFrame("CollisionManager.EnumerateFrustum");
            var hash = new HashSet<GameComponent>();
            if (Filter == null)
                OctTree.EnumerateItems(Frustum, hash);
            else
                OctTree.EnumerateItems(Frustum, hash, Filter);
            PerformanceMonitor.PopFrame();
            return hash;
        }

        public IEnumerable<GameComponent> EnumerateIntersectingObjects(BoundingBox box, Func<GameComponent, bool> Filter = null)
        {
            PerformanceMonitor.PushFrame("CollisionManager.EnumerateIntersectingObjects w/ Filter");
            var hash = new HashSet<GameComponent>();
            if (Filter == null)
                OctTree.EnumerateItems(box, hash);
            else
                OctTree.EnumerateItems(box, hash, Filter);
            PerformanceMonitor.PopFrame();
            return hash;
        }

        public void EnumerateIntersectingObjects(BoundingBox box, HashSet<GameComponent> Into)
        {
            PerformanceMonitor.PushFrame("CollisionManager.EnumerateIntersectingObjects w/ Filter");
            OctTree.EnumerateItems(box, Into);
            PerformanceMonitor.PopFrame();
        }

        public IEnumerable<GlobalChunkCoordinate> EnumerateChunkIDsInBounds(BoundingBox Box)
        {
            var minChunkID = GlobalVoxelCoordinate.FromVector3(Box.Min).GetGlobalChunkCoordinate();
            var maxChunkID = GlobalVoxelCoordinate.FromVector3(Box.Max).GetGlobalChunkCoordinate();

            for (var x = minChunkID.X; x <= maxChunkID.X; ++x)
                for (var y = minChunkID.Y; y < maxChunkID.Y; ++y)
                    for (var z = minChunkID.Z; z < maxChunkID.Z; ++z)
                        yield return new GlobalChunkCoordinate(x, y, z);
        }

        public IEnumerable<VoxelChunk> EnumerateChunksInBounds(BoundingBox Box)
        {
            return EnumerateChunkIDsInBounds(Box)
                .Where(id => ChunkManager.ChunkData.CheckBounds(id))
                .Select(id => ChunkManager.ChunkData.GetChunk(id));
        }

        public IEnumerable<VoxelChunk> EnumerateChunksInBounds(BoundingFrustum Frustum)
        {
            return EnumerateChunksInBounds(MathFunctions.GetBoundingBox(Frustum.GetCorners()))
                .Where(c =>
                {
                    var min = new GlobalVoxelCoordinate(c.ID, new LocalVoxelCoordinate(0, 0, 0));
                    var box = new BoundingBox(min.ToVector3(), min.ToVector3() + new Vector3(VoxelConstants.ChunkSizeX, VoxelConstants.ChunkSizeY, VoxelConstants.ChunkSizeZ));
                    return Frustum.Contains(box) != ContainmentType.Disjoint;
                });
        }
    }
}
