using System.CodeDom.Compiler;
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

        private Gum.Root GuiRoot;
        private Gum.Widget Tip;
        private NewGui.InfoTicker LoadTicker;
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

            if (Settings.GenerateFromScratch)
            {
                Generator = new WorldGenerator(Settings) {Seed = MathFunctions.Random.Next()};
                Generator.Generate();
            }
            else
            {
                CreateWorld();
            }

            DwarfGame.GumInputMapper.GetInputQueue();
            GuiRoot = new Gum.Root(DwarfGame.GumSkin);

            Tip = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                Font = "outline-font",
                TextColor = new Vector4(1, 1, 1, 1),
                MinimumSize = new Point(0, 128),
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                TextVerticalAlign = Gum.VerticalAlign.Center,
                Text = "Press any key to jump!",
                AutoLayout = Gum.AutoLayout.DockBottom
            });

            LoadTicker = GuiRoot.RootItem.AddChild(new NewGui.InfoTicker
            {
                Font = "outline-font",
                AutoLayout = Gum.AutoLayout.DockFill,
                TextColor = new Vector4(1,1,1,1)
            }) as NewGui.InfoTicker;

            GuiRoot.RootItem.Layout();

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
            // Todo - Save gui creation for play state. We're only creating it here so we can give it to
            //      the world class. The world doesn't need it until after loading.

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

                // Hack: So that saved games still load.
                if (World.PlayerCompany.Information == null)
                    World.PlayerCompany.Information = new CompanyInformation();

                StateManager.PopState();
                StateManager.PushState(new PlayState(Game, StateManager, World));

                World.OnSetLoadingMessage = null;
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
                    if (item.Message == Gum.InputEvents.KeyPress)
                        Runner.Jump();

                GuiRoot.Update(gameTime.ToGameTime());
                Runner.Update(gameTime);
            }

            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {        
            // Todo: This state should be rendering these, NOT the world manager.
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
