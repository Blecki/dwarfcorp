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
                var file = NewOverworldFile.Load(path);
                StateManager.PopState();
                var genState = new WorldGeneratorState(Game, Game.StateManager, file.CreateSettings(), false);
                StateManager.PushState(genState);
            };
        }
        
    }

}