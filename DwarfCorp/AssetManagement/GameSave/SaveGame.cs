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

        [JsonIgnore] private String Path;


        public void WriteFile(string directory)
        {
            global::System.IO.Directory.CreateDirectory(directory);
            global::System.IO.Directory.CreateDirectory(directory + System.IO.Path.DirectorySeparatorChar + "Chunks");

            foreach (ChunkFile chunk in ChunkData)
            {
                var filename = directory + System.IO.Path.DirectorySeparatorChar + "Chunks" + System.IO.Path.DirectorySeparatorChar + chunk.ID.X + "_" + chunk.ID.Y + "_" + chunk.ID.Z + ".";
                FileUtils.SaveJSon(chunk, filename + ChunkFile.Extension);
            }

            FileUtils.SaveJSon(this.Metadata, directory + System.IO.Path.DirectorySeparatorChar + "Metadata." + MetaData.Extension);
            FileUtils.SaveJSon(this.PlayData, directory + System.IO.Path.DirectorySeparatorChar + "World." + PlayData.Extension);
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
        
        public bool ReadChunks()
        {
            if (Metadata == null) throw new InvalidProgramException("MetaData must be loaded before chunk data.");

            ChunkData = new List<ChunkFile>();

            var chunkDirs = System.IO.Directory.GetDirectories(Path, "Chunks");

            if (chunkDirs.Length > 0)
            {
                foreach (string chunk in Directory.GetFiles(chunkDirs[0], "*." + ChunkFile.Extension))
                    ChunkData.Add(FileUtils.LoadJsonFromAbsolutePath<ChunkFile>(chunk));
            }
            else
            {
                Console.Error.WriteLine("Can't load chunks {0}, no chunks found", Path);
                return false;
            }            

            return true;
        }

        // Todo: Goal here is to not have to load all chunks at once.
        public List<ChunkFile> LoadChunks()
        {
            if (Metadata == null)
                throw new InvalidProgramException("MetaData must be loaded before chunk data.");

            var chunkDirectory = System.IO.Path.Combine(Path, "Chunks");
            if (!Directory.Exists(chunkDirectory))
                throw new InvalidOperationException("No chunk directory found.");

            var r = new List<ChunkFile>();

            foreach (string chunkFileName in Directory.GetFiles(chunkDirectory, "*." + ChunkFile.Extension))
                r.Add(FileUtils.LoadJsonFromAbsolutePath<ChunkFile>(chunkFileName));

            return r;
        }
            
        public bool LoadPlayData(string filePath, WorldManager world)
        {
            // Todo: Use actual set path
            string[] worldFiles = Directory.GetFiles(filePath, "*." + PlayData.Extension);

            if (worldFiles.Length > 0)
                PlayData = FileUtils.LoadJsonFromAbsolutePath<PlayData>(worldFiles[0], world);
            else
            {
                Console.Error.WriteLine("Can't load world from {0}, no data file found.", filePath);
                return false;
            }
            return true;
        }

        private bool ReadMetadata()
        {
            if (!Directory.Exists(Path))
                return false;

            var metaFiles = Directory.GetFiles(Path, "*." + MetaData.Extension);

            if (metaFiles.Length > 0)
                Metadata = FileUtils.LoadJsonFromAbsolutePath<MetaData>(metaFiles[0]);
            else
            {
                Console.Error.WriteLine("Can't load file {0}, no metadata found", Path);
                return false;
            }

            var screenshots = Directory.GetFiles(Path, "*.png");

            if (screenshots.Length > 0)
                Screenshot = AssetManager.LoadUnbuiltTextureFromAbsolutePath(screenshots[0]);

            return true;
        }

        public static string GetLatestSaveFile()
        {
            var saveDirectory = Directory.CreateDirectory(DwarfGame.GetSaveDirectory());

            DirectoryInfo newest = null;
            foreach (var dir in saveDirectory.EnumerateDirectories())
            {
                if (newest == null || newest.CreationTime < dir.CreationTime)
                {
                    var valid = false;
                    try
                    {
                        var saveGame = SaveGame.LoadMetaFromDirectory(dir.FullName);
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
                ChunkData = World.ChunkManager.GetChunkEnumerator().Select(c => ChunkFile.CreateFromChunk(c)).ToList()
            };
        }

        public static SaveGame LoadMetaFromDirectory(String Directory)
        {
            var r = new SaveGame();
            r.Path = Directory;
            if (r.ReadMetadata())
                return r;
            return null;
        }
    }
}
