using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{

    /// <summary>
    ///  Minimal representation of a chunk.
    ///  Exists to write to and from files.
    /// </summary>
    public class ChunkFile : SaveData
    {
        public short[, ,] Types;
        public short[, ,] LiquidTypes;
        public byte[, ,] Liquid;
        public Point3 Size;
        public Point3 ID;
        public Vector3 Origin;

        public new static string Extension = "chunk"; 
        public new static string CompressedExtension = "zchunk";

        public ChunkFile()
        {

        }

        public ChunkFile(VoxelChunk chunk)
        {
            Size = new Point3(chunk.SizeX, chunk.SizeY, chunk.SizeZ);
            ID = chunk.ID;
            Types = new short[Size.X, Size.Y, Size.Z];
            LiquidTypes = new short[Size.X, Size.Y, Size.Z];
            Liquid = new byte[Size.X, Size.Y, Size.Z];
            FillDataFromChunk(chunk);
        }

        public ChunkFile(string fileName, bool compressed)
        {
            ReadFile(fileName, compressed);
        }


        public void CopyFrom(ChunkFile chunkFile)
        {
            this.ID = chunkFile.ID;
            this.Liquid = chunkFile.Liquid;
            this.LiquidTypes = chunkFile.LiquidTypes;
            this.Origin = chunkFile.Origin;
            this.Size = chunkFile.Size;
            this.Types = chunkFile.Types;
        }

        public override bool ReadFile(string filePath, bool isCompressed)
        {
            ChunkFile chunkFile = FileUtils.LoadJson<ChunkFile>(filePath, isCompressed);

            if (chunkFile == null)
            {
                return false;
            }
            else
            {
                CopyFrom(chunkFile);
                return true;
            }
        }

        public override bool WriteFile(string filePath, bool compress)
        {
            return FileUtils.SaveJSon<ChunkFile>(this, filePath, compress);
        }

        public void FillDataFromChunk(VoxelChunk chunk)
        {
            for (int x = 0; x < Size.X; x++)
            {
                for (int y = 0; y < Size.Y; y++)
                {
                    for (int z = 0; z < Size.Z; z++)
                    {
                        Voxel vox = chunk.VoxelGrid[x][y][z];
                        WaterCell water = chunk.Water[x][y][z];

                        if(vox == null)
                        {
                            Types[x, y, z] = 0;
                        }
                        else
                        {
                            Types[x, y, z] = vox.Type.ID;
                        }

                        if (water.WaterLevel > 0)
                        {
                            Liquid[x, y, z] = water.WaterLevel;
                            LiquidTypes[x, y, z] = (short)water.Type;
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
