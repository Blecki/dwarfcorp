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
            public string Name;
            public float SeaLevel;
            public Overworld.MapData[,] Data;
            
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

        public static string Extension = "world";
        public static string CompressedExtension = "zworld";

        public NewOverworldFile()
        {
        }

        public NewOverworldFile(GraphicsDevice device, Overworld.MapData[,] map, string name, float seaLevel)
        {
            Data = new OverworldData(device, map, name, seaLevel);
        }

        public NewOverworldFile(string fileName)
        {
            ReadFile(fileName);
        }

        public bool ReadFile(string filePath)
        {
            // If no meta data, fall back to old save style (pass true for compressed and binary)
            //DwarfGame.COMPRESSED_BINARY_SAVES

            var worldFilePath = filePath + System.IO.Path.DirectorySeparatorChar + "world.zworld";
            var metaFilePath = filePath + System.IO.Path.DirectorySeparatorChar + "meta.txt";

            if (System.IO.File.Exists(metaFilePath))
            {
                var metaInfo = System.IO.File.ReadAllText(metaFilePath);

                var fileStream = new FileStream(worldFilePath, FileMode.Open);
                var zip = new ZipInputStream(fileStream);
                zip.GetNextEntry();
                var formatter = new BinaryFormatter();
                var file = formatter.Deserialize(zip) as OverworldData;

                if (file == null)
                    return false;

                Data = file;
                return true;
            }
            else
            {
                var oldWorldFile = new OverworldFile(filePath + Path.DirectorySeparatorChar + "world." +
                    (DwarfGame.COMPRESSED_BINARY_SAVES ? OverworldFile.CompressedExtension : OverworldFile.Extension), DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES);

                Data = new OverworldData();
                Data.Data = oldWorldFile.Data.CreateMap();
                Data.FactionList = oldWorldFile.Data.FactionList;
                Data.Name = oldWorldFile.Data.Name;
                Data.SeaLevel = oldWorldFile.Data.SeaLevel;

                return true;
            }
        }

        public void SaveScreenshot(string filename)
        {
            Data.Screenshot.SaveAsPng(new System.IO.FileStream(filename, System.IO.FileMode.Create), Data.Screenshot.Width, Data.Screenshot.Height);
        }

        public bool WriteFile(string filePath)
        {
            var worldFilePath = filePath + System.IO.Path.DirectorySeparatorChar + "world.zworld";
            var metaFilePath = filePath + System.IO.Path.DirectorySeparatorChar + "meta.txt";
            // Don't use zips in FNA version
            var fileStream = new FileStream(worldFilePath, FileMode.OpenOrCreate);
            var zip = new ZipOutputStream(fileStream);
            var formatter = new BinaryFormatter();
            zip.SetLevel(9);
            var entry = new ZipEntry(Path.GetFileName(filePath));
            zip.PutNextEntry(entry);

            formatter.Serialize(zip, Data);
            zip.CloseEntry();
            zip.Close();
            fileStream.Close();

            System.IO.File.WriteAllText(metaFilePath, Program.Version);

            return true;
        }
    }
}
