using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Newtonsoft.Json;

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
        public Vector2 Origin; // Todo: Does this belong in a cell-specific settings object?
        public Embarkment InitalEmbarkment = EmbarkmentLibrary.DefaultEmbarkment;
        public string ExistingFile = null;
        public bool GenerateFromScratch = false;
        public int Seed = 0;
        public int NumCaveLayers = 8;
        public int zLevels = 4; // This is actually y levels but genre convention is to call depth Z.
        public Rectangle SpawnRect;

        [JsonIgnore] public Overworld Overworld = null;
        [JsonIgnore] public List<Faction> Natives;

        public static string GetRandomWorldName()
        {
            List<List<string>> templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds);
            return TextGenerator.GenerateRandom(templates);
        }

        public OverworldGenerationSettings()
        {
            Seed = Name.GetHashCode();
            Overworld = new Overworld(Width, Height);
        }
    }
}
