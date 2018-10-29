// ChunkFile.cs
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
        public Vector3 Origin;

        public byte[] Liquid;
        public byte[] Types;
        public byte[] GrassType;
        //public byte[] Decals;
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
                //Decals = new byte[VoxelConstants.ChunkVoxelCount],
                RampsSunlightExplored = new byte[VoxelConstants.ChunkVoxelCount],
                Origin = chunk.Origin
            };

            chunk.Data.Types.CopyTo(r.Types, 0);
            chunk.Data.Grass.CopyTo(r.GrassType, 0);
            //chunk.Data.Decals.CopyTo(r.Decals, 0);
            chunk.Data.RampsSunlightExploredPlayerBuilt.CopyTo(r.RampsSunlightExplored, 0);
            chunk.Data._Water.CopyTo(r.Liquid, 0);

            return r;
        }

        public VoxelChunk ToChunk(ChunkManager Manager)
        {
            VoxelChunk c = new VoxelChunk(Manager, Origin, ID);

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
                        {
                            VoxelHandle handle = new VoxelHandle(c, new LocalVoxelCoordinate(x, y, z));
                            c.Data.LiquidPresent[y] += handle.LiquidLevel;
                        }
            }
            if (RampsSunlightExplored != null)
                RampsSunlightExplored.CopyTo(c.Data.RampsSunlightExploredPlayerBuilt, 0);
            if (GrassType != null)
                GrassType.CopyTo(c.Data.Grass, 0);
            //if (Decals != null)
            //    Decals.CopyTo(c.Data.Decals, 0);

            c.CalculateInitialSunlight();
            return c;
        }
    }
}