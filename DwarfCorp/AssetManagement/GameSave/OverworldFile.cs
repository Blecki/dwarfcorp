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
        [Serializable]
        public class OverworldData
        {
            public string Version;
            public string Name;
            public float SeaLevel;
            [JsonIgnore] [NonSerialized] public OverworldCell[,] Data;
            
            [Serializable]
            public struct FactionDescriptor
            {
                public string Name { get; set; }
                public byte Id { get; set; }
                public string Race { get; set; }
                public Color PrimaryColory { get; set; }
                public Color SecondaryColor { get; set; }
                public int CenterX { get; set; }
                public int CenterY { get; set; }
                public float GoodWill { get; set; }
            }

            public List<FactionDescriptor> FactionList;

            public OverworldCell[,] CreateMap()
            {
                return Data;
            }

            public Texture2D CreateScreenshot(GraphicsDevice device, int width, int height, float seaLevel)
            {
                GameStates.GameState.Game.LogSentryBreadcrumb("Saving", String.Format("User saving an overworld with size {0} x {1}", width, height), SharpRaven.Data.BreadcrumbLevel.Info);
                Texture2D toReturn = null;
                var mapData = CreateMap();
                toReturn = new Texture2D(device, width, height);
                global::System.Threading.Mutex imageMutex = new global::System.Threading.Mutex();
                Color[] worldData = new Color[width * height];
                Overworld.TextureFromHeightMap("Height", mapData, ScalarFieldType.Height, width, height, imageMutex, worldData, toReturn, seaLevel);

                return toReturn;
            }

            public Texture2D CreateSaveTexture(GraphicsDevice Device, int Width, int Height)
            {
                var r = new Texture2D(Device, Width, Height, false, SurfaceFormat.Color);
                var data = new Color[Width * Height];
                Overworld.GenerateSaveTexture(Data, Width, Height, data);
                r.SetData(data);
                return r;
            }

            public void LoadFromTexture(Texture2D Texture)
            {
                Data = new OverworldCell[Texture.Width, Texture.Height];
                var colorData = new Color[Texture.Width * Texture.Height];
                GameState.Game.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Texture.GetData(colorData);
                Overworld.DecodeSaveTexture(Data, Texture.Width, Texture.Height, colorData);
            }

            public OverworldData()
            {
            }

            public OverworldData(GraphicsDevice device, OverworldCell[,] map, string name, float seaLevel)
            {
                int sizeX = map.GetLength(0);
                int sizeY = map.GetLength(1);
                
                Name = name;
                SeaLevel = seaLevel;
                Data = map;
                
                FactionList = new List<FactionDescriptor>();
                byte id = 0;
                foreach (Faction f in Overworld.NativeFactions)
                {
                    FactionList.Add(new FactionDescriptor()
                    {
                        Name = f.Name,
                        PrimaryColory = f.PrimaryColor,
                        SecondaryColor = f.SecondaryColor,
                        Id = id,
                        Race = f.Race.Name,
                        CenterX = f.Center.X,
                        CenterY = f.Center.Y, 
                        GoodWill = f.GoodWill
                    });
                    id++;
                }
            }
        }

        public OverworldData Data { get; set; }
        private GraphicsDevice Device {  get { return GameState.Game.GraphicsDevice; } }
        private int Width;
        private int Height;

        public NewOverworldFile()
        {
        }

        public NewOverworldFile(GraphicsDevice device, OverworldCell[,] map, string name, float seaLevel)
        {
            var worldFilePath = name + System.IO.Path.DirectorySeparatorChar + "world.png";
            var metaFilePath = name + System.IO.Path.DirectorySeparatorChar + "meta.txt";

            if (File.Exists(worldFilePath) && File.Exists(metaFilePath))
            {
                // Do nothing since overworlds should be saved precisely once.
                return;
            }

            Data = new OverworldData(device, map, name, seaLevel);
            Width = map.GetLength(0);
            Height = map.GetLength(1);
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
                var metadata = FileUtils.LoadJsonFromAbsolutePath<OverworldData>(metaFilePath);

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
                return FileUtils.LoadJsonFromAbsolutePath<OverworldData>(metaFilePath).Name;
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

            Data = FileUtils.LoadJsonFromAbsolutePath<OverworldData>(metaFilePath);

            var worldTexture = AssetManager.LoadUnbuiltTextureFromAbsolutePath(worldFilePath);

            if (worldTexture != null)
            {
                Data.LoadFromTexture(worldTexture);
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
            Data.Version = Program.Version;
            FileUtils.SaveJSon(Data, metaFilePath, false);

            using (var texture = Data.CreateSaveTexture(Device, Width, Height))
            using (var stream = new System.IO.FileStream(worldFilePath, System.IO.FileMode.Create))
                texture.SaveAsPng(stream, Width, Height);

            using (var texture = Data.CreateScreenshot(Device, Width, Height, Data.SeaLevel))
            using (var stream = new System.IO.FileStream(filePath + Path.DirectorySeparatorChar + "screenshot.png", System.IO.FileMode.Create))
                texture.SaveAsPng(stream, Width, Height);

                return true;
        }
    }
}
