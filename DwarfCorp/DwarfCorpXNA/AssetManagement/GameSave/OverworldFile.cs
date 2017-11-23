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
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.ComponentModel;

namespace DwarfCorp
{
    [Serializable]
    public class NewOverworldFile
    {
        [Serializable]
        public class OverworldData
        {
            public string Version;
            public string Name;
            public float SeaLevel;
            [JsonIgnore] [NonSerialized] public Overworld.MapData[,] Data;
            
            [Serializable]
            public struct FactionDescriptor
            {
                public string Name { get; set; }
                public byte Id { get; set; }
                public string Race { get; set; }
                public Color PrimaryColory { get; set; }
                public Color SecondaryColor { get; set; }
                public int CenterX { get; set; }
                public int CenterY { get; set; }
            }

            public List<FactionDescriptor> FactionList;

            [JsonIgnore] [NonSerialized] public Texture2D Screenshot;

            public Overworld.MapData[,] CreateMap()
            {
                return Data;
            }

            public Texture2D CreateTexture(GraphicsDevice device, int width, int height, float seaLevel)
            {
                Texture2D toReturn = null;
                Overworld.MapData[,] mapData = CreateMap();
                toReturn = new Texture2D(device, width, height);
                System.Threading.Mutex imageMutex = new System.Threading.Mutex();
                Color[] worldData = new Color[width * height];
                Overworld.TextureFromHeightMap("Height", mapData, Overworld.ScalarFieldType.Height, width, height, imageMutex, worldData, toReturn, seaLevel);

                return toReturn;
            }

            public Texture2D CreateSaveTexture(GraphicsDevice Device, int Width, int Height)
            {
                var r = new Texture2D(Device, Width, Height);
                var data = new Color[Width * Height];
                Overworld.GenerateSaveTexture(Data, Width, Height, data);
                r.SetData(data);
                return r;
            }

            public void LoadFromTexture(Texture2D Texture)
            {
                Data = new Overworld.MapData[Texture.Width, Texture.Height];
                var colorData = new Color[Texture.Width * Texture.Height];
                Texture.GetData(colorData);
                Overworld.DecodeSaveTexture(Data, Texture.Width, Texture.Height, colorData);
            }

            public OverworldData()
            {
            }

            public OverworldData(GraphicsDevice device, Overworld.MapData[,] map, string name, float seaLevel)
            {
                int sizeX = map.GetLength(0);
                int sizeY = map.GetLength(1);
                
                Name = name;
                SeaLevel = seaLevel;
                Data = map;
                
                Screenshot = CreateTexture(device, sizeX, sizeY, seaLevel);
                FactionList = new List<FactionDescriptor>();
                byte id = 0;
                foreach (Faction f in Overworld.NativeFactions)
                {
                    FactionList.Add(new FactionDescriptor()
                    {
                        Name = f.Name,
                        PrimaryColory = f.PrimaryColor,
                        SecondaryColor = f.SecondaryColor,
                        Id = id,
                        Race = f.Race.Name,
                        CenterX = f.Center.X,
                        CenterY = f.Center.Y, 
                    });
                    id++;
                }
            }
        }

        public OverworldData Data { get; set; }
        private GraphicsDevice Device;
        private int Width;
        private int Height;

        public NewOverworldFile()
        {
        }

        public NewOverworldFile(GraphicsDevice device, Overworld.MapData[,] map, string name, float seaLevel)
        {
            this.Device = device;

            var worldFilePath = name + System.IO.Path.DirectorySeparatorChar + "world.png";
            var metaFilePath = name + System.IO.Path.DirectorySeparatorChar + "meta.txt";
            if (File.Exists(worldFilePath) && File.Exists(metaFilePath))
            {
                // Do nothing since overworlds should be saved precisely once.
                return;
            }

            Data = new OverworldData(device, map, name, seaLevel);
            Width = map.GetLength(0);
            Height = map.GetLength(1);
        }

        public NewOverworldFile(string fileName)
        {
            ReadFile(fileName);
        }

        public bool ReadFile(string filePath)
        {
            var worldFilePath = filePath + System.IO.Path.DirectorySeparatorChar + "world.png";
            var metaFilePath = filePath + System.IO.Path.DirectorySeparatorChar + "meta.txt";

            Data = FileUtils.LoadJson<OverworldData>(metaFilePath, false);

            var worldTexture = TextureManager.LoadInstanceTexture(worldFilePath);
            Data.LoadFromTexture(worldTexture);

            return true;
        }
        
        public void SaveScreenshot(string filename)
        {
            using (var stream = new System.IO.FileStream(filename, System.IO.FileMode.Create))
            {
                Data.Screenshot.SaveAsPng(stream, Data.Screenshot.Width, Data.Screenshot.Height);
            }
        }

        public bool WriteFile(string filePath)
        {
            var worldFilePath = filePath + System.IO.Path.DirectorySeparatorChar + "world.png";
            var metaFilePath = filePath + System.IO.Path.DirectorySeparatorChar + "meta.txt";

            if (File.Exists(worldFilePath) && File.Exists(metaFilePath))
            {
                Console.Out.WriteLine("Overworld {0} already exists. Just assuming it is correct.", worldFilePath);
                return false;
            }

            // Write meta info
            Data.Version = Program.Version;
            FileUtils.SaveJSon(Data, metaFilePath, false);

            // Save Image
            var texture = Data.CreateSaveTexture(Device, Width, Height);
            using (var stream = new System.IO.FileStream(worldFilePath, System.IO.FileMode.Create))
            {
                texture.SaveAsPng(stream, Width, Height);
            }
           
            return true;
        }
    }
}
