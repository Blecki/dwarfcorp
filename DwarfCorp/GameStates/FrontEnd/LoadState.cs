using System.CodeDom.Compiler;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates
{
    public enum LoadTypes
    {
        GenerateOverworld,
        UseExistingOverworld
    }

    public class LoadState : GameState
    {
        public WorldManager World { get; set; }
        public InputManager Input = new InputManager();
        private DwarfRunner Runner;
        private bool DoneLoading = false;
        private bool DisplayException = false;
        private Gui.Root GuiRoot;
        private Gui.Widget Tip;
        private InfoTicker LoadTicker;
        private OverworldGenerator Generator;
        public Tutorial.TutorialManager TutorialManager;
        private LoadTypes LoadType;
        private Embarkment InitialEmbarkment;
        private ColonyCell InitialCell;

        private Timer TipTimer = new Timer(1, false, Timer.TimerMode.Real);
        public Overworld Settings { get; set; }

        public LoadState(DwarfGame game, Overworld settings, LoadTypes LoadType) :
            base(game)
        {
            this.LoadType = LoadType;
            Settings = settings;
            EnableScreensaver = true;
            InitialEmbarkment = settings.InstanceSettings.InitalEmbarkment;
            InitialCell = settings.InstanceSettings.Cell;

            Runner = new DwarfRunner(game);
        }

        public override void OnEnter()
        {
            TutorialManager = new Tutorial.TutorialManager();
            IsInitialized = true;
            DwarfTime.LastTimeX.Speed = 1.0f;

            IndicatorManager.SetupStandards();

            DwarfGame.GumInputMapper.GetInputQueue();
            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);

            Tip = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Font = "font10",
                TextColor = new Vector4(1, 1, 1, 1),
                MinimumSize = new Point(0, 64),
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Text = "Press any key to jump!",
                AutoLayout = Gui.AutoLayout.DockBottom,
                Background = new Gui.TileReference("basic", 0),
                BackgroundColor = new Vector4(0, 0, 0, 0.5f),
                InteriorMargin = new Gui.Margin(0, 0, 15, 15)
            });

            LoadTicker = GuiRoot.RootItem.AddChild(new Gui.Widgets.InfoTicker
            {
                Font = "font8",
                AutoLayout = Gui.AutoLayout.DockFill,
                TextColor = new Vector4(1,1,1,1)
            }) as Gui.Widgets.InfoTicker;

            GuiRoot.RootItem.Layout();

            if (LoadType == LoadTypes.GenerateOverworld)
            {
                Generator = new OverworldGenerator(Settings, true);
                Generator.Generate();
            }
            else
            {
                CreateWorld();
            }

            base.OnEnter();
        }

        private void CreateWorld()
        {
            if (LoadType == LoadTypes.GenerateOverworld) // Generating the world erases some settings.
            {
                Settings.InstanceSettings.Cell = InitialCell;
                Settings.InstanceSettings.InitalEmbarkment = InitialEmbarkment;
            }

            World = new WorldManager(Game)
            {
                // Todo: Just keep a reference to the settings OMG.
                WorldSizeInChunks = new Point3(Settings.InstanceSettings.Cell.Bounds.Width, Settings.zLevels, Settings.InstanceSettings.Cell.Bounds.Height),
                Overworld = Settings,
            };

            World.Renderer.PersistentSettings.MaxViewingLevel = World.WorldSizeInVoxels.Y;

            World.OnLoadedEvent += () => DoneLoading = true;
            World.OnSetLoadingMessage = (s) => LoadTicker.AddMessage(s);

            World.StartLoad();
        }

        public override void Update(DwarfTime gameTime)
        {
            if (DoneLoading)
            {
                // Todo: Decouple gui/input from world.
                // Copy important bits to PlayState - This is a hack; decouple world from gui and input instead.
                PlayState.Input = Input;
                GameStateManager.PopState(false);
                GameStateManager.PushState(new PlayState(Game, World));

                World.OnSetLoadingMessage = null;
            }
            else
            {
                if (LoadType == LoadTypes.GenerateOverworld)
                {
                    if (Generator.CurrentState == OverworldGenerator.GenerationState.Finished && World == null)
                    {
                        // World generation is finished!
                        LoadTicker.AddMessage("Checking spawn position...");
                        while (InitialCell.Bounds.Width == 8 && InitialCell.Bounds.Height == 8 && !IsGoodSpawn())
                        {
                            LoadTicker.AddMessage("Selecting new spawn...");
                            InitialCell = Settings.ColonyCells.EnumerateCells().Where(c => c.Bounds.Width == 8 && c.Bounds.Height == 8).SelectRandom();
                        }

                        CreateWorld();
                    }
                    else
                    {
                        if (!LoadTicker.HasMesssage(Generator.LoadingMessage))
                            LoadTicker.AddMessage(Generator.LoadingMessage);
                    }
                }

                foreach (var item in DwarfGame.GumInputMapper.GetInputQueue())
                {
                    GuiRoot.HandleInput(item.Message, item.Args);
                    if (item.Message == Gui.InputEvents.KeyPress)
                        Runner.Jump();
                }

                GuiRoot.Update(gameTime.ToRealTime());
                Runner.Update(gameTime);

                if (World != null && World.LoadStatus == WorldManager.LoadingStatus.Failure && !DisplayException)
                {
                    DisplayException = true;
                    string exceptionText = World.LoadingException == null
                        ? "Unknown exception."
                        : World.LoadingException.ToString();
                    GuiRoot.MouseVisible = true;
                    GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
                    DwarfTime.LastTimeX.IsPaused = false;
                    DwarfTime.LastTimeX.Speed = 1.0f;
                    World = null;
                    DwarfGame.LogSentryBreadcrumb("Loading", "Loading failed.", SharpRaven.Data.BreadcrumbLevel.Error);
                    GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm()
                    {
                        CancelText = "",
                        Text = "Oh no! Loading failed :( This crash has been automatically reported to the developers: " + exceptionText,
                        OnClick = (s, a) =>
                        {
                            DwarfGame.LogSentryBreadcrumb("Loading", "Loading failed. Player going back to start.");
                            GameStateManager.ClearState();
                        },
                        OnClose = (s) =>
                        {
                            DwarfGame.LogSentryBreadcrumb("Loading", "Loading failed. Player going back to start.");
                            GameStateManager.ClearState();
                        },
                        Rect = GuiRoot.RenderData.VirtualScreen
                    });
                }
            }

            base.Update(gameTime);
        }

        private bool IsGoodSpawn()
        {
            var oceanFound = 0;
            for (var x = InitialCell.Bounds.X; x < InitialCell.Bounds.X + InitialCell.Bounds.Width; ++x)
                for (var y = InitialCell.Bounds.Y; y < InitialCell.Bounds.Y + InitialCell.Bounds.Height; ++y)
                    if (Settings.Map.GetOverworldValueAt(x, y, OverworldField.Height) < Settings.GenerationSettings.SeaLevel)
                        oceanFound += 1;
            return oceanFound < (InitialCell.Bounds.Width * InitialCell.Bounds.Height) / 2;
        }

        public override void Render(DwarfTime gameTime)
        {        
            TipTimer.Update(gameTime);
            if (TipTimer.HasTriggered)
            {
                var entry = Datastructures.SelectRandom(TutorialManager.EnumerateTutorials());

                Tip.Text = entry.Value.Title + "\n" + entry.Value.Text;
                Tip.Invalidate();
                TipTimer.Reset(10.0f);
            }

            EnableScreensaver = true;
            base.Render(gameTime);

            Runner.Render(Game.GraphicsDevice, DwarfGame.SpriteBatch, gameTime);
            GuiRoot.Draw();
        }

    }
}
