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
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SaveGame
    {
        public Texture2D Screenshot { get; set; }
        public List<ChunkFile> ChunkData { get; set; }
        public MetaData Metadata { get; set; }
        public PlayData PlayData { get; set; }

        public void WriteFile(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            System.IO.Directory.CreateDirectory(directory + ProgramData.DirChar + "Chunks");

            foreach (ChunkFile chunk in ChunkData)
            {
                chunk.WriteFile(directory + ProgramData.DirChar + "Chunks" + ProgramData.DirChar + chunk.ID.X + "_" + chunk.ID.Y + "_" + chunk.ID.Z + "." + (DwarfGame.COMPRESSED_BINARY_SAVES ? ChunkFile.CompressedExtension : ChunkFile.Extension), DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES);
            }

            FileUtils.SaveJSon(this.Metadata, directory + ProgramData.DirChar + "Metadata." + MetaData.Extension, false);
            FileUtils.SaveJSon(this.PlayData, directory + ProgramData.DirChar + "World." + PlayData.Extension, DwarfGame.COMPRESSED_BINARY_SAVES);
        }

        private SaveGame() { }
        
        public bool ReadChunks(string filePath)
        {
            string[] chunkDirs = System.IO.Directory.GetDirectories(filePath, "Chunks");


            if (chunkDirs.Length > 0)
            {
                var chunkFiles = Directory.GetFiles(chunkDirs[0], "*." + (DwarfGame.COMPRESSED_BINARY_SAVES ? ChunkFile.CompressedExtension : ChunkFile.Extension));
                ChunkData = new List<ChunkFile>();
                foreach (string chunk in chunkFiles)
                {
                    ChunkData.Add(new ChunkFile(chunk, DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES));
                }
            }
            else
            {
                Console.Error.WriteLine("Can't load chunks {0}, no chunks found", filePath);
                return false;
            }
            return true;
        }

        public bool LoadPlayData(string filePath, WorldManager world)
        {
            string[] worldFiles = System.IO.Directory.GetFiles(filePath, "*." + PlayData.Extension);

            if (worldFiles.Length > 0)
                PlayData = FileUtils.LoadJson<PlayData>(worldFiles[0], DwarfGame.COMPRESSED_BINARY_SAVES,
                    world);
            else
            {
                Console.Error.WriteLine("Can't load world from {0}, no data file found.", filePath);
                return false;
            }
            return true;
        }

        private bool ReadMetadata(string filePath)
        {
            if (!System.IO.Directory.Exists(filePath))
            {
                return false;
            }
            else
            {
                string[] metaFiles = System.IO.Directory.GetFiles(filePath, "*." + MetaData.Extension);

                if (metaFiles.Length > 0)
                    Metadata = FileUtils.LoadJson<MetaData>(metaFiles[0], false, null);
                else
                {
                    Console.Error.WriteLine("Can't load file {0}, no metadata found", filePath);
                    return false;
                }

                string[] screenshots = System.IO.Directory.GetFiles(filePath, "*.png");

                if (screenshots.Length > 0)
                    Screenshot = TextureManager.LoadInstanceTexture(screenshots[0], false);

                return true;
            }
        }

        public static string GetLatestSaveFile()
        {
            DirectoryInfo saveDirectory = Directory.CreateDirectory(DwarfGame.GetSaveDirectory());

            DirectoryInfo newest = null;
            foreach (var dir in saveDirectory.EnumerateDirectories())
            {
                if (newest == null || newest.CreationTime < dir.CreationTime)
                {
                    var valid = false;
                    try
                    {
                        var saveGame = SaveGame.CreateFromDirectory(dir.FullName);
                        valid = Program.CompatibleVersions.Contains(saveGame.Metadata.Version);
                    }
                    catch (Exception e)
                    { }

                    if (valid) newest = dir;
                }
            }

            return newest == null ? null : newest.FullName;
        }

        public static SaveGame CreateFromWorld(WorldManager World)
        {
            var r = new SaveGame
            {
                Metadata = new MetaData
                {
                    OverworldFile = Overworld.Name,
                    WorldOrigin = World.WorldOrigin,
                    WorldScale = World.WorldScale,
                    TimeOfDay = World.Sky.TimeOfDay,
                    GameID = World.GameID,
                    Time = World.Time,
                    Slice = (int)World.ChunkManager.ChunkData.MaxViewingLevel,
                    NumChunks = World.ChunkManager.WorldSize,
                    Version = Program.Version
                },
                PlayData = PlayData.CreateFromWorld(World),
                ChunkData = new List<ChunkFile>(),
            };

            foreach (ChunkFile file in World.ChunkManager.ChunkData.GetChunkEnumerator().Select(c => new ChunkFile(c)))
                r.ChunkData.Add(file);

            return r;
        }

        public static SaveGame CreateFromDirectory(String Directory)
        {
            var r = new SaveGame();
            if (r.ReadMetadata(Directory))
                return r;
            return null;
        }
    }
}
