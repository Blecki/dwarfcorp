using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class MetaData
    {
        public string OverworldFile { get; set; } // Todo: The overworld is known due to new system... KILLLLL! Requires work in loading system.
        public GameStates.InstanceSettings InstanceSettings;
        public float TimeOfDay { get; set; }
        public WorldTime Time { get; set; }
        public String Version;
        public String Commit;
        public WorldRendererPersistentSettings RendererSettings;

        public String DescriptionString = "Description";
        
        public static string Extension = "meta";
        public static string CompressedExtension = "zmeta";

        public static MetaData CreateFromWorld(WorldManager World)
        {
            return new MetaData
            {
                OverworldFile = World.Settings.Overworld.Name,
                InstanceSettings = World.Settings.InstanceSettings,
                TimeOfDay = World.Renderer.Sky.TimeOfDay,
                Time = World.Time,
                Version = Program.Version,
                Commit = Program.Commit,
                RendererSettings = World.Renderer.PersistentSettings,
                DescriptionString = String.Format("World size: {0}x{1}\nDwarves: {2}/{3}\nLiquid Assets: {4}\nMaterial Assets: {5}",
                    World.WorldSizeInVoxels.X, World.WorldSizeInVoxels.Z,
                    World.CalculateSupervisedEmployees(), World.CalculateSupervisionCap(),
                    World.PlayerFaction.Economy.Funds.ToString(),
                    World.ListResourcesInStockpilesPlusMinions().Values.Select(r =>
                    {
                        var value = ResourceLibrary.GetResourceByName(r.First.Type).MoneyValue.Value;
                        return (r.First.Count * value) + (r.Second.Count * value);
                    }).Sum())
            };
        }
    }
}