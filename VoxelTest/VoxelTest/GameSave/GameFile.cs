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
    public class GameFile : SaveData
    {
        public class GameData
        {
            public PlayerFile PlayerData { get; set; }
            public EconomyFile EconomyData { get; set; }
            public CompanyFile CompanyData { get; set; }
            public List<ChunkFile> ChunkData { get; set; }
            public List<EntityFile> EntityData { get; set; }
            public MetaData Metadata { get; set; }

            public GameData()
            {
                Metadata = new MetaData();
            }

            public class MetaData : SaveData
            {
                public string OverworldFile { get; set; }
                public float WorldScale { get; set; }
                public Vector2 WorldOrigin { get; set; }
                public int ChunkWidth { get; set; }
                public int ChunkHeight { get; set; }
                public float TimeOfDay { get; set; }
                public Vector3 CameraPosition { get; set; }
                public Vector2 CameraRotation { get; set; }

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
                    CameraPosition = file.CameraPosition;
                    CameraRotation = file.CameraRotation;
                    OverworldFile = file.OverworldFile;
                }

                public override bool ReadFile(string filePath, bool isCompressed)
                {
                    MetaData file = FileUtils.LoadJson<MetaData>(filePath, isCompressed);

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

                public override bool WriteFile(string filePath, bool compress)
                {
                    return FileUtils.SaveJSon<MetaData>(this, filePath, compress);
                }
            }

            public void SaveToDirectory(string directory)
            {
                PlayerData.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Player." + PlayerFile.CompressedExtension, true);
                EconomyData.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Economy." + EconomyFile.CompressedExtension, true);
                CompanyData.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Company." + CompanyFile.CompressedExtension, true);

                System.IO.Directory.CreateDirectory(directory + System.IO.Path.DirectorySeparatorChar + "Chunks");

                foreach (ChunkFile chunk in ChunkData)
                {
                    chunk.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Chunks" + System.IO.Path.DirectorySeparatorChar + chunk.ID.X + "_" + chunk.ID.Y + "_" + chunk.ID.Z + "." + ChunkFile.CompressedExtension, true);
                }


                System.IO.Directory.CreateDirectory(directory + System.IO.Path.DirectorySeparatorChar + "Entitites");

                foreach (EntityFile ent in EntityData)
                {
                    ent.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Entitites" + System.IO.Path.DirectorySeparatorChar + ent.Type + ent.ID + "." + EntityFile.CompressedExtension, true);
                }

                Metadata.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "MetaData." + MetaData.CompressedExtension, true);
            }
        }

        public GameData Data { get; set; }



        public new static string Extension = "game";
        public new static string CompressedExtension = "zgame"; 

        public GameFile(PlayState playState, string overworld)
        {
            Data = new GameData();
            
            Data.EconomyData = new EconomyFile(PlayState.master.Economy);
            Data.CompanyData = new CompanyFile(PlayerSettings.Default.CompanyName, PlayerSettings.Default.CompanyLogo, PlayerSettings.Default.CompanyMotto);

            Data.Metadata.OverworldFile = overworld;
            Data.Metadata.WorldOrigin = PlayState.WorldOrigin;
            Data.Metadata.WorldScale = PlayState.WorldScale;
            Data.Metadata.TimeOfDay = PlayState.Sky.TimeOfDay;
            Data.Metadata.ChunkHeight = GameSettings.Default.ChunkHeight;
            Data.Metadata.ChunkWidth = GameSettings.Default.ChunkWidth;
            Data.Metadata.CameraPosition = PlayState.camera.Position;
            Data.Metadata.CameraRotation = new Vector2(PlayState.camera.Phi, PlayState.camera.Theta);

            Data.EntityData = new List<EntityFile>();

            foreach(GameComponent component in PlayState.componentManager.Components.Values)
            {
                if (component is LocatableComponent && component.Parent == PlayState.componentManager.RootComponent)
                {
                    LocatableComponent loc = (LocatableComponent)component;

                    EntityFile entityFile = new EntityFile(component.GlobalID, component.Name, loc.GlobalTransform, loc.LocalTransform.M44);
                    Data.EntityData.Add(entityFile);
                }
            }

            Data.ChunkData = new List<ChunkFile>();

            foreach (KeyValuePair<Point3, VoxelChunk> pair in PlayState.chunkManager.ChunkMap)
            {
                ChunkFile file = new ChunkFile(pair.Value);
                Data.ChunkData.Add(file);
            }

            Data.PlayerData = new PlayerFile(PlayState.master);
        }

        public virtual string GetExtension() { return "game"; }
        public virtual string GetCompressedExtension() { return "zgame"; }

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

        public override bool ReadFile(string filePath, bool isCompressed)
        {
            if (!System.IO.Directory.Exists(filePath))
            {
                return false;
            }
            else
            {
                string[] metaFiles = GameFile.GameData.MetaData.GetFilesInDirectory(filePath, isCompressed, GameFile.GameData.MetaData.CompressedExtension, GameFile.GameData.MetaData.Extension);
                string[] companyFiles = CompanyFile.GetFilesInDirectory(filePath, isCompressed, CompanyFile.CompressedExtension, CompanyFile.Extension);
                string[] economyFiles = EconomyFile.GetFilesInDirectory(filePath, isCompressed, EconomyFile.CompressedExtension, EconomyFile.Extension);
                string[] playerFiles = PlayerFile.GetFilesInDirectory(filePath, isCompressed, PlayerFile.CompressedExtension, PlayerFile.Extension);

                if (metaFiles.Length > 0)
                {
                    Data.Metadata = new GameData.MetaData(metaFiles[0], isCompressed);
                }
                else
                {
                    return false;
                }

                if (companyFiles.Length > 0)
                {
                    Data.CompanyData = new CompanyFile(companyFiles[0], isCompressed);
                }
                else
                {
                    return false;
                }

                if (economyFiles.Length > 0)
                {
                    Data.EconomyData = new EconomyFile(economyFiles[0], isCompressed);
                }
                else
                {
                    return false;
                }

                if (playerFiles.Length > 0)
                {
                    Data.PlayerData = new PlayerFile(playerFiles[0], isCompressed);
                }
                else
                {
                    return false;
                }

                string[] chunkDirs = System.IO.Directory.GetDirectories(filePath, "Chunks");

                if (chunkDirs.Length > 0)
                {
                    string chunkDir = chunkDirs[0];

                    string[] chunks = ChunkFile.GetFilesInDirectory(chunkDir, isCompressed, ChunkFile.CompressedExtension, ChunkFile.Extension);
                    Data.ChunkData = new List<ChunkFile>();
                    foreach (string chunk in chunks)
                    {
                        Data.ChunkData.Add(new ChunkFile(chunk, isCompressed));
                    }
                }
                else
                {
                    return false;
                }

                string[] entDirs = System.IO.Directory.GetDirectories(filePath, "Entitites");

                if (entDirs.Length > 0)
                {
                    string entDir = entDirs[0];

                    string[] ents = EntityFile.GetFilesInDirectory(entDir, isCompressed, EntityFile.CompressedExtension, EntityFile.Extension);
                    Data.EntityData = new List<EntityFile>();
                    foreach (string ent in ents)
                    {
                        Data.EntityData.Add(new EntityFile(ent, isCompressed));
                    }
                }
                else
                {
                    return false;
                }

                return true;

            }
        }

        public override bool WriteFile(string filePath, bool compress)
        {
            Data.SaveToDirectory(filePath);
            return true;
        }

    }
}
