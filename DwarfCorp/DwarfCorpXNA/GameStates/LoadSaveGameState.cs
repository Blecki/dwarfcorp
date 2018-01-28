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
                System.IO.DirectoryInfo savedirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetSaveDirectory());
                var dirs = savedirectory.EnumerateDirectories().ToList();
                dirs.Sort((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                return dirs.Select(d => d.FullName).ToList();
            };

            this.ScreenshotSource = (path) =>
            {
                var screenshots = System.IO.Directory.GetFiles(path, "*.png");
                if (screenshots.Length == 0)
                    return null;
                else
                    return TextureManager.LoadInstanceTexture(screenshots[0], false);
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
                    return Program.CompatibleVersions.Contains(saveGame.Metadata.Version);
                }
                catch (Exception)
                {
                    return false;
                }
            };

            this.InvalidItemText = "This save was created with a different version of DwarfCorp and cannot be loaded.";
        }
        
    }

}