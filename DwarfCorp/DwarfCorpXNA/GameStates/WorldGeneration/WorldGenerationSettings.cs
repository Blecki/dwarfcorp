using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Security.Policy;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Converters;

namespace DwarfCorp.GameStates
{
    public class WorldGenerationSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; }
        public int NumCivilizations { get; set; }
        public int NumRains { get; set; }
        public int NumVolcanoes { get; set; }
        public float RainfallScale { get; set; }
        public int NumFaults { get; set; }
        public float SeaLevel { get; set; }
        public float TemperatureScale { get; set; }
        public Point3 ColonySize { get; set; }
        public Vector2 WorldGenerationOrigin { get; set; }
        public float WorldScale { get; set; }
        public Embarkment InitalEmbarkment { get; set; }
        public Vector2 WorldOrigin { get; set; }
        public string ExistingFile { get; set; }
        public List<Faction> Natives { get; set; }
        public bool GenerateFromScratch { get; set; }
        public int Seed { get; set; }

        public static string GetRandomWorldName()
        {
            List<List<string>> templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds);
            return TextGenerator.GenerateRandom(templates);
        }

        public WorldGenerationSettings()
        {
            Width = 512;
            Height = 512;
            Name = GetRandomWorldName();
            NumCivilizations = 5;
            NumFaults = 3;
            NumRains = 1000;
            NumVolcanoes = 3;
            RainfallScale = 1.0f;
            SeaLevel = 0.17f;
            TemperatureScale = 1.0f;
            ColonySize = new Point3(8, 1, 8);
            WorldScale = 4.0f;
            InitalEmbarkment = Embarkment.DefaultEmbarkment;
            WorldOrigin = new Vector2(Width / WorldScale, Height / WorldScale) * 0.5f;
            ExistingFile = null;
            GenerateFromScratch = false;
            Seed = Name.GetHashCode();
        }
    }
}
