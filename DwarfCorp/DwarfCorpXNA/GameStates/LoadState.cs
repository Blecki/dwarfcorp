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
        private Gui.Widgets.InfoTicker LoadTicker;
        private WorldGenerator Generator;
        // Displays tips when the game is loading.
        public List<string> LoadingTips = new List<string>()
        {
            "Can't get the right angle? Hold SHIFT to move the camera around!",
            "Need to see tiny dwarves? Use the mousewheel to zoom!",
            "Press Q to quickly slice the terrain at the height of the cursor.",
            "Press E to quickly un-slice the terrain.",
            "The number keys can be used to quickly switch between tools.",
            "Employees will not work if they are unhappy.",
            "Monsters got you down? Try hiring some thugs!",
            "The most lucrative resources are beneath the earth.",
            "Dwarves can swim!",
            "Stockpiles are free!",
            "Payday occurs at midnight. Make sure to sell your goods before then!",
            "Dwarves prefer to eat in common rooms, but they will eat out of stockpiles if necessary.",
            "The minimap can be closed and opened.",
            "Monsters are shown on the minimap.",
            "Axedwarves are better at chopping trees than miners."
        };

        private Timer TipTimer = new Timer(5, false);
        public WorldGenerationSettings Settings { get; set; }
        public LoadState(DwarfGame game, GameStateManager stateManager, WorldGenerationSettings settings) :
            base(game, "LoadState", stateManager)
        {
            Settings = settings;
            EnableScreensaver = true;

            Runner = new DwarfRunner(game);
        }

        public override void OnEnter()
        {
            IsInitialized = true;
            DwarfTime.LastTime.Speed = 1.0f;

            IndicatorManager.SetupStandards();

            DwarfGame.GumInputMapper.GetInputQueue();
            GuiRoot = new Gui.Root(DwarfGame.GumSkin);

            Tip = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Font = "font18-outline",
                TextColor = new Vector4(1, 1, 1, 1),
                MinimumSize = new Point(0, 128),
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Text = "Press any key to jump!",
                AutoLayout = Gui.AutoLayout.DockBottom
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
                Generator = new WorldGenerator(Settings) { Seed = MathFunctions.Random.Next() };
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
                WorldOrigin = Settings.WorldOrigin,
                WorldScale = Settings.WorldScale,
                WorldSize = Settings.ColonySize,
                InitialEmbark = Settings.InitalEmbarkment,
                ExistingFile = Settings.ExistingFile,
                SeaLevel = Settings.SeaLevel,
                Natives = Settings.Natives
            };

            World.WorldScale = Settings.WorldScale;
            World.WorldGenerationOrigin = Settings.WorldGenerationOrigin;

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
                PlayState.Input = Input;
                StateManager.PopState();
                StateManager.PushState(new PlayState(Game, StateManager, World));

                World.OnSetLoadingMessage = null;
                Overworld.NativeFactions = World.Natives;
            }
            else
            {
                if (Settings.GenerateFromScratch && Generator.CurrentState == WorldGenerator.GenerationState.Finished && World == null)
                {
                    Settings = Generator.Settings;
                    CreateWorld();
                } else if (Settings.GenerateFromScratch)
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
                GuiRoot.Update(gameTime.ToGameTime());
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
                    
                    GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm()
                    {
                        CancelText = "",
                        Text = "Loading failed: " + exceptionText,
                        OnClick = (s, a) =>
                        {
                            StateManager.PopState();
                            StateManager.ClearState();
                            StateManager.PushState(new MainMenuState(Game, StateManager));
                        },
                        OnClose = (s) =>
                        {
                            StateManager.PopState();
                            StateManager.ClearState();
                            StateManager.PushState(new MainMenuState(Game, StateManager));
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
                Tip.Text = LoadingTips[MathFunctions.Random.Next(LoadingTips.Count)];
                Tip.Invalidate();
            }

            EnableScreensaver = true;
            if (World != null)
                World.Render(gameTime);
            base.Render(gameTime);

            Runner.Render(Game.GraphicsDevice, DwarfGame.SpriteBatch, gameTime);
            GuiRoot.Draw();
        }

    }
}
