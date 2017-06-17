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
    public class NewGameLoaderState : PaginatedChooserState
    {
        public NewGameLoaderState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, StateManager)
        {
            this.ProceedButtonText = "Load";
            this.NoItemsText = "No saves found.";

            this.ItemSource = () =>
            {
                System.IO.DirectoryInfo savedirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Saves");
                return savedirectory.EnumerateDirectories().Select(d => d.FullName).ToList();
            };

            this.ScreenshotSource = (path) =>
            {
                var screenshots = SaveData.GetFilesInDirectory(path, false, "png", "png");
                if (screenshots.Length == 0)
                    return null;
                else
                    return TextureManager.LoadInstanceTexture(screenshots[0]);
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
        }
        
    }

}