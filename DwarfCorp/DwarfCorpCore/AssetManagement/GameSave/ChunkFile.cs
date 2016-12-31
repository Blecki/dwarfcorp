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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    ///     Minimal representation of a chunk.
    ///     Exists to write to and from files.
    /// </summary>
    [Serializable]
    public class ChunkFile
    {
        /// <summary>
        /// The file extension.
        /// </summary>
        public static string Extension = "chunk";
        /// <summary>
        /// The compressed (zip) file extension
        /// </summary>
        public static string CompressedExtension = "zchunk";
        /// <summary>
        /// Array telling us which voxels have been explored.
        /// </summary>
        public bool[,,] Explored;
        /// <summary>
        /// The identifier of the chunk.
        /// </summary>
        public Point3 ID;
        /// <summary>
        /// The liquid levels (0-255)
        /// </summary>
        public byte[,,] Liquid;
        /// <summary>
        /// The liquid types.
        /// </summary>
        public byte[,,] LiquidTypes;
        /// <summary>
        /// The origin of the chunk in world coordinates. (leastmost corner)
        /// </summary>
        public Vector3 Origin;
        /// <summary>
        /// The size of the chunk in voxels.
        /// </summary>
        public Point3 Size;
        /// <summary>
        /// The types of the voxels in the chunk.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkFile"/> class from a file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="compressed">if set to <c>true</c> the file is zip compressed.</param>
        /// <param name="binary">if set to <c>true</c> the file is binary, otherwise, it is a JSON file..</param>
        public ChunkFile(string fileName, bool compressed, bool binary)
        {
            ReadFile(fileName, compressed, binary);
        }


        /// <summary>
        /// Deep clone another chunk file.
        /// </summary>
        /// <param name="chunkFile">The chunk file.</param>
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

        /// <summary>
        /// Reads the file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="isCompressed">if set to <c>true</c> the file is gzip compressed.</param>
        /// <param name="isBinary">if set to <c>true</c> the file is binary.</param>
        /// <returns>True if the file could be read, or false otherwise.</returns>
        public bool ReadFile(string filePath, bool isCompressed, bool isBinary)
        {
            if (!isBinary)
            {
                var chunkFile = FileUtils.LoadJson<ChunkFile>(filePath, isCompressed);

                if (chunkFile == null)
                {
                    return false;
                }
                CopyFrom(chunkFile);
                return true;
            }
            else
            {
                var chunkFile = FileUtils.LoadBinary<ChunkFile>(filePath);

                if (chunkFile == null)
                {
                    return false;
                }
                CopyFrom(chunkFile);
                return true;
            }
        }

        /// <summary>
        /// Writes the chunk data to a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="compress">if set to <c>true</c> compress the file using gzip.</param>
        /// <param name="binary">if set to <c>true</c> write a binary file.</param>
        /// <returns></returns>
        public bool WriteFile(string filePath, bool compress, bool binary)
        {
            if (!binary)
                return FileUtils.SaveJSon(this, filePath, compress);
            return FileUtils.SaveBinary(this, filePath);
        }

        /// <summary>
        /// Create a new chunk using this file.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <returns>A new chunk containing the data from this file.</returns>
        public VoxelChunk ToChunk(ChunkManager manager)
        {
            int chunkSizeX = Size.X;
            int chunkSizeY = Size.Y;
            int chunkSizeZ = Size.Z;
            Vector3 origin = Origin;
            // Note, this old way of doing it is too slow, instead directly set the data.
            //Voxel[][][] voxels = ChunkGenerator.Allocate(chunkSizeX, chunkSizeY, chunkSizeZ);

            // Create a new chunk
            var c = new VoxelChunk(manager, origin, 1, ID, chunkSizeX, chunkSizeY, chunkSizeZ)
            {
                ShouldRebuild = true,
                ShouldRecalculateLighting = true,
                ShouldRebuildWater = true
            };

            // For each voxel, set its properties.
            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    for (int y = 0; y < chunkSizeY; y++)
                    {
                        int index = c.Data.IndexAt(x, y, z);
                        if (Types[x, y, z] > 0)
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

            return c;
        }

        /// <summary>
        /// Fills this data file with the voxel data from a chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        public void FillDataFromChunk(VoxelChunk chunk)
        {
            for (int x = 0; x < Size.X; x++)
            {
                for (int y = 0; y < Size.Y; y++)
                {
                    for (int z = 0; z < Size.Z; z++)
                    {
                        int index = chunk.Data.IndexAt(x, y, z);
                        WaterCell water = chunk.Data.Water[index];
                        Types[x, y, z] = chunk.Data.Types[index];
                        Explored[x, y, z] = chunk.Data.IsExplored[index];

                        if (water.WaterLevel > 0)
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