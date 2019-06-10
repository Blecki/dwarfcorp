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
    public class NewOverworldFile
    {
        public OverworldMetaData MetaData;
        public OverworldGenerationSettings Settings;

        public NewOverworldFile()
        {
        }

        public NewOverworldFile(GraphicsDevice device, OverworldGenerationSettings Settings)
        {
            this.Settings = Settings;

            var worldFilePath = Settings.Name + System.IO.Path.DirectorySeparatorChar + "world.png";
            var metaFilePath = Settings.Name + System.IO.Path.DirectorySeparatorChar + "meta.txt";

            MetaData = new OverworldMetaData(device, Settings);
        }
        
        private OverworldCell[,] LoadFromTexture(Texture2D Texture)
        {
            var map = new OverworldCell[Texture.Width, Texture.Height];
            var colorData = new Color[Texture.Width * Texture.Height];
            GameState.Game.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Texture.GetData(colorData);
            DwarfCorp.OverworldMap.DecodeSaveTexture(map, Texture.Width, Texture.Height, colorData);

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
                    for (var x = 0; x < map.GetLength(0); ++x)
                        for (var y = 0; y < map.GetLength(1); ++y)
                            if (replacementMap.ContainsKey(map[x, y].Biome))
                                map[x, y].Biome = (byte)replacementMap[map[x, y].Biome];
                }
            }

            return map;
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
                var metaFilePath = filePath + Path.DirectorySeparatorChar + "meta.txt";
                return FileUtils.LoadJsonFromAbsolutePath<OverworldMetaData>(metaFilePath).Settings.Name;
            }
            catch (Exception)
            {
                return "?";
            }
        }

        public bool ReadFile(string filePath)
        {
            var metaFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "meta.txt";
            MetaData = FileUtils.LoadJsonFromAbsolutePath<OverworldMetaData>(metaFilePath);

            foreach (var resource in MetaData.Resources)
                if (!ResourceLibrary.Exists(resource.Name))
                    ResourceLibrary.Add(resource);

            var worldFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "world.png";
            var worldTexture = AssetManager.LoadUnbuiltTextureFromAbsolutePath(worldFilePath);

            if (worldTexture != null)
            {
                var worldData = LoadFromTexture(worldTexture);
                MetaData.Settings.Overworld = new OverworldMap() { Map = worldData };
            }
            else
            {
                Console.Out.WriteLine("Failed to load overworld texture.");
                return false;
            }

            return true;
        }

        private Texture2D CreateScreenshot()
        {
            DwarfGame.LogSentryBreadcrumb("Saving", String.Format("User saving an overworld with size {0} x {1}", MetaData.Settings.Width, MetaData.Settings.Height), SharpRaven.Data.BreadcrumbLevel.Info);
            Texture2D toReturn = new Texture2D(GameState.Game.GraphicsDevice, MetaData.Settings.Width, MetaData.Settings.Height);
            var colorData = new Color[MetaData.Settings.Width * MetaData.Settings.Height];
            MetaData.Settings.Overworld.CreateTexture("Height", null, 1, colorData, MetaData.Settings.SeaLevel);
            MetaData.Settings.Overworld.ShadeHeight(1, colorData);
            toReturn.SetData(colorData);
            return toReturn;
        }

        private Texture2D CreateSaveTexture()
        {
            var r = new Texture2D(GameState.Game.GraphicsDevice, MetaData.Settings.Width, MetaData.Settings.Height, false, SurfaceFormat.Color);
            var data = new Color[MetaData.Settings.Width * MetaData.Settings.Height];
            MetaData.Settings.Overworld.GenerateSaveTexture(data);
            r.SetData(data);
            return r;
        }

        public bool WriteFile(string filePath)
        {
            var worldFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "world.png";
            var metaFilePath = filePath + global::System.IO.Path.DirectorySeparatorChar + "meta.txt";

            // Write meta info
            MetaData.Version = Program.Version;
            FileUtils.SaveJSon(MetaData, metaFilePath);

            using (var texture = CreateSaveTexture())
            using (var stream = new System.IO.FileStream(worldFilePath, System.IO.FileMode.Create))
                texture.SaveAsPng(stream, Settings.Width, Settings.Height);

            using (var texture = CreateScreenshot())
            using (var stream = new System.IO.FileStream(filePath + Path.DirectorySeparatorChar + "screenshot.png", System.IO.FileMode.Create))
                texture.SaveAsPng(stream, Settings.Width, Settings.Height);

                return true;
        }

        public OverworldGenerationSettings CreateSettings()
        {
            return MetaData.Settings;
        }

        public static NewOverworldFile Load(String Path)
        {
            return new NewOverworldFile(Path);
        }
    }
}
