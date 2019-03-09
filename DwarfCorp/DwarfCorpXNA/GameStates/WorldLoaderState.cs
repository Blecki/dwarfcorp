using System.Collections.Generic;
using System.Linq;
using System;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.Gui;

namespace DwarfCorp.GameStates
{
    public class WorldLoaderState : PaginatedChooserState
    {
        public WorldLoaderState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, StateManager)
        {
            this.ProceedButtonText = "Load";
            this.NoItemsText = "No worlds found.";

            this.InvalidItemText = "This world was saved by an earlier version of DwarfCorp and is not compatible.";
            this.ValidateItem = (item) =>
            {
                return NewOverworldFile.CheckCompatibility(item) ? "" : "Incompatible save file.";
            };
            this.GetItemName = (item) =>
            {
                return NewOverworldFile.GetOverworldName(item);
            };

            this.ItemSource = () =>
            {
                global::System.IO.DirectoryInfo worldDirectory = global::System.IO.Directory.CreateDirectory(DwarfGame.GetWorldDirectory());
                var dirs = worldDirectory.EnumerateDirectories().ToList();
                dirs.Sort((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                return dirs;
            };

            this.ScreenshotSource = (path) =>
            {
                try
                {
                    return AssetManager.LoadUnbuiltTextureFromAbsolutePath(path + global::System.IO.Path.DirectorySeparatorChar + "screenshot.png");
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception.ToString());
                    return null;
                }
            };

            this.OnProceedClicked = (path) =>
            {
                var file = new NewOverworldFile(path);
                Overworld.Map = file.Data.CreateMap();
                Overworld.Name = file.Data.Name;
                Overworld.NativeFactions = new List<Faction>();
                foreach (var faction in file.Data.FactionList)
                    Overworld.NativeFactions.Add(new Faction(faction));
                var settings = new WorldGenerationSettings();
                settings.Width = Overworld.Map.GetLength(1);
                settings.Height = Overworld.Map.GetLength(0);
                settings.Name = global::System.IO.Path.GetFileName(path);
                StateManager.PopState();
                settings.Natives = Overworld.NativeFactions;
                var genState = new WorldGeneratorState(Game, Game.StateManager, settings, false);
                StateManager.PushState(genState);
            };
        }
        
    }

}