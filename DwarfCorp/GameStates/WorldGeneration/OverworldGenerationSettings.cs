using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp.GameStates
{
    public class OverworldGenerationSettings
    {
        public int Width;
        public int Height;
        public string Name;
        public int NumCivilizations;
        public int NumRains;
        public int NumVolcanoes;
        public float RainfallScale;
        public int NumFaults;
        public float SeaLevel;
        public float TemperatureScale;
        public Point3 ColonySize;
        public Vector2 WorldGenerationOrigin;
        public float WorldScale;
        public Embarkment InitalEmbarkment;
        public Vector2 WorldOrigin;
        public string ExistingFile;
        public List<Faction> Natives;
        public bool GenerateFromScratch;
        public int Seed;
        public bool StartUnderground = false;
        public bool RevealSurface = true;
        public int NumCaveLayers = 8;
        public int zLevels = 4; // This is actually y levels but genre convention is to call depth Z.
        public Rectangle SpawnRect;

        public static string GetRandomWorldName()
        {
            List<List<string>> templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds);
            return TextGenerator.GenerateRandom(templates);
        }

        public OverworldGenerationSettings()
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
            InitalEmbarkment = EmbarkmentLibrary.DefaultEmbarkment;
            WorldOrigin = new Vector2(Width / WorldScale, Height / WorldScale) * 0.5f;
            ExistingFile = null;
            GenerateFromScratch = false;
            Seed = Name.GetHashCode();
        }
    }
}
