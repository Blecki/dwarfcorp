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
        // C# Needs a "Friend" mechanism similar to C++. Only the classes ChunkFile and VoxelHandle
        //  should ever access this data.
        public bool[] IsExplored;
        public byte[] Health;
        public byte[] Types;
        public byte[] SunColors;
        public WaterCell[] Water;
        public RampType[] RampTypes;

        public int[] LiquidPresent;
        public int[] VoxelsPresentInSlice;
        
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
                LiquidPresent = new int[VoxelConstants.ChunkSizeY],
                VoxelsPresentInSlice = new int[VoxelConstants.ChunkSizeY]
            };

            for (int i = 0; i < numVoxels; i++)
            {
                toReturn.Water[i] = new WaterCell();
            }

            return toReturn;
        }

        public void ResetSunlight(byte sunColor)
        {
            for (int i = 0; i < VoxelConstants.ChunkVoxelCount; i++)
                SunColors[i] = sunColor;
        }
    }

}
