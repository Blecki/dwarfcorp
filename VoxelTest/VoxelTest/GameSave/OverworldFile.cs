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

    public class OverworldFile : SaveData
    {
        public class OverworldData
        {
            public string Name { get; set; }
            public int[,] Biomes { get; set; }
            public int[,] Erosion { get; set; }
            public int[,] Faults { get; set; }
            public int[,] Height { get; set; }
            public int[,] Rainfall { get; set; }
            public int[,] Temperature { get; set; }
            public int[,] Water { get; set; }
            public int[,] Weathering { get; set; }

            public Overworld.MapData[,] CreateMap()
            {
                int sx = Biomes.GetLength(0);
                int sy = Biomes.GetLength(1);

                Overworld.MapData[,] toReturn = new Overworld.MapData[Biomes.GetLength(0), Biomes.GetLength(1)];

                for(int x = 0; x < sx; x++)
                {
                    for(int y = 0; y < sy; y++)
                    {
                        toReturn[x, y] = new Overworld.MapData
                        {
                            Biome = (Overworld.Biome) Biomes[x, y],
                            Erosion = (float) Erosion[x, y] / 255.0f,
                            Faults = (float) Faults[x, y] / 255.0f,
                            Height = (float) Height[x, y] / 255.0f,
                            Rainfall = (float) Rainfall[x, y] / 255.0f,
                            Temperature = (float) Temperature[x, y] / 255.0f,
                            Water = (Overworld.WaterType) (Water[x, y]),
                            Weathering = (float) Weathering[x, y] / 255.0f
                        };
                    }
                }

                return toReturn;
            }

            public Texture2D CreateTexture(GraphicsDevice device, int width, int height)
            {
                Texture2D toReturn = null;
                Overworld.MapData[,] mapData = CreateMap();
                toReturn = new Texture2D(device, width, height);
                System.Threading.Mutex imageMutex = new System.Threading.Mutex();
                Color[] worldData = new Color[width * height];
                Overworld.TextureFromHeightMap("Height", mapData, Overworld.ScalarFieldType.Height, width, height, imageMutex, worldData, toReturn);

                return toReturn;
            }

            public OverworldData()
            {
            }

            public OverworldData(Overworld.MapData[,] map, string name)
            {
                int sizeX = map.GetLength(0);
                int sizeY = map.GetLength(1);
                Biomes = new int[sizeX, sizeY];
                Erosion = new int[sizeX, sizeY];
                Faults = new int[sizeX, sizeY];
                Rainfall = new int[sizeX, sizeY];
                Temperature = new int[sizeX, sizeY];
                Water = new int[sizeX, sizeY];
                Weathering = new int[sizeX, sizeY];
                Height = new int[sizeX, sizeY];
                Name = name;

                for(int x = 0; x < sizeX; x++)
                {
                    for(int y = 0; y < sizeY; y++)
                    {
                        Overworld.MapData data = map[x, y];
                        Biomes[x, y] = (int) data.Biome;
                        Erosion[x, y] = (int) (data.Erosion * 255);
                        Faults[x, y] = (int) (data.Faults * 255);
                        Height[x, y] = (int) (data.Height * 255);
                        Rainfall[x, y] = (int) (data.Rainfall * 255);
                        Temperature[x, y] = (int) (data.Temperature * 255);
                        Water[x, y] = (int) (data.Water);
                        Weathering[x, y] = (int) (data.Weathering * 255);
                    }
                }
            }
        }

        public OverworldData Data { get; set; }

        public new static string Extension = "world";
        public new static string CompressedExtension = "zworld";

        public OverworldFile()
        {
        }

        public OverworldFile(Overworld.MapData[,] map, string name)
        {
            Data = new OverworldData(map, name);
        }

        public OverworldFile(string fileName, bool isCompressed)
        {
            ReadFile(fileName, isCompressed);
        }


        public void CopyFrom(OverworldFile file)
        {
            Data = file.Data;
        }

        public override bool ReadFile(string filePath, bool isCompressed)
        {
            OverworldFile file = FileUtils.LoadJson<OverworldFile>(filePath, isCompressed);

            if(file == null)
            {
                return false;
            }
            else
            {
                CopyFrom(file);
                return true;
            }
        }

        public override bool WriteFile(string filePath, bool compress)
        {
            return FileUtils.SaveJSon<OverworldFile>(this, filePath, compress);
        }
    }

}