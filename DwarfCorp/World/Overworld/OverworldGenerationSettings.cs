using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.GameStates
{
    public class OverworldGenerationSettings // Todo: Rename Overworld Settings?
    {
        public CompanyInformation Company;
        public ResourceSet PlayerCorporationResources;
        public DwarfBux PlayerCorporationFunds;

        public int Width = 128;
        public int Height = 128;
        public string Name = GetRandomWorldName();
        public int NumCivilizations = 5;
        public int NumRains = 1000;
        public int NumVolcanoes = 3;
        public float RainfallScale = 2.0f;
        public int NumFaults = 3;
        public float SeaLevel = 0.17f;
        public float TemperatureScale = 1.0f;
        [JsonIgnore] public Embarkment InitalEmbarkment = null;
        public int Difficulty = 2;
        public bool GenerateFromScratch = false;
        public int Seed = 0;
        public int NumCaveLayers = 8;
        public int zLevels = 4; // This is actually y levels but genre convention is to call depth Z.
        public InstanceSettings InstanceSettings; // These are only saved because it makes the selector default to the last launched branch.

        [JsonIgnore] public Overworld Overworld = null;

        public List<OverworldFaction> Natives;

        public static string GetRandomWorldName()
        {
            return TextGenerator.GenerateRandom(TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds));
        }

        public OverworldGenerationSettings()
        {
            Seed = Name.GetHashCode();
            Company = new CompanyInformation();
            Overworld = new Overworld(Width, Height);
            InstanceSettings = new InstanceSettings();
            PlayerCorporationResources = new ResourceSet();
            InitalEmbarkment = new Embarkment();
        }
    }
}
