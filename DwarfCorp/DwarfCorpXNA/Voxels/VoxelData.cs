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

        public byte[] GrassLayer;
        public byte[] GrassType;
        public byte[] GrassDecay;

        public byte[] Decals;

        public WaterCell[] Water;
        public RampType[] RampTypes;

        public int[] LiquidPresent;
        public int[] VoxelsPresentInSlice;
        public RawPrimitive[] SliceCache;
        
        public static VoxelData Allocate()
        {
            VoxelData toReturn = new VoxelData()
            {
                Health = new byte[VoxelConstants.ChunkVoxelCount],
                IsExplored = new bool[VoxelConstants.ChunkVoxelCount],
                SunColors = new byte[VoxelConstants.ChunkVoxelCount],
                Types = new byte[VoxelConstants.ChunkVoxelCount],
                GrassLayer = new byte[VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ],
                GrassType = new byte[VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ],
                GrassDecay = new byte[VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ],
                Decals = new byte[VoxelConstants.ChunkVoxelCount],
                Water = new WaterCell[VoxelConstants.ChunkVoxelCount],
                RampTypes = new RampType[VoxelConstants.ChunkVoxelCount],

                LiquidPresent = new int[VoxelConstants.ChunkSizeY],
                VoxelsPresentInSlice = new int[VoxelConstants.ChunkSizeY],
                SliceCache = new RawPrimitive[VoxelConstants.ChunkSizeY]
            };

            // Todo: This might be unecessary.
            for (int i = 0; i < VoxelConstants.ChunkVoxelCount; i++)
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
