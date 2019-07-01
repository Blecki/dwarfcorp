using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.GameStates
{
    public class Overworld
    {
        public CompanyInformation Company;
        public ResourceSet PlayerCorporationResources;
        public DwarfBux PlayerCorporationFunds;
        public CellSet ColonyCells;
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

        public static Overworld Create()
        {
            var r = new Overworld();
            r.Name = GetRandomWorldName();
            r.Seed = r.Name.GetHashCode();
            r.Company = new CompanyInformation();
            r.Map = new OverworldMap(r.Width, r.Height);
            r.PlayerCorporationResources = new ResourceSet();

            r.ColonyCells = new CellSet("World\\colonies");
            r.InstanceSettings = new InstanceSettings(r.ColonyCells.GetCellAt(16, 0));

            return r;
        }

        public Politics GetPolitics(OverworldFaction ThisFaction, OverworldFaction OtherFaction)
        {
            return ThisFaction.Politics[OtherFaction.Name];
        }
    }
}
