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
        private bool NeedsRefresh = true;
        private int SaveSelected = 0;
        private Gum.Widget BottomBar;

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

            GuiRoot.RootItem.Transparent = false;
            GuiRoot.RootItem.Background = new Gum.TileReference("basic", 0);
            GuiRoot.RootItem.InteriorMargin = new Gum.Margin(16, 16, 16, 16);

            // CONSTRUCT GUI HERE...
            BottomBar = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockBottom,
                MinimumSize = new Point(0, 60),
                TextHorizontalAlign = Gum.HorizontalAlign.Center
            });

            if (Saves.Count == 0)
                BottomBar.Text = "No save games found.";

            if (Saves.Count > 0)
            {
                BottomBar.AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.FloatBottomRight,
                    Border = "border-button",
                    Text = "Load",
                    OnClick = (sender, args) =>
                    {
                        var selectedSave = Saves[PreviewOffset + SaveSelected];
                        StateManager.ClearState();
                        StateManager.PushState(new LoadState(Game, Game.StateManager,
                            new WorldGenerationSettings
                            {
                                ExistingFile = selectedSave.Path,
                                Name = selectedSave.Path
                            }));
                    }
                });

                BottomBar.AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.FloatTopRight,
                    Border = "border-button",
                    Text = "Next",
                    OnClick = (sender, args) =>
                    {
                        if (PreviewOffset + Grid.ItemsThatFit < Saves.Count)
                            PreviewOffset += Grid.ItemsThatFit;
                    }
                });

                BottomBar.AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.FloatTopLeft,
                    Border = "border-button",
                    Text = "Prev",
                    OnClick = (sender, args) =>
                    {
                        if (PreviewOffset > 0)
                            PreviewOffset -= Grid.ItemsThatFit;
                    }
                });
            }

            BottomBar.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.FloatBottomLeft,
                Border = "border-button",
                Text = "Back",
                OnClick = (sender, args) =>
                {
                    StateManager.PopState();
                }
            });

            Grid = GuiRoot.RootItem.AddChild(new NewGui.GridPanel
            {
                ItemSize = new Point(128, 128),
                ItemSpacing = new Point(8, 8),
                AutoLayout = Gum.AutoLayout.DockFill,
                Border = "border-one",
                InteriorMargin = new Gum.Margin(4,4,4,4)
            }) as NewGui.GridPanel;

            GuiRoot.RootItem.Layout();

            if (Saves.Count > 0)
            {
                var gridSpaces = Grid.ItemsThatFit;
                for (var i = 0; i < gridSpaces; ++i)
                {
                    var lambda_index = i;
                    Grid.AddChild(new Gum.Widget
                    {
                        Border = "border-one",
                        OnClick = (sender, args) =>
                        {
                            SaveSelected = lambda_index;
                            NeedsRefresh = true;
                        }
                    });
                }

                GuiRoot.RootItem.Layout();
            }

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

            if (NeedsRefresh && Saves.Count > 0)
            {
                NeedsRefresh = false;

                var pageSize = System.Math.Min(Saves.Count - PreviewOffset, Grid.ItemsThatFit);

                if (SaveSelected >= pageSize)
                    SaveSelected = pageSize - 1;

                BottomBar.Text = Saves[PreviewOffset + SaveSelected].Path;

                for (var i = 0; i < pageSize; ++i)
                {
                    var square = Grid.GetChild(i);
                    square.BackgroundColor = new Vector4(1, 1, 1, 1);
                    if (i == SaveSelected)
                        square.BackgroundColor = new Vector4(1, 0, 0, 1);
                    square.Invalidate();
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
                    GuiRoot.DrawQuad(Grid.GetChild(i - PreviewOffset).Rect.Interior(7,7,7,7), game.Screenshot);
            }

            GuiRoot.MousePointer = mouse;
            GuiRoot.DrawMouse();
            base.Render(gameTime);
        }
    }

}