using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.GameStates
{
    public class Overworld // Todo: Rename Overworld Settings?
    {
        public CompanyInformation Company;
        public ResourceSet PlayerCorporationResources;
        public DwarfBux PlayerCorporationFunds;
        public List<ColonyCell> ColonyCells;
        public List<OverworldFaction> Natives;

        public int Width = 128;
        public int Height = 128;
        public string Name = "";
        public int Difficulty = 2;
        public int Seed = 0;
        public int NumCaveLayers = 8;
        public int zLevels = 4; // This is actually y levels but genre convention is to call depth Z.

        public InstanceSettings InstanceSettings; // These are only saved because it makes the selector default to the last launched branch.

        [JsonIgnore] public OverworldMap Map = null;
        [JsonIgnore] public OverworldGenerationSettings GenerationSettings = new OverworldGenerationSettings();

        public static string GetRandomWorldName()
        {
            return TextGenerator.GenerateRandom(TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds));
        }

        public Overworld()
        {
            Name = GetRandomWorldName();
            Seed = Name.GetHashCode();
            Company = new CompanyInformation();
            Map = new OverworldMap(Width, Height);
            PlayerCorporationResources = new ResourceSet();
            ColonyCells = ColonyCell.DeriveFromTexture("World\\colonies");

            InstanceSettings = new InstanceSettings(ColonyCells[1]);
        }
    }
}
