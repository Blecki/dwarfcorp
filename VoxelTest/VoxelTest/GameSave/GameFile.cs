using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

                public override sealed bool ReadFile(string filePath, bool isCompressed)
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

                public override bool WriteFile(string filePath, bool compress)
                {
                    return FileUtils.SaveJSon<MetaData>(this, filePath, compress);
                }
            }

            public void SaveToDirectory(string directory)
            {
                System.IO.Directory.CreateDirectory(directory);
                PlayerData.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Player." + PlayerFile.CompressedExtension, true);
                EconomyData.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Economy." + EconomyFile.CompressedExtension, true);
                CompanyData.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Company." + CompanyFile.CompressedExtension, true);

                System.IO.Directory.CreateDirectory(directory + System.IO.Path.DirectorySeparatorChar + "Chunks");

                foreach(ChunkFile chunk in ChunkData)
                {
                    chunk.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Chunks" + System.IO.Path.DirectorySeparatorChar + chunk.ID.X + "_" + chunk.ID.Y + "_" + chunk.ID.Z + "." + ChunkFile.CompressedExtension, true);
                }


                System.IO.Directory.CreateDirectory(directory + System.IO.Path.DirectorySeparatorChar + "Entitites");

                foreach(EntityFile ent in EntityData)
                {
                    ent.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "Entitites" + System.IO.Path.DirectorySeparatorChar + ent.Type + ent.ID + "." + EntityFile.CompressedExtension, true);
                }

                Metadata.WriteFile(directory + System.IO.Path.DirectorySeparatorChar + "MetaData." + MetaData.CompressedExtension, true);
            }
        }

        public GameData Data { get; set; }
        public Dictionary<uint, uint> IDMap { get; set; } 

        public new static string Extension = "game";
        public new static string CompressedExtension = "zgame";

        public GameFile(string overworld)
        {
            Data = new GameData
            {
                EconomyData = new EconomyFile(PlayState.Master.Economy),
                CompanyData = new CompanyFile(PlayerSettings.Default.CompanyName, PlayerSettings.Default.CompanyLogo, PlayerSettings.Default.CompanyMotto),
                Metadata =
                {
                    OverworldFile = overworld,
                    WorldOrigin = PlayState.WorldOrigin,
                    WorldScale = PlayState.WorldScale,
                    TimeOfDay = PlayState.Sky.TimeOfDay,
                    ChunkHeight = GameSettings.Default.ChunkHeight,
                    ChunkWidth = GameSettings.Default.ChunkWidth,
                    CameraPosition = PlayState.camera.Position,
                    CameraRotation = new Vector2(PlayState.camera.Phi, PlayState.camera.Theta)
                },
                EntityData = new List<EntityFile>(),
            };
            IDMap = new Dictionary<uint, uint>();
            foreach(EntityFile entityFile in from component in PlayState.ComponentManager.Components.Values
                where component is LocatableComponent && component.Parent == PlayState.ComponentManager.RootComponent && component.IsActive
                let loc = (LocatableComponent) component
                select new EntityFile(component.GlobalID, component.Name, loc.GlobalTransform, loc.LocalTransform.M44))
            {
                Data.EntityData.Add(entityFile);
            }

            Data.ChunkData = new List<ChunkFile>();

            foreach(ChunkFile file in PlayState.ChunkManager.ChunkMap.Select(pair => new ChunkFile(pair.Value)))
            {
                Data.ChunkData.Add(file);
            }

            Data.PlayerData = new PlayerFile(PlayState.Master);
        }

        public void CreateEntities(PlayState playState)
        {

            foreach (EntityFile file in Data.EntityData)
            {
                GameComponent component = file.CreateComponent(PlayState.ComponentManager, playState.GraphicsDevice, playState.Game.Content, PlayState.ChunkManager, PlayState.Master, PlayState.camera);

                if (component == null)
                {
                    continue;
                }

                PlayState.ComponentManager.AddComponent(component);
                IDMap[file.ID] = component.GlobalID;
            }
        }

        public static VoxelRef GetVoxelRef(VoxelPtr voxelPtr)
        {
            VoxelChunk chunk = PlayState.ChunkManager.ChunkMap[new Point3(voxelPtr.Ptr[0], voxelPtr.Ptr[1], voxelPtr.Ptr[2])];
            
            if(chunk == null)
            {
                return null;
            }

            List<VoxelRef> list = PlayState.ChunkManager.GetVoxelReferencesAtWorldLocation(chunk.Origin + new Vector3(voxelPtr.Ptr[3], voxelPtr.Ptr[4], voxelPtr.Ptr[5]));

            return list.Count == 0 ? null : list.ElementAt(0);
        }

        public  GameComponent GetEntity(EntityPtr entityPtr)
        {
            if(!IDMap.ContainsKey(entityPtr.ID))
            {
                return null;
            }

            uint globalID = IDMap[entityPtr.ID];
            return PlayState.ComponentManager.Components.ContainsKey(globalID) ? PlayState.ComponentManager.Components[globalID] : null;
        }

        public void CreatePlayerData(PlayState playState)
        {
            foreach(GameMaster.Designation designation in from des in Data.PlayerData.Data.DigDesignations
                select GetVoxelRef(des)
                into vref
                where vref != null
                select new GameMaster.Designation()
                {
                    numCreaturesAssigned = 0,
                    vox = vref
                })
            {
                PlayState.Master.DigDesignations.Add(designation);
            }

            foreach(GameMaster.Designation designation in from des in Data.PlayerData.Data.GuardDesignations
                select GetVoxelRef(des)
                into vref
                where vref != null
                select new GameMaster.Designation()
                {
                    numCreaturesAssigned = 0,
                    vox = vref
                })
            {
                PlayState.Master.GuardDesignations.Add(designation);
            }

            foreach(LocatableComponent loc in (from ent in Data.PlayerData.Data.ChopDesignations
                where IDMap.ContainsKey(ent.ID)
                select IDMap[ent.ID]
                into globalID
                where PlayState.ComponentManager.Components.ContainsKey(globalID)
                select PlayState.ComponentManager.Components[globalID]).OfType<LocatableComponent>())
            {
                PlayState.Master.ChopDesignations.Add(loc);
            }

            foreach (LocatableComponent loc in (from ent in Data.PlayerData.Data.GatherDesignations
                                                where IDMap.ContainsKey(ent.ID)
                                                select IDMap[ent.ID]
                                                    into globalID
                                                    where PlayState.ComponentManager.Components.ContainsKey(globalID)
                                                    select PlayState.ComponentManager.Components[globalID]).OfType<LocatableComponent>())
            {
                PlayState.Master.GatherDesignations.Add(loc);
            }

            foreach(ZoneData zone in Data.PlayerData.Data.Rooms)
            {
                List<VoxelRef> voxelRef = zone.Voxels.Select(GetVoxelRef).ToList();
                Room room = new Room(voxelRef, RoomLibrary.GetType(zone.Type), PlayState.ChunkManager)
                {
                    ID = zone.Name
                };

                PlayState.Master.RoomDesignator.DesignatedRooms.Add(room);

                foreach(EntityPtr ent in zone.AttachedEntities)
                {
                    room.Components.Add(GetEntity(ent));
                }
            }

            foreach(ZoneData zone in Data.PlayerData.Data.Stockpiles)
            {
                List<VoxelRef> voxelRef = zone.Voxels.Select(GetVoxelRef).ToList();
                Stockpile stock = new Stockpile(zone.Name, PlayState.ChunkManager);
                foreach(VoxelRef vRef in voxelRef)
                {
                    stock.AddVoxel(vRef);
                }

                PlayState.Master.Stockpiles.Add(stock);

                foreach (EntityPtr ent in zone.AttachedEntities)
                {
                    GameComponent component = GetEntity(ent);
                    stock.AddItem(Item.CreateItem(component.Name + component.GlobalID, stock, component as LocatableComponent), GetVoxelRef(ent.Voxel));
                }
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
            IDMap = new Dictionary<uint, uint>();
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

        public override sealed bool ReadFile(string filePath, bool isCompressed)
        {
            if(!System.IO.Directory.Exists(filePath))
            {
                return false;
            }
            else
            {
                string[] metaFiles = GameFile.GameData.MetaData.GetFilesInDirectory(filePath, isCompressed, GameFile.GameData.MetaData.CompressedExtension, GameFile.GameData.MetaData.Extension);
                string[] companyFiles = CompanyFile.GetFilesInDirectory(filePath, isCompressed, CompanyFile.CompressedExtension, CompanyFile.Extension);
                string[] economyFiles = EconomyFile.GetFilesInDirectory(filePath, isCompressed, EconomyFile.CompressedExtension, EconomyFile.Extension);
                string[] playerFiles = PlayerFile.GetFilesInDirectory(filePath, isCompressed, PlayerFile.CompressedExtension, PlayerFile.Extension);

                if(metaFiles.Length > 0)
                {
                    Data.Metadata = new GameData.MetaData(metaFiles[0], isCompressed);
                }
                else
                {
                    return false;
                }

                if(companyFiles.Length > 0)
                {
                    Data.CompanyData = new CompanyFile(companyFiles[0], isCompressed);
                }
                else
                {
                    return false;
                }

                if(economyFiles.Length > 0)
                {
                    Data.EconomyData = new EconomyFile(economyFiles[0], isCompressed);
                }
                else
                {
                    return false;
                }

                if(playerFiles.Length > 0)
                {
                    Data.PlayerData = new PlayerFile(playerFiles[0], isCompressed);
                }
                else
                {
                    return false;
                }

                string[] chunkDirs = System.IO.Directory.GetDirectories(filePath, "Chunks");

                if(chunkDirs.Length > 0)
                {
                    string chunkDir = chunkDirs[0];

                    string[] chunks = ChunkFile.GetFilesInDirectory(chunkDir, isCompressed, ChunkFile.CompressedExtension, ChunkFile.Extension);
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

                string[] entDirs = System.IO.Directory.GetDirectories(filePath, "Entitites");

                if(entDirs.Length > 0)
                {
                    string entDir = entDirs[0];

                    string[] ents = EntityFile.GetFilesInDirectory(entDir, isCompressed, EntityFile.CompressedExtension, EntityFile.Extension);
                    Data.EntityData = new List<EntityFile>();
                    foreach(string ent in ents)
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