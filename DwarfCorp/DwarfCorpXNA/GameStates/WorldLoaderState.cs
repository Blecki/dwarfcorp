using System.Collections.Generic;
using System.Linq;
using System;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Gum;

namespace DwarfCorp.GameStates
{
    public class WorldLoaderState : PaginatedChooserState
    {
        public WorldLoaderState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, StateManager)
        {
            this.ProceedButtonText = "Load";
            this.NoItemsText = "No worlds found.";

            this.ItemSource = () =>
            {
                System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Worlds");
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
                var file = new OverworldFile(path + ProgramData.DirChar + "world." + OverworldFile.CompressedExtension, true, true);
                Overworld.Map = file.Data.CreateMap();
                Overworld.Name = file.Data.Name;
                Overworld.NativeFactions = new List<Faction>();
                var races = ContentPaths.LoadFromJson<Dictionary<string, Race>>(ContentPaths.World.races);
                foreach (var faction in file.Data.FactionList)
                {
                    Overworld.NativeFactions.Add(new Faction(faction, races));
                }
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