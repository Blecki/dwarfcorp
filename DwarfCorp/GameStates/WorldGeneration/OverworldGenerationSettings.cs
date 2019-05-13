using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp.GameStates
{
    public class OverworldGenerationSettings
    {
        public CompanyInformation Company;
        public int Width = 128;
        public int Height = 128;
        public string Name = GetRandomWorldName();
        public int NumCivilizations = 5;
        public int NumRains = 1000;
        public int NumVolcanoes = 3;
        public float RainfallScale = 1.0f;
        public int NumFaults = 3;
        public float SeaLevel = 0.17f;
        public float TemperatureScale = 1.0f;
        public Point3 ColonySize = new Point3(0, 1, 0);
        public Vector2 WorldGenerationOrigin;
        public Embarkment InitalEmbarkment = EmbarkmentLibrary.DefaultEmbarkment;
        public Vector2 WorldOrigin = Vector2.Zero;
        public string ExistingFile = null;
        public List<Faction> Natives;
        public bool GenerateFromScratch = false;
        public int Seed = 0;
        public bool StartUnderground = false; // Todo: Discard
        public bool RevealSurface = true; // Todo: Discard
        public int NumCaveLayers = 8;
        public int zLevels = 4; // This is actually y levels but genre convention is to call depth Z.
        public Rectangle SpawnRect;
        public Overworld Overworld = null;

        public static string GetRandomWorldName()
        {
            List<List<string>> templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds);
            return TextGenerator.GenerateRandom(templates);
        }

        public OverworldGenerationSettings()
        {
            WorldOrigin = new Vector2(Width, Height) * 0.5f;
            Seed = Name.GetHashCode();
            Overworld = new Overworld(Width, Height);
        }
    }
}
