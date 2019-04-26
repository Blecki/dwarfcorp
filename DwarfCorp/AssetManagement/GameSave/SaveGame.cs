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
            global::System.IO.Directory.CreateDirectory(directory);
            global::System.IO.Directory.CreateDirectory(directory + Path.DirectorySeparatorChar + "Chunks");

            foreach (ChunkFile chunk in ChunkData)
            {
                var filename = directory + Path.DirectorySeparatorChar + "Chunks" + Path.DirectorySeparatorChar + chunk.ID.X + "_" + chunk.ID.Y + "_" + chunk.ID.Z + ".";
                if (DwarfGame.COMPRESSED_BINARY_SAVES)
                    FileUtils.SaveBinary(chunk, filename + ChunkFile.CompressedExtension);
                else
                    FileUtils.SaveJSon(chunk, filename + ChunkFile.Extension, false);
            }

            FileUtils.SaveJSon(this.Metadata, directory + Path.DirectorySeparatorChar + "Metadata." + MetaData.Extension, false);
            FileUtils.SaveJSon(this.PlayData, directory + Path.DirectorySeparatorChar + "World." + PlayData.Extension, DwarfGame.COMPRESSED_BINARY_SAVES);
        }

        public static void DeleteOldestSave(string subdir, int maxToKeep, string blacklist)
        {
            var dir = global::System.IO.Directory.CreateDirectory(subdir);
            var parent = dir.Parent;
            var subdirs = parent.GetDirectories();
            var validDirs = subdirs.Where(d => !d.Name.Contains(blacklist)).ToList();
            if (validDirs.Count <= maxToKeep)
                return;
            DirectoryInfo oldestDir = null;
            TimeSpan oldestTime = new TimeSpan(0, 0, 0, 0, 0);
            foreach(var d in validDirs)
            {
                if ((DateTime.Now - d.LastWriteTime) > oldestTime)
                {
                    oldestTime = (DateTime.Now - d.LastWriteTime);
                    oldestDir = d;
                }
            }
            if (oldestDir != null)
                oldestDir.Delete(true);
        }

        private SaveGame() { }
        
        public bool ReadChunks(string filePath)
        {
            if (Metadata == null) throw new InvalidProgramException("MetaData must be loaded before chunk data.");

            ChunkData = new List<ChunkFile>();

            var chunkDirs = global::System.IO.Directory.GetDirectories(filePath, "Chunks");
            
            if (chunkDirs.Length > 0)
            {
                foreach (string chunk in Directory.GetFiles(chunkDirs[0], "*." + (DwarfGame.COMPRESSED_BINARY_SAVES ? ChunkFile.CompressedExtension : ChunkFile.Extension)))
                {
                    if (DwarfGame.COMPRESSED_BINARY_SAVES)
                        ChunkData.Add(FileUtils.LoadBinary<ChunkFile>(chunk));
                    else
                        ChunkData.Add(FileUtils.LoadJsonFromAbsolutePath<ChunkFile>(chunk));
                }
            }
            else
            {
                Console.Error.WriteLine("Can't load chunks {0}, no chunks found", filePath);
                return false;
            }

            // Remap the saved voxel ids to the ids of the currently loaded voxels.
            if (Metadata.VoxelTypeMap != null)
            {
                // First build a replacement mapping.

                var newVoxelMap = VoxelLibrary.GetVoxelTypeMap();
                var newReverseMap = new Dictionary<String, int>();
                foreach (var mapping in newVoxelMap)
                    newReverseMap.Add(mapping.Value, mapping.Key);

                var replacementMap = new Dictionary<int, int>();
                foreach (var mapping in Metadata.VoxelTypeMap)
                {
                    if (newReverseMap.ContainsKey(mapping.Value))
                    {
                        var newId = newReverseMap[mapping.Value];
                        if (mapping.Key != newId)
                            replacementMap.Add(mapping.Key, newId);
                    }
                }

                // If there are no changes, skip the expensive iteration.
                if (replacementMap.Count != 0)
                {
                    foreach (var chunk in ChunkData)
                        for (var i = 0; i < chunk.Types.Length; ++i)
                            if (replacementMap.ContainsKey(chunk.Types[i]))
                                chunk.Types[i] = (byte)replacementMap[chunk.Types[i]];
                }
            }

            return true;
        }

        public bool LoadPlayData(string filePath, WorldManager world)
        {
            string[] worldFiles = global::System.IO.Directory.GetFiles(filePath, "*." + PlayData.Extension);

            if (worldFiles.Length > 0)
            {
                if (DwarfGame.COMPRESSED_BINARY_SAVES)
                    PlayData = FileUtils.LoadCompressedJsonFromAbsolutePath<PlayData>(worldFiles[0], world);
                else
                    PlayData = FileUtils.LoadJsonFromAbsolutePath<PlayData>(worldFiles[0], world);
            }
            else
            {
                Console.Error.WriteLine("Can't load world from {0}, no data file found.", filePath);
                return false;
            }
            return true;
        }

        private bool ReadMetadata(string filePath)
        {
            if (!global::System.IO.Directory.Exists(filePath))
            {
                return false;
            }
            else
            {
                string[] metaFiles = global::System.IO.Directory.GetFiles(filePath, "*." + MetaData.Extension);

                if (metaFiles.Length > 0)
                    Metadata = FileUtils.LoadJsonFromAbsolutePath<MetaData>(metaFiles[0]);
                else
                {
                    Console.Error.WriteLine("Can't load file {0}, no metadata found", filePath);
                    return false;
                }

                string[] screenshots = global::System.IO.Directory.GetFiles(filePath, "*.png");

                if (screenshots.Length > 0)
                    Screenshot = AssetManager.LoadUnbuiltTextureFromAbsolutePath(screenshots[0]);

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
                    catch (Exception)
                    { }

                    if (valid) newest = dir;
                }
            }

            return newest == null ? null : newest.FullName;
        }

        public static SaveGame CreateFromWorld(WorldManager World)
        {
            return new SaveGame
            {
                Metadata = MetaData.CreateFromWorld(World),
                PlayData = PlayData.CreateFromWorld(World),
                ChunkData = World.ChunkManager.ChunkData.GetChunkEnumerator().Select(c => ChunkFile.CreateFromChunk(c)).ToList()
            };
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
