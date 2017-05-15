// GameFile.cs
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{

    public class GameFile 
    {
        public class GameData : SaveData
        {
            public Texture2D Screenshot { get; set; }
            public List<ChunkFile> ChunkData { get; set; }
            public MetaData Metadata { get; set; }

            public OrbitCamera Camera { get; set; }

            public ComponentManager Components { get; set; }

            public List<Goals.Goal> Goals { get; set; }

            public int GameID { get; set; }

            public GameData()
            {
                Metadata = new MetaData();
            }

            public void SaveToDirectory(string directory)
            {
                System.IO.Directory.CreateDirectory(directory);
                System.IO.Directory.CreateDirectory(directory + ProgramData.DirChar + "Chunks");

                foreach(ChunkFile chunk in ChunkData)
                {
                    chunk.WriteFile(directory + ProgramData.DirChar + "Chunks" + ProgramData.DirChar + chunk.ID.X + "_" + chunk.ID.Y + "_" + chunk.ID.Z + "." + (DwarfGame.COMPRESSED_BINARY_SAVES ? ChunkFile.CompressedExtension : ChunkFile.Extension), DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES);
                }

                Metadata.WriteFile(directory + ProgramData.DirChar + "MetaData." + (DwarfGame.COMPRESSED_BINARY_SAVES ? MetaData.CompressedExtension : MetaData.Extension), 
                    DwarfGame.COMPRESSED_BINARY_SAVES);

                FileUtils.SaveJSon(Camera, directory + ProgramData.DirChar + "Camera." + (DwarfGame.COMPRESSED_BINARY_SAVES ? GameFile.CompressedExtension : GameFile.Extension), DwarfGame.COMPRESSED_BINARY_SAVES);

                FileUtils.SaveJSon(Components, directory + ProgramData.DirChar + "Components." 
                    + (DwarfGame.COMPRESSED_BINARY_SAVES ? GameFile.CompressedExtension : GameFile.Extension), DwarfGame.COMPRESSED_BINARY_SAVES);

                FileUtils.SaveJSon(Goals, directory + ProgramData.DirChar + "Goals."
                     + (DwarfGame.COMPRESSED_BINARY_SAVES ? GameFile.CompressedExtension : GameFile.Extension), DwarfGame.COMPRESSED_BINARY_SAVES);
            }
        }

        public GameData Data { get; set; }
       
        public static string Extension = "json";
        public static string CompressedExtension = "zip";

        public GameFile(string overworld, int id, WorldManager world)
        {
            Data = new GameData
            {
                Metadata =
                {
                    OverworldFile = overworld,
                    WorldOrigin = world.WorldOrigin,
                    WorldScale = world.WorldScale,
                    TimeOfDay = world.Sky.TimeOfDay,
                    ChunkHeight = world.ChunkHeight,
                    ChunkWidth = world.ChunkWidth,
                    GameID = id,
                    Time = world.Time
                },
                Camera = world.Camera,
                Components = world.ComponentManager,
                ChunkData = new List<ChunkFile>(),
                Goals = world.Master.GoalManager.EnumerateGoals().ToList(),
                GameID = id,
            };


            foreach (ChunkFile file in world.ChunkManager.ChunkData.ChunkMap.Select(pair => new ChunkFile(pair.Value)))
            {
                Data.ChunkData.Add(file);
            }

        }

        public virtual string GetExtension()
        {
            return "game";
        }

        public virtual string GetCompressedExtension()
        {
            return "zgame";
        }

        public GameFile(string file, bool compressed, WorldManager world)
        {
            Data = new GameData();
            ReadFile(file, compressed, world);
        }

        public GameFile()
        {
            Data = new GameData();
        }

        public void CopyFrom(GameFile file)
        {
            Data = file.Data;
        }

        public bool LoadComponents(string filePath, WorldManager world)
        {
            string[] componentFiles = SaveData.GetFilesInDirectory(filePath, DwarfGame.COMPRESSED_BINARY_SAVES, "Components", GameFile.CompressedExtension, GameFile.Extension);
            if (componentFiles.Length > 0)
            {
                Data.Components = FileUtils.LoadJson<ComponentManager>(componentFiles[0], DwarfGame.COMPRESSED_BINARY_SAVES, world);
            }
            else
            {
                return false;
            }

            return true;
        }

        public  bool ReadFile(string filePath, bool isCompressed, WorldManager world)
        {
            if(!System.IO.Directory.Exists(filePath))
            {
                return false;
            }
            else
            {
                string[] screenshots = SaveData.GetFilesInDirectory(filePath, false, "png", "png");
                string[] metaFiles = SaveData.GetFilesInDirectory(filePath, isCompressed, "MetaData", GameFile.MetaData.CompressedExtension, GameFile.MetaData.Extension);
                string[] cameraFiles = SaveData.GetFilesInDirectory(filePath, false, "json", "json");

                if(metaFiles.Length > 0)
                {
                    Data.Metadata = new MetaData(metaFiles[0], isCompressed);
                    Data.GameID = Data.Metadata.GameID;
                }
                else
                {
                    return false;
                }

                if(cameraFiles.Length > 0)
                {
                    Data.Camera = FileUtils.LoadJson<OrbitCamera>(cameraFiles[0], false, world);
                }
                else
                {
                    return false;
                }

                string[] chunkDirs = System.IO.Directory.GetDirectories(filePath, "Chunks");

                if(chunkDirs.Length > 0)
                {
                    string chunkDir = chunkDirs[0];

                    string[] chunks = SaveData.GetFilesInDirectory(chunkDir, isCompressed, ChunkFile.CompressedExtension, ChunkFile.Extension);
                    Data.ChunkData = new List<ChunkFile>();
                    foreach(string chunk in chunks)
                    {
                        Data.ChunkData.Add(new ChunkFile(chunk, isCompressed, DwarfGame.COMPRESSED_BINARY_SAVES));
                    }
                }
                else
                {
                    return false;
                }

                if(screenshots.Length > 0)
                {
                    string screenshot = screenshots[0];
                    Data.Screenshot = TextureManager.LoadInstanceTexture(screenshot);
                }

                return true;
            }
        }

        public bool WriteFile(string filePath, bool compress)
        {
            Data.SaveToDirectory(filePath);
            return true;
        }

        public class MetaData 
        {
            public string OverworldFile { get; set; }
            public float WorldScale { get; set; }
            public Vector2 WorldOrigin { get; set; }
            public int ChunkWidth { get; set; }
            public int ChunkHeight { get; set; }
            public float TimeOfDay { get; set; }
            public int GameID { get; set; }
            public WorldTime Time { get; set; }
            public static string Extension = "meta";
            public static string CompressedExtension = "zmeta";

            public MetaData(string file, bool compressed)
            {
                ReadFile(file, compressed);
            }


            public MetaData()
            {
            }

            public void CopyFrom(MetaData file)
            {
                WorldScale = file.WorldScale;
                WorldOrigin = file.WorldOrigin;
                ChunkWidth = file.ChunkWidth;
                ChunkHeight = file.ChunkHeight;
                TimeOfDay = file.TimeOfDay;
                GameID = file.GameID;
                OverworldFile = file.OverworldFile;
                Time = file.Time;
            }

            public  bool ReadFile(string filePath, bool isCompressed)
            {
                MetaData file = FileUtils.LoadJson<MetaData>(filePath, isCompressed);

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

            public  bool WriteFile(string filePath, bool compress)
            {
                return FileUtils.SaveJSon(this, filePath, compress);
            }
        }
    }

}
