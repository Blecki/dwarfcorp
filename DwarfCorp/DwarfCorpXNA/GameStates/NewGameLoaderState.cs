using System.Collections.Generic;
using System.Linq;
using System;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    public class NewGameLoaderState : GameState
    {
        public class SaveGame
        {
            public String Path;
            public Texture2D Screenshot;
            
            public enum ScreenshotStatusEnum
            {
                Unloaded,
                NoneFound,
                Loaded
            }

            public ScreenshotStatusEnum ScreenshotStatus = ScreenshotStatusEnum.Unloaded;
        }

        private Gum.Root GuiRoot;
        private List<SaveGame> Saves = new List<SaveGame>();
        private NewGui.GridPanel Grid;
        private int PreviewOffset = 0;

        public NewGameLoaderState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, "GuiStateTemplate", StateManager)
        { }

        public override void OnEnter()
        {
            // Find all save games.
            System.IO.DirectoryInfo savedirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Saves");
            foreach (var directory in savedirectory.EnumerateDirectories())
                Saves.Add(new SaveGame { Path = directory.FullName });

            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gum.Root(Gum.Root.MinimumSize, DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

            // CONSTRUCT GUI HERE...
            var bottomBar = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockBottom,
                MinimumSize = new Point(0, 60)
            });

            Grid = GuiRoot.RootItem.AddChild(new NewGui.GridPanel
            {
                ItemSize = new Point(64, 64),
                ItemSpacing = new Point(8, 8),
                AutoLayout = Gum.AutoLayout.DockFill,
                Border = "border-fancy",
                InteriorMargin = new Gum.Margin(24,24,24,24)
            }) as NewGui.GridPanel;

            GuiRoot.RootItem.Layout();

            var gridSpaces = Grid.ItemsThatFit;
            for (var i = 0; i < gridSpaces.X * gridSpaces.Y; ++i)
                Grid.AddChild(new Gum.Widget
                {
                    Border = "border-thin"
                });

            GuiRoot.RootItem.Layout();

            // Must be true or Render will not be called.
            IsInitialized = true;

            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToGameTime());
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            var mouse = GuiRoot.MousePointer;
            GuiRoot.MousePointer = null;
            GuiRoot.Draw();

            for (var i = PreviewOffset; i < Saves.Count && i < (PreviewOffset + Grid.Children.Count); ++i)
            {
                var game = Saves[i];
                if (game.ScreenshotStatus == SaveGame.ScreenshotStatusEnum.Unloaded)
                {
                    var screenshots = SaveData.GetFilesInDirectory(game.Path, false, "png", "png");
                    if (screenshots.Length == 0)
                        game.ScreenshotStatus = SaveGame.ScreenshotStatusEnum.NoneFound;
                    else
                    {
                        game.Screenshot = TextureManager.LoadInstanceTexture(screenshots[0]);
                        game.ScreenshotStatus = SaveGame.ScreenshotStatusEnum.Loaded;
                    }
                }

                if (game.ScreenshotStatus == SaveGame.ScreenshotStatusEnum.Loaded)
                    GuiRoot.DrawQuad(Grid.GetChild(i - PreviewOffset).GetDrawableInterior(), game.Screenshot);
            }

            GuiRoot.MousePointer = mouse;
            GuiRoot.DrawMouse();
            base.Render(gameTime);
        }
    }

}