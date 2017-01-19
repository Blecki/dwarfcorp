// OverworldFile.cs
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
    [Serializable]
    public class OverworldFile
    {
        [Serializable]
        public class OverworldData
        {
            public string Name { get; set; }
            public int[,] Biomes { get; set; }
            public float[,] Erosion { get; set; }
            public float[,] Faults { get; set; }
            public float[,] Height { get; set; }
            public float[,] Rainfall { get; set; }
            public float[,] Temperature { get; set; }
            public int[,] Water { get; set; }
            public float[,] Weathering { get; set; }

            [Newtonsoft.Json.JsonIgnore] [NonSerialized] public Texture2D Screenshot;

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
                            Erosion =  Erosion[x, y],
                            Faults =  Faults[x, y],
                            Height =  Height[x, y],
                            Rainfall = Rainfall[x, y],
                            Temperature = Temperature[x, y],
                            Water = (Overworld.WaterType) (Water[x, y]),
                            Weathering =  Weathering[x, y]
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
                Overworld.TextureFromHeightMap("Height", mapData, Overworld.ScalarFieldType.Height, width, height, imageMutex, worldData, toReturn, WorldManager.SeaLevel);

                return toReturn;
            }

            public OverworldData()
            {
            }

            public OverworldData(GraphicsDevice device, Overworld.MapData[,] map, string name)
            {
                int sizeX = map.GetLength(0);
                int sizeY = map.GetLength(1);
                Biomes = new int[sizeX, sizeY];
                Erosion = new float[sizeX, sizeY];
                Faults = new float[sizeX, sizeY];
                Rainfall = new float[sizeX, sizeY];
                Temperature = new float[sizeX, sizeY];
                Water = new int[sizeX, sizeY];
                Weathering = new float[sizeX, sizeY];
                Height = new float[sizeX, sizeY];
                Name = name;

                for(int x = 0; x < sizeX; x++)
                {
                    for(int y = 0; y < sizeY; y++)
                    {
                        Overworld.MapData data = map[x, y];
                        Biomes[x, y] =  (int)data.Biome;
                        Erosion[x, y] =  (data.Erosion);
                        Faults[x, y] = (data.Faults);
                        Height[x, y] = (data.Height);
                        Rainfall[x, y] =  (data.Rainfall);
                        Temperature[x, y] = (data.Temperature);
                        Water[x, y] = (int)(data.Water);
                        Weathering[x, y] =  (data.Weathering);
                    }
                }

                Screenshot = CreateTexture(device, sizeX, sizeY);
            }
        }

        public OverworldData Data { get; set; }

        public static string Extension = "world";
        public static string CompressedExtension = "zworld";

        public OverworldFile()
        {
        }

        public OverworldFile(GraphicsDevice device, Overworld.MapData[,] map, string name)
        {
            Data = new OverworldData(device, map, name);
        }

        public OverworldFile(string fileName, bool isCompressed, bool isBinary)
        {
            ReadFile(fileName, isCompressed, isBinary);
        }


        public void CopyFrom(OverworldFile file)
        {
            Data = file.Data;
        }

        public  bool ReadFile(string filePath, bool isCompressed, bool isBinary)
        {
            if (!isBinary)
            {
                OverworldFile file = FileUtils.LoadJson<OverworldFile>(filePath, isCompressed);

                if (file == null)
                {
                    return false;
                }
                else
                {
                    CopyFrom(file);
                    return true;
                }
            }
            else
            {
                OverworldFile file = FileUtils.LoadBinary<OverworldFile>(filePath);

                if (file == null)
                {
                    return false;
                }
                else
                {
                    CopyFrom(file);
                    return true;
                }
            }
        }

        public void SaveScreenshot(string filename)
        {
            Data.Screenshot.SaveAsPng(new System.IO.FileStream(filename, System.IO.FileMode.Create), Data.Screenshot.Width, Data.Screenshot.Height);
        }

        public bool WriteFile(string filePath, bool compress, bool binary)
        {
            if (!binary)
                return FileUtils.SaveJSon<OverworldFile>(this, filePath, compress);
            else
            {
                return FileUtils.SaveBinary<OverworldFile>(this, filePath);
            }
        }
    }

}