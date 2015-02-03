using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorpCore;
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
                    chunk.WriteFile(directory + ProgramData.DirChar + "Chunks" + ProgramData.DirChar + chunk.ID.X + "_" + chunk.ID.Y + "_" + chunk.ID.Z + "." + ChunkFile.CompressedExtension, true);
                }

                Metadata.WriteFile(directory + ProgramData.DirChar + "MetaData." + MetaData.CompressedExtension, true);
                
                FileUtils.SaveJSon(Camera, directory + ProgramData.DirChar + "Camera." + "json", false);

                FileUtils.SaveJSon(Components, directory + ProgramData.DirChar + "Components." + "zcomp", true);
            }
        }

        public GameData Data { get; set; }

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
                    GameID = id
                },
                Camera = PlayState.Camera,
                Components = PlayState.ComponentManager,
                ChunkData = new List<ChunkFile>(),
                GameID = id
            };


            foreach(ChunkFile file in PlayState.ChunkManager.ChunkData.ChunkMap.Select(pair => new ChunkFile(pair.Value)))
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

        public GameFile(string file, bool compressed)
        {
            Data = new GameData();
            ReadFile(file, compressed);
        }

        public GameFile()
        {
            Data = new GameData();
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

        public  bool ReadFile(string filePath, bool isCompressed)
        {
            if(!System.IO.Directory.Exists(filePath))
            {
                return false;
            }
            else
            {
                string[] screenshots = SaveData.GetFilesInDirectory(filePath, false, "png", "png");
                string[] metaFiles = SaveData.GetFilesInDirectory(filePath, isCompressed, GameFile.MetaData.CompressedExtension, GameFile.MetaData.Extension);
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
                    Data.Camera = FileUtils.LoadJson<OrbitCamera>(cameraFiles[0], false);
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
                        Data.ChunkData.Add(new ChunkFile(chunk, isCompressed));
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

            public new static string Extension = "meta";
            public new static string CompressedExtension = "zmeta";

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