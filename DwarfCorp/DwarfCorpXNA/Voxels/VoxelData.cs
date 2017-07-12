using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Stores actual data for voxels in SOA format.
    /// </summary>
    public class VoxelData
    {
        public bool[] IsExplored;
        public byte[] Health;
        public byte[] Types;
        public byte[] SunColors;
        public WaterCell[] Water;
        public RampType[] RampTypes;

        // Todo: %KILL% - Replace with constants version!!
        public static int IndexAt(LocalVoxelCoordinate C)
        {
            return (C.Z * VoxelConstants.ChunkSizeY + C.Y) * VoxelConstants.ChunkSizeX + C.X;
        }

        // Todo: %KILL%
        public Vector3 CoordsAt(int idx)
        {
            int x = idx % (VoxelConstants.ChunkSizeX);
            idx /= (VoxelConstants.ChunkSizeX);
            int y = idx % (VoxelConstants.ChunkSizeY);
            idx /= (VoxelConstants.ChunkSizeY);
            int z = idx;
            return new Vector3(x, y, z);
        }
    }

}
