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
                return NewOverworldFile.CheckCompatibility(item);
            };

            this.ItemSource = () =>
            {
                System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetWorldDirectory());
                return worldDirectory.EnumerateDirectories().Select(d => d.FullName).ToList();
            };

            this.ScreenshotSource = (path) =>
            {
                try
                {
                    return TextureManager.LoadInstanceTexture(path + ProgramData.DirChar + "screenshot.png");
                }
                catch (Exception)
                {
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
                settings.Name = System.IO.Path.GetFileName(path);
                StateManager.PopState();
                settings.Natives = Overworld.NativeFactions;
                var genState = new WorldGeneratorState(Game, Game.StateManager, settings, false);
                StateManager.PushState(genState);
            };
        }
        
    }

}