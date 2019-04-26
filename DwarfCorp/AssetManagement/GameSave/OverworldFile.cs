using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DwarfCorp
{
    [Serializable]
    public class NewOverworldFile
    {
        public OverworldMetaData MetaData;
        public OverworldCell[,] OverworldMap;
        private GraphicsDevice Device {  get { return GameState.Game.GraphicsDevice; } }
        private int Width;
        private int Height;

        public NewOverworldFile()
        {
        }

        public NewOverworldFile(GraphicsDevice device, Overworld Overworld, string name, float seaLevel)
        {
            var worldFilePath = name + System.IO.Path.DirectorySeparatorChar + "world.png";
            var metaFilePath = name + System.IO.Path.DirectorySeparatorChar + "meta.txt";

            if (File.Exists(worldFilePath) && File.Exists(metaFilePath))
            {
                // Do nothing since overworlds should be saved precisely once.
                return;
            }

            OverworldMap = Overworld.Map;
            MetaData = new OverworldMetaData(device, Overworld, name, seaLevel);
            Width = Overworld.Map.GetLength(0);
            Height = Overworld.Map.GetLength(1);
        }
        
        public Texture2D CreateScreenshot(GraphicsDevice device, int width, int height, float seaLevel)
        {
            GameStates.GameState.Game.LogSentryBreadcrumb("Saving", String.Format("User saving an overworld with size {0} x {1}", width, height), SharpRaven.Data.BreadcrumbLevel.Info);
            Texture2D toReturn = null;
            toReturn = new Texture2D(device, width, height);
            global::System.Threading.Mutex imageMutex = new global::System.Threading.Mutex();
            Color[] worldData = new Color[width * height];
            Overworld.TextureFromHeightMap("Height", OverworldMap, null, OverworldField.Height, width, height, imageMutex, worldData, toReturn, seaLevel);

            return toReturn;
        }

        public Texture2D CreateSaveTexture(GraphicsDevice Device)
        {
            var r = new Texture2D(Device, OverworldMap.GetLength(0), OverworldMap.GetLength(1), false, SurfaceFormat.Color);
            var data = new Color[OverworldMap.GetLength(0) * OverworldMap.GetLength(1)];
            Overworld.GenerateSaveTexture(OverworldMap, data);
            r.SetData(data);
            return r;
        }

        public void LoadFromTexture(Texture2D Texture)
        {
            OverworldMap = new OverworldCell[Texture.Width, Texture.Height];
            var colorData = new Color[Texture.Width * Texture.Height];
            GameState.Game.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Texture.GetData(colorData);
            Overworld.DecodeSaveTexture(OverworldMap, Texture.Width, Texture.Height, colorData);

            // Remap the saved voxel ids to the ids of the currently loaded voxels.
            if (MetaData.BiomeTypeMap != null)
            {
                // First build a replacement mapping.

                var newBiomeMap = BiomeLibrary.GetBiomeTypeMap();
                var newReverseMap = new Dictionary<String, int>();
                foreach (var mapping in newBiomeMap)
                    newReverseMap.Add(mapping.Value, mapping.Key);

                var replacementMap = new Dictionary<int, int>();
                foreach (var mapping in MetaData.BiomeTypeMap)
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
                    for (var x = 0; x < OverworldMap.GetLength(0); ++x)
                        for (var y = 0; y < OverworldMap.GetLength(1); ++y)
                            if (replacementMap.ContainsKey(OverworldMap[x, y].Biome))
                                OverworldMap[x, y].Biome = (byte)replacementMap[OverworldMap[x, y].Biome];
                }
            }
        }

        public NewOverworldFile(string fileName)
        {
            ReadFile(fileName);
        }

        public static bool CheckCompatibility(string filePath)
        {
            try
            {
                var metaFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "meta.txt";
                var metadata = FileUtils.LoadJsonFromAbsolutePath<OverworldMetaData>(metaFilePath);

                return Program.CompatibleVersions.Contains(metadata.Version);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetOverworldName(string filePath)
        {
            try
            {
                var metaFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "meta.txt";
                return FileUtils.LoadJsonFromAbsolutePath<OverworldMetaData>(metaFilePath).Name;
            }
            catch (Exception)
            {
                return "?";
            }
        }

        public bool ReadFile(string filePath)
        {
            var worldFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "world.png";
            var metaFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "meta.txt";

            MetaData = FileUtils.LoadJsonFromAbsolutePath<OverworldMetaData>(metaFilePath);

            var worldTexture = AssetManager.LoadUnbuiltTextureFromAbsolutePath(worldFilePath);

            if (worldTexture != null)
            {
                LoadFromTexture(worldTexture);
            }
            else
            {
                Console.Out.WriteLine("Failed to load overworld texture.");
                return false;
            }
            return true;
        }
        
        public bool WriteFile(string filePath)
        {
            var worldFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "world.png";
            var metaFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "meta.txt";

            if (File.Exists(worldFilePath) && File.Exists(metaFilePath))
            {
                Console.Out.WriteLine("Overworld {0} already exists. Just assuming it is correct.", worldFilePath);
                return false;
            }

            // Write meta info
            MetaData.Version = Program.Version;
            FileUtils.SaveJSon(MetaData, metaFilePath, false);

            using (var texture = CreateSaveTexture(Device))
            using (var stream = new System.IO.FileStream(worldFilePath, System.IO.FileMode.Create))
                texture.SaveAsPng(stream, Width, Height);

            using (var texture = CreateScreenshot(Device, OverworldMap.GetLength(0), OverworldMap.GetLength(1), MetaData.SeaLevel))
            using (var stream = new System.IO.FileStream(filePath + Path.DirectorySeparatorChar + "screenshot.png", System.IO.FileMode.Create))
                texture.SaveAsPng(stream, Width, Height);

                return true;
        }

        public Overworld CreateOverworld()
        {
            var Overworld = new Overworld(OverworldMap.GetLength(0), OverworldMap.GetLength(1));
            Overworld.Map = OverworldMap;
            Overworld.Name = MetaData.Name;
            Overworld.NativeFactions = new List<Faction>();
            foreach (var faction in MetaData.FactionList)
                Overworld.NativeFactions.Add(new Faction(faction));
            return Overworld;
        }

        public OverworldGenerationSettings CreateSettings()
        {
            var settings = new OverworldGenerationSettings();
            settings.Overworld = CreateOverworld();
            settings.Width = settings.Overworld.Map.GetLength(1);
            settings.Height = settings.Overworld.Map.GetLength(0);
            settings.Name = MetaData.Name;
            settings.Natives = settings.Overworld.NativeFactions;
            return settings;
        }

        public static NewOverworldFile Load(String Path)
        {
            return new NewOverworldFile(Path);
        }
    }
}
