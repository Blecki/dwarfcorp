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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class GameFile
    {
        public static string Extension = "game";
        public static string CompressedExtension = "zgame";

        public GameFile(string overworld, int id)
        {
            Data = new GameData
            {
                Metadata =
                {
                    OverworldFile = overworld,
                    WorldOrigin = PlayState.WorldOrigin,
                    WorldScale = PlayState.WorldScale,
                    TimeOfDay = PlayState.Sky.TimeOfDay,
                    ChunkHeight = PlayState.ChunkHeight,
                    ChunkWidth = PlayState.ChunkWidth,
                    GameID = id,
                    Time = PlayState.Time
                },
                Camera = PlayState.Camera,
                Components = PlayState.ComponentManager,
                ChunkData = new List<ChunkFile>(),
                GameID = id,
            };


            foreach (
                ChunkFile file in PlayState.ChunkManager.ChunkData.ChunkMap.Select(pair => new ChunkFile(pair.Value)))
            {
                Data.ChunkData.Add(file);
            }
        }

        public GameFile(string file, bool compressed)
        {
            Data = new GameData();
            ReadFile(file, compressed);
        }

        public GameFile()
        {
            Data = new GameData();
        }

        public GameData Data { get; set; }

        public virtual string GetExtension()
        {
            return "game";
        }

        public virtual string GetCompressedExtension()
        {
            return "zgame";
        }

        public void CopyFrom(GameFile file)
        {
            Data = file.Data;
        }

        public bool LoadComponents(string filePath)
        {
            string[] componentFiles = SaveData.GetFilesInDirectory(filePath, true, "zcomp", "zcomp");
            if (componentFiles.Length > 0)
            {
                Data.Components = FileUtils.LoadJson<ComponentManager>(componentFiles[0], true);
            }
            else
            {
                return false;
            }

            return true;
        }

        public bool ReadFile(string filePath, bool isCompressed)
        {
            if (!Directory.Exists(filePath))
            {
                return false;
            }
            string[] screenshots = SaveData.GetFilesInDirectory(filePath, false, "png", "png");
            string[] metaFiles = SaveData.GetFilesInDirectory(filePath, isCompressed, MetaData.CompressedExtension,
                MetaData.Extension);
            string[] cameraFiles = SaveData.GetFilesInDirectory(filePath, false, "json", "json");

            if (metaFiles.Length > 0)
            {
                Data.Metadata = new MetaData(metaFiles[0], isCompressed);
                Data.GameID = Data.Metadata.GameID;
            }
            else
            {
                return false;
            }

            if (cameraFiles.Length > 0)
            {
                Data.Camera = FileUtils.LoadJson<OrbitCamera>(cameraFiles[0], false);
            }
            else
            {
                return false;
            }

            string[] chunkDirs = Directory.GetDirectories(filePath, "Chunks");

            if (chunkDirs.Length > 0)
            {
                string chunkDir = chunkDirs[0];

                string[] chunks = SaveData.GetFilesInDirectory(chunkDir, isCompressed, ChunkFile.CompressedExtension,
                    ChunkFile.Extension);
                Data.ChunkData = new List<ChunkFile>();
                foreach (string chunk in chunks)
                {
                    Data.ChunkData.Add(new ChunkFile(chunk, isCompressed, true));
                }
            }
            else
            {
                return false;
            }

            if (screenshots.Length > 0)
            {
                string screenshot = screenshots[0];
                Data.Screenshot = TextureManager.LoadInstanceTexture(screenshot);
            }

            return true;
        }

        public bool WriteFile(string filePath, bool compress)
        {
            Data.SaveToDirectory(filePath);
            return true;
        }

        public class GameData : SaveData
        {
            public GameData()
            {
                Metadata = new MetaData();
            }

            public Texture2D Screenshot { get; set; }
            public List<ChunkFile> ChunkData { get; set; }
            public MetaData Metadata { get; set; }

            public OrbitCamera Camera { get; set; }

            public ComponentManager Components { get; set; }

            public int GameID { get; set; }

            public void SaveToDirectory(string directory)
            {
                Directory.CreateDirectory(directory);
                Directory.CreateDirectory(directory + ProgramData.DirChar + "Chunks");

                foreach (ChunkFile chunk in ChunkData)
                {
                    chunk.WriteFile(
                        directory + ProgramData.DirChar + "Chunks" + ProgramData.DirChar + chunk.ID.X + "_" + chunk.ID.Y +
                        "_" + chunk.ID.Z + "." + ChunkFile.CompressedExtension, true, true);
                }

                Metadata.WriteFile(directory + ProgramData.DirChar + "MetaData." + MetaData.CompressedExtension, true);

                FileUtils.SaveJSon(Camera, directory + ProgramData.DirChar + "Camera." + "json", false);

                FileUtils.SaveJSon(Components, directory + ProgramData.DirChar + "Components." + "zcomp", true);
            }
        }

        public class MetaData
        {
            public static string Extension = "meta";
            public static string CompressedExtension = "zmeta";

            public MetaData(string file, bool compressed)
            {
                ReadFile(file, compressed);
            }


            public MetaData()
            {
            }

            public string OverworldFile { get; set; }
            public float WorldScale { get; set; }
            public Vector2 WorldOrigin { get; set; }
            public int ChunkWidth { get; set; }
            public int ChunkHeight { get; set; }
            public float TimeOfDay { get; set; }
            public int GameID { get; set; }
            public WorldTime Time { get; set; }

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

            public bool ReadFile(string filePath, bool isCompressed)
            {
                var file = FileUtils.LoadJson<MetaData>(filePath, isCompressed);

                if (file == null)
                {
                    return false;
                }
                CopyFrom(file);
                return true;
            }

            public bool WriteFile(string filePath, bool compress)
            {
                return FileUtils.SaveJSon(this, filePath, compress);
            }
        }
    }
}