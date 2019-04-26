using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{
    /// <summary>
    ///     Minimal representation of a chunk.
    ///     Exists to write to and from files.
    /// </summary>
    [Serializable]
    public class ChunkFile
    {
        public static string Extension = "chunk";
        public static string CompressedExtension = "zchunk";

        public GlobalChunkCoordinate ID;
        public GlobalVoxelCoordinate Origin;

        public byte[] Liquid;
        public byte[] Types;
        public byte[] GrassType;
        public byte[] RampsSunlightExplored;
        
        public ChunkFile()
        {
        }

        public static ChunkFile CreateFromChunk(VoxelChunk chunk)
        {
            var r = new ChunkFile
            {
                ID = chunk.ID,
                Types = new byte[VoxelConstants.ChunkVoxelCount],
                Liquid = new byte[VoxelConstants.ChunkVoxelCount],
                GrassType = new byte[VoxelConstants.ChunkVoxelCount],
                RampsSunlightExplored = new byte[VoxelConstants.ChunkVoxelCount],
                Origin = chunk.Origin
            };

            chunk.Data.Types.CopyTo(r.Types, 0);
            chunk.Data.Grass.CopyTo(r.GrassType, 0);
            chunk.Data.RampsSunlightExploredPlayerBuilt.CopyTo(r.RampsSunlightExplored, 0);
            chunk.Data._Water.CopyTo(r.Liquid, 0);

            return r;
        }

        public VoxelChunk ToChunk(ChunkManager Manager)
        {
            VoxelChunk c = new VoxelChunk(Manager, ID);

            for (var i = 0; i < VoxelConstants.ChunkVoxelCount; ++i)
            {
                c.Data.Types[i] = Types[i];

                if (Types[i] > 0)
                {
                    // Rebuild the VoxelsPresentInSlice counters
                    c.Data.VoxelsPresentInSlice[(i >> VoxelConstants.ZDivShift) >> VoxelConstants.XDivShift] += 1;
                }                
            }

            if (Liquid != null)
            {
                Liquid.CopyTo(c.Data._Water, 0);
                for (int y = 0; y < VoxelConstants.ChunkSizeY; y++)
                    for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
                        for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                            c.Data.LiquidPresent[y] += VoxelHandle.UnsafeCreateLocalHandle(c, new LocalVoxelCoordinate(x, y, z)).LiquidLevel;
            }

            if (RampsSunlightExplored != null)
                RampsSunlightExplored.CopyTo(c.Data.RampsSunlightExploredPlayerBuilt, 0);
            if (GrassType != null)
                GrassType.CopyTo(c.Data.Grass, 0);
            return c;
        }
    }
}