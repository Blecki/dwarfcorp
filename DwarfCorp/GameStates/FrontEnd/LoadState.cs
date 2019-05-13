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
        private WorldGenerator Generator;
        public Tutorial.TutorialManager TutorialManager;

        private Timer TipTimer = new Timer(1, false, Timer.TimerMode.Real);
        public OverworldGenerationSettings Settings { get; set; }

        public LoadState(DwarfGame game, GameStateManager stateManager, OverworldGenerationSettings settings) :
            base(game, "LoadState", stateManager)
        {
            Settings = settings;
            EnableScreensaver = true;

            Runner = new DwarfRunner(game);
        }

        public override void OnEnter()
        {
            TutorialManager = new Tutorial.TutorialManager();
            IsInitialized = true;
            DwarfTime.LastTime.Speed = 1.0f;

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

            if (Settings.GenerateFromScratch)
            {
                Generator = new WorldGenerator(Settings, true) { Seed = MathFunctions.Random.Next() };
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
            World = new WorldManager(Game)
            {
                // Todo: Just keep a reference to the settings OMG.
                WorldOrigin = Settings.WorldOrigin,
                WorldSizeInChunks = new Point3(Settings.ColonySize.X, Settings.zLevels, Settings.ColonySize.Z),
                InitialEmbark = Settings.InitalEmbarkment,
                ExistingFile = Settings.ExistingFile,
                SeaLevel = Settings.SeaLevel,
                Natives = Settings.Natives,
                StartUnderground = Settings.StartUnderground,
                GenerationSettings = Settings,
            };

            // Todo: Get rid of duplication.
            World.WorldGenerationOrigin = Settings.WorldGenerationOrigin;
            World.SpawnRect = Settings.SpawnRect;
            World.OnLoadedEvent += () => DoneLoading = true;

            World.Setup();
            World.OnSetLoadingMessage = (s) => LoadTicker.AddMessage(s);
        }

        public override void Update(DwarfTime gameTime)
        {
            if (DoneLoading)
            {
                // Todo: Decouple gui/input from world.
                // Copy important bits to PlayState - This is a hack; decouple world from gui and input instead.
                if (StateManager.CurrentState == this)
                {
                    PlayState.Input = Input;
                    StateManager.PopState(false);
                    StateManager.PushState(new PlayState(Game, StateManager, World));

                    World.OnSetLoadingMessage = null;
                    World.GenerationSettings.Overworld.NativeFactions = World.Natives;
                }
            }
            else
            {
                if (Settings.GenerateFromScratch && Generator.CurrentState == WorldGenerator.GenerationState.Finished && World == null)
                {
                    CreateWorld();
                }
                else if (Settings.GenerateFromScratch)
                {
                    if (!LoadTicker.HasMesssage(Generator.LoadingMessage))
                        LoadTicker.AddMessage(Generator.LoadingMessage);
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
                    DwarfTime.LastTime.IsPaused = false;
                    DwarfTime.LastTime.Speed = 1.0f;
                    World = null;
                    Game.LogSentryBreadcrumb("Loading", "Loading failed.", SharpRaven.Data.BreadcrumbLevel.Error);
                    GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm()
                    {
                        CancelText = "",
                        Text = "Oh no! Loading failed :( This crash has been automatically reported to the developers: " + exceptionText,
                        OnClick = (s, a) =>
                        {
                            StateManager.Game.LogSentryBreadcrumb("Loading", "Loading failed. Player going back to start.");
                            if (StateManager.CurrentState == this)
                            {
                                StateManager.PopState(false);
                                StateManager.ClearState();
                                StateManager.PushState(new MainMenuState(Game, StateManager));
                            }
                        },
                        OnClose = (s) =>
                        {
                            StateManager.Game.LogSentryBreadcrumb("Loading", "Loading failed. Player going back to start.");
                            if (StateManager.CurrentState == this)
                            {
                                StateManager.PopState(false);
                                StateManager.ClearState();
                                StateManager.PushState(new MainMenuState(Game, StateManager));
                            }
                        },
                        Rect = GuiRoot.RenderData.VirtualScreen
                    });
                }
            }

            base.Update(gameTime);
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
