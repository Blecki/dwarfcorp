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
    public class LoadSaveGameState : PaginatedChooserState
    {
        public LoadSaveGameState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, StateManager)
        {
            this.ProceedButtonText = "Load";
            this.NoItemsText = "No saves found.";

            this.ItemSource = () =>
            {
                global::System.IO.DirectoryInfo savedirectory = global::System.IO.Directory.CreateDirectory(DwarfGame.GetSaveDirectory());
                var dirs = savedirectory.EnumerateDirectories().ToList();
                dirs.Sort((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                return dirs.ToList();
            };

            this.ScreenshotSource = (path) =>
            {
                var screenshots = global::System.IO.Directory.GetFiles(path, "*.png");
                if (screenshots.Length == 0)
                    return null;
                else
                    return AssetManager.LoadUnbuiltTextureFromAbsolutePath(screenshots[0]);
            };

            this.OnProceedClicked = (path) =>
            {
                StateManager.ClearState();
                StateManager.PushState(new LoadState(Game, Game.StateManager,
                    new WorldGenerationSettings
                    {
                        ExistingFile = path,
                        Name = path
                    }));
            };

            this.ValidateItem = (path) =>
            {
                try
                {
                    var saveGame = SaveGame.CreateFromDirectory(path);
                    if(!Program.CompatibleVersions.Contains(saveGame.Metadata.Version))
                    {
                        return String.Format("Incompatible version {0}", saveGame.Metadata.Version);
                    }
                    var overworld = saveGame.Metadata.OverworldFile;
                    if(!global::System.IO.Directory.Exists(DwarfGame.GetWorldDirectory() + Program.DirChar + overworld))
                    {
                        return string.Format("Overworld \"{0}\" does not exist.", overworld);
                    }
                    return "";
                }
                catch (Exception e)
                {
                    return String.Format("Error while loading {0}", e.Message);
                }
            };

            this.GetItemName = (path) =>
            {
                try
                {
                    var saveGame = SaveGame.CreateFromDirectory(path);
                    return saveGame.Metadata.OverworldFile;
                }
                catch (Exception)
                {
                    return "?";
                }
            };

            this.InvalidItemText = "This save was created with a different version of DwarfCorp and cannot be loaded.";
        }
        
    }

}