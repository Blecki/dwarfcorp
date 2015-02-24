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
    ///  Minimal representation of a chunk.
    ///  Exists to write to and from files.
    /// </summary>
    public class ChunkFile 
    {
        public short[,,] Types;
        public short[,,] LiquidTypes;
        public byte[,,] Liquid;
        public Point3 Size;
        public Point3 ID;
        public Vector3 Origin;

        public static string Extension = "chunk";
        public static string CompressedExtension = "zchunk";

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
            Origin = chunk.Origin;
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

        public bool ReadFile(string filePath, bool isCompressed)
        {
            ChunkFile chunkFile = FileUtils.LoadJson<ChunkFile>(filePath, isCompressed);

            if(chunkFile == null)
            {
                return false;
            }
            else
            {
                CopyFrom(chunkFile);
                return true;
            }
        }

        public bool WriteFile(string filePath, bool compress)
        {
            return FileUtils.SaveJSon<ChunkFile>(this, filePath, compress);
        }

        public VoxelChunk ToChunk(ChunkManager manager)
        {
            int chunkSizeX = this.Size.X;
            int chunkSizeY = this.Size.Y;
            int chunkSizeZ = this.Size.Z;
            Vector3 origin = this.Origin;
            //Voxel[][][] voxels = ChunkGenerator.Allocate(chunkSizeX, chunkSizeY, chunkSizeZ);
            float scaleFator = PlayState.WorldScale;
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
                            c.Data.Types[index] = (byte) Types[x, y, z];
                            c.Data.IsVisible[index] = true;
                            c.Data.Health[index] = (byte)VoxelLibrary.GetVoxelType(Types[x, y, z]).StartingHealth;
                        }
                    }
                }
            }


            for(int x = 0; x < chunkSizeX; x++)
            {
                for(int z = 0; z < chunkSizeZ; z++)
                {
                    for(int y = 0; y < chunkSizeY; y++)
                    {
                        int index = c.Data.IndexAt(x, y, z);
                        c.Data.Water[index].WaterLevel = Liquid[x, y, z];
                        c.Data.Water[index].Type = (LiquidType) LiquidTypes[x, y, z];
                    }
                }
            }
            c.ShouldRebuildWater = true;

            return c;
        }

        public void FillDataFromChunk(VoxelChunk chunk)
        {
            for(int x = 0; x < Size.X; x++)
            {
                for(int y = 0; y < Size.Y; y++)
                {
                    for(int z = 0; z < Size.Z; z++)
                    {
                        int index = chunk.Data.IndexAt(x, y, z);
                        Voxel vox = chunk.MakeVoxel(x, y, z);
                        WaterCell water = chunk.Data.Water[index];

                        if(vox == null)
                        {
                            Types[x, y, z] = 0;
                        }
                        else
                        {
                            Types[x, y, z] = vox.Type.ID;
                        }

                        if(water.WaterLevel > 0)
                        {
                            Liquid[x, y, z] = water.WaterLevel;
                            LiquidTypes[x, y, z] = (short) water.Type;
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