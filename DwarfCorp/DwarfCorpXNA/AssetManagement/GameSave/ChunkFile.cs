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

        public bool[] Explored;
        public byte[] Liquid;
        public byte[] LiquidTypes;
        public byte[] Types;
        public byte[] GrassLayer;
        public byte[] GrassType;
        public byte[] GrassDecay;
        
        public ChunkFile()
        {
        }

        public ChunkFile(VoxelChunk chunk)
        {
            ID = chunk.ID;
            Types = new byte[VoxelConstants.ChunkVoxelCount];
            LiquidTypes = new byte[VoxelConstants.ChunkVoxelCount];
            Liquid = new byte[VoxelConstants.ChunkVoxelCount];
            Explored = new bool[VoxelConstants.ChunkVoxelCount];
            GrassLayer = new byte[VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ];
            GrassType = new byte[VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ];
            GrassDecay = new byte[VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ];
            Origin = chunk.Origin;
            FillDataFromChunk(chunk);
        }

        public ChunkFile(string fileName, bool compressed, bool binary)
        {
            ReadFile(fileName, compressed, binary);
        }

        public void CopyFrom(ChunkFile chunkFile)
        {
            ID = chunkFile.ID;
            Liquid = chunkFile.Liquid;
            LiquidTypes = chunkFile.LiquidTypes;
            Origin = chunkFile.Origin;
            Types = chunkFile.Types;
            Explored = chunkFile.Explored;
            GrassLayer = chunkFile.GrassLayer;
            GrassType = chunkFile.GrassType;
            GrassDecay = chunkFile.GrassDecay;
        }

        public bool ReadFile(string filePath, bool isCompressed, bool isBinary)
        {
            if (!isBinary)
            {
                ChunkFile chunkFile = FileUtils.LoadJson<ChunkFile>(filePath, isCompressed);

                if (chunkFile == null)
                {
                    return false;
                }
                CopyFrom(chunkFile);
                return true;
            }
            else
            {
                ChunkFile chunkFile = FileUtils.LoadBinary<ChunkFile>(filePath);

                if (chunkFile == null)
                {
                    return false;
                }
                CopyFrom(chunkFile);
                return true;
            }
        }

        public bool WriteFile(string filePath, bool compress, bool binary)
        {
            if (!binary)
                return FileUtils.SaveJSon(this, filePath, compress);
            return FileUtils.SaveBinary(this, filePath);
        }

        public VoxelChunk ToChunk(ChunkManager Manager)
        {
            VoxelChunk c = new VoxelChunk(Manager, Origin, ID);

            for (var i = 0; i < VoxelConstants.ChunkVoxelCount; ++i)
            {
                c.Data.Types[i] = Types[i];

                if (Types[i] > 0)
                {
                    c.Data.Health[i] = (byte)VoxelLibrary.GetVoxelType(Types[i]).StartingHealth;

                    // Rebuild the VoxelsPresentInSlice counters
                    c.Data.VoxelsPresentInSlice[(i >> VoxelConstants.ZDivShift) >> VoxelConstants.XDivShift] += 1;
                }                
            }

            Explored.CopyTo(c.Data.IsExplored, 0);
            // Separate loop for cache effeciency
            for (var i = 0; i < VoxelConstants.ChunkVoxelCount; ++i)
            {
                c.Data.Water[i].WaterLevel = Liquid[i];
                c.Data.Water[i].Type = (LiquidType)LiquidTypes[i];

                // Rebuild the LiquidPresent counters
                if ((LiquidType)LiquidTypes[i] != LiquidType.None)
                    c.Data.LiquidPresent[(i >> VoxelConstants.ZDivShift) >> VoxelConstants.XDivShift] += 1;
            }

            GrassLayer.CopyTo(c.Data.GrassLayer, 0);
            GrassType.CopyTo(c.Data.GrassType, 0);
            GrassDecay.CopyTo(c.Data.GrassDecay, 0);

            c.CalculateInitialSunlight();
            return c;
        }

        public void FillDataFromChunk(VoxelChunk chunk)
        {
            chunk.Data.Types.CopyTo(Types, 0);
            chunk.Data.IsExplored.CopyTo(Explored, 0);
            chunk.Data.GrassLayer.CopyTo(GrassLayer, 0);
            chunk.Data.GrassType.CopyTo(GrassType, 0);
            chunk.Data.GrassDecay.CopyTo(GrassDecay, 0);

            for (var i = 0; i < VoxelConstants.ChunkVoxelCount; ++i)
            {
                var water = chunk.Data.Water[i];
                if (water.WaterLevel > 0)
                {
                    Liquid[i] = water.WaterLevel;
                    LiquidTypes[i] = (byte)water.Type;
                }
                else
                {
                    Liquid[i] = 0;
                    LiquidTypes[i] = 0;
                }
            }
        }
    }
}