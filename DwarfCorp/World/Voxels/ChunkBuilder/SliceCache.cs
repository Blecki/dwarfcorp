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
    public class SliceCache
    {
        public int[] AmbientValues = new int[4];
        public Dictionary<GlobalVoxelCoordinate, VertexLighting.VertexColorInfo> LightCache = new Dictionary<GlobalVoxelCoordinate, VertexLighting.VertexColorInfo>();
        public Dictionary<GlobalVoxelCoordinate, bool> ExploredCache = new Dictionary<GlobalVoxelCoordinate, bool>();
        public Dictionary<VoxelVertex, bool> ShouldSlope = new Dictionary<VoxelVertex, bool>();

        public void ClearSliceCache()
        {
            LightCache.Clear();
            ExploredCache.Clear();
        }

        public static GlobalVoxelCoordinate GetCacheKey(VoxelHandle Handle, VoxelVertex Vertex)
        {
            var coord = Handle.Coordinate;

            if ((Vertex & VoxelVertex.Front) == VoxelVertex.Front)
                coord = new GlobalVoxelCoordinate(coord.X, coord.Y, coord.Z + 1);

            if ((Vertex & VoxelVertex.Top) == VoxelVertex.Top)
                coord = new GlobalVoxelCoordinate(coord.X, coord.Y + 1, coord.Z);

            if ((Vertex & VoxelVertex.Right) == VoxelVertex.Right)
                coord = new GlobalVoxelCoordinate(coord.X + 1, coord.Y, coord.Z);

            return coord;
        }

        public void ClearVoxelCache()
        {
            ShouldSlope.Clear();
        }
    }
}