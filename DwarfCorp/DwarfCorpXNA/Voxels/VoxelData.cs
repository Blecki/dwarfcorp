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

        public static VoxelData Allocate()
        {
            int numVoxels = VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeY * VoxelConstants.ChunkSizeZ;

            VoxelData toReturn = new VoxelData()
            {
                Health = new byte[numVoxels],
                IsExplored = new bool[numVoxels],
                SunColors = new byte[numVoxels],
                Types = new byte[numVoxels],
                Water = new WaterCell[numVoxels],
                RampTypes = new RampType[numVoxels],
            };

            for (int i = 0; i < numVoxels; i++)
            {
                toReturn.Water[i] = new WaterCell();
            }

            return toReturn;
        }
    }

}
