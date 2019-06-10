using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.GameStates
{
    public class GenerationParameters
    {
        public int NumCivilizations = 5;
        public int NumRains = 1000;
        public int NumVolcanoes = 3;
        public float RainfallScale = 2.0f;
        public int NumFaults = 3;
        public float SeaLevel = 0.17f;
        public float TemperatureScale = 1.0f;
    }

    public class OverworldGenerationSettings // Todo: Rename Overworld Settings?
    {
        public CompanyInformation Company;
        public ResourceSet PlayerCorporationResources;
        public DwarfBux PlayerCorporationFunds;

        public GenerationParameters GenerationSettings = new GenerationParameters();

        public int Width = 128;
        public int Height = 128;
        public string Name = GetRandomWorldName();
        public int Difficulty = 2;
        public bool GenerateFromScratch = false;
        public int Seed = 0;
        public int NumCaveLayers = 8;
        public int zLevels = 4; // This is actually y levels but genre convention is to call depth Z.
        public InstanceSettings InstanceSettings; // These are only saved because it makes the selector default to the last launched branch.

        [JsonIgnore] public OverworldMap Overworld = null;

        public List<ColonyCell> ColonyCells;


        public List<OverworldFaction> Natives;

        public static string GetRandomWorldName()
        {
            return TextGenerator.GenerateRandom(TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds));
        }

        public OverworldGenerationSettings()
        {
            Seed = Name.GetHashCode();
            Company = new CompanyInformation();
            Overworld = new OverworldMap(Width, Height);
            PlayerCorporationResources = new ResourceSet();

            InstanceSettings = new InstanceSettings();
            InstanceSettings.InitalEmbarkment = new Embarkment();
            ColonyCells = ColonyCell.DeriveFromTexture("World\\colonies");
            InstanceSettings.Cell = ColonyCells[1];
        }
    }
}
