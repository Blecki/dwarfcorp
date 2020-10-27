using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates // Todo: Why in GameStates?
{
    public class Overworld
    {
        public CompanyInformation Company;
        public ResourceSet PlayerCorporationResources;
        public DwarfBux PlayerCorporationFunds;
        public List<OverworldFaction> Natives;

        public Point Size = new Point(16, 16);
        public Point3 WorldSizeInChunks { get { return new Point3(Size.X, zLevels, Size.Y); } }
        public int Width { get { return Size.X * VoxelConstants.OverworldScale; } }
        public int Height { get { return Size.Y * VoxelConstants.OverworldScale; } }
        public string Name = "";
        public Difficulty Difficulty = null;
        public int Seed = 0;
        public int NumCaveLayers = 8;
        public int zLevels = 4; // This is actually y levels but genre convention is to call depth Z.
        public bool DebugWorld = false;

        public InstanceSettings InstanceSettings; // These are only saved because it makes the selector default to the last launched branch.

        public String GetInstancePath()
        {
            return DwarfGame.GetWorldDirectory() + System.IO.Path.DirectorySeparatorChar + Name;
        }

        public Dictionary<String, Politics> Politics = new Dictionary<string, Politics>();
        public Dictionary<TaskCategory, TaskPriority> DefaultTaskPriorities = new Dictionary<TaskCategory, TaskPriority>();

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
            r.Difficulty = Library.GetDifficulty("Normal");
            r.PlayerCorporationFunds = r.Difficulty.StartingFunds;

            r.InstanceSettings = new InstanceSettings(r);

            return r;
        }

        public Politics GetPolitics(OverworldFaction ThisFaction, OverworldFaction OtherFaction)
        {
            var key = "";
            if (String.Compare(ThisFaction.Name, OtherFaction.Name, false) < 0)
                key = ThisFaction.Name + " & " + OtherFaction.Name;
            else
                key = OtherFaction.Name + " & " + ThisFaction.Name;

            if (!Politics.ContainsKey(key))
                Politics.Add(key, DwarfCorp.Politics.CreatePolitivs(ThisFaction, OtherFaction));

            return Politics[key];
        }

        public TaskPriority GetDefaultTaskPriority(TaskCategory Category)
        {
            if (DefaultTaskPriorities.ContainsKey(Category))
                return DefaultTaskPriorities[Category];
            else
                return TaskPriority.NotSet;
        }
    }
}
