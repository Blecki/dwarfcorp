// PlayState.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
