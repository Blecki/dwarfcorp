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
    ///  Minimal representation of a chunk.
    ///  Exists to write to and from files.
    /// </summary>
    [Serializable]
    public class ChunkFile 
    {
        public static string Extension = "chunk";
        public static string CompressedExtension = "zchunk";

        public bool[,,] Explored;

        public Point3 ID;
        public byte[,,] Liquid;
        public byte[,,] LiquidTypes;

        public Vector3 Origin;
        public Point3 Size;
        public byte[,,] Types;

        public ChunkFile()
        {
        }

        public ChunkFile(VoxelChunk chunk)
        {
            Size = new Point3(chunk.SizeX, chunk.SizeY, chunk.SizeZ);
            ID = chunk.ID;
            Types = new byte[Size.X, Size.Y, Size.Z];
            LiquidTypes = new byte[Size.X, Size.Y, Size.Z];
            Liquid = new byte[Size.X, Size.Y, Size.Z];
            Explored = new bool[Size.X, Size.Y, Size.Z];
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
            Size = chunkFile.Size;
            Types = chunkFile.Types;
            Explored = chunkFile.Explored;
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

        public VoxelChunk ToChunk(ChunkManager manager)
        {
            int chunkSizeX = this.Size.X;
            int chunkSizeY = this.Size.Y;
            int chunkSizeZ = this.Size.Z;
            Vector3 origin = this.Origin;
            VoxelChunk c = new VoxelChunk(manager, origin, 1, ID, chunkSizeX, chunkSizeY, chunkSizeZ)
            {
                ShouldRebuild = true,
                ShouldRecalculateLighting = true
            };

            for(int x = 0; x < chunkSizeX; x++)
            {
                for(int z = 0; z < chunkSizeZ; z++)
                {
                    for(int y = 0; y < chunkSizeY; y++)
                    {
                        int index = c.Data.IndexAt(x, y, z);
                        if(Types[x, y, z] > 0)
                        {
                            c.Data.Types[index] = Types[x, y, z];
                            c.Data.Health[index] = (byte) VoxelLibrary.GetVoxelType(Types[x, y, z]).StartingHealth;
                        }
                        c.Data.IsExplored[index] = Explored[x, y, z];
                        c.Data.Water[index].WaterLevel = Liquid[x, y, z];
                        c.Data.Water[index].Type = (LiquidType)LiquidTypes[x, y, z];
                    }
                }
            }
            c.ShouldRebuildWater = true;

            return c;
        }

        public void FillDataFromChunk(VoxelChunk chunk)
        {
            VoxelChunk.VoxelData data = chunk.Data;
            for(int x = 0; x < Size.X; x++)
            {
                for(int y = 0; y < Size.Y; y++)
                {
                    for(int z = 0; z < Size.Z; z++)
                    {
                        int index = data.IndexAt(x, y, z);
                        WaterCell water = data.Water[index];
                        Types[x, y, z] = data.Types[index];
                        Explored[x, y, z] = data.IsExplored[index];

                        if(water.WaterLevel > 0)
                        {
                            Liquid[x, y, z] = water.WaterLevel;
                            LiquidTypes[x, y, z] = (byte) water.Type;
                        }
                        else
                        {
                            Liquid[x, y, z] = 0;
                            LiquidTypes[x, y, z] = 0;
                        }
                    }
                }
            }
        }
    }

}