using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This game state is just the set of menus at the start of the game. Allows navigation to other game states.
    /// </summary>
    public class MainMenuState : GameState
    {
        public Texture2D Logo { get; set; }
        public DwarfGUI GUI { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public ListSelector ListSelect { get; set; }
        public Drawer2D Drawer { get; set; }
        public InputManager Input { get; set; }
        public bool IsGameRunning { get; set; }


        public MainMenuState(DwarfGame game, GameStateManager stateManager) :
            base(game, "MainMenuState", stateManager)
        {
            ResourceLibrary library = new ResourceLibrary(game);
            IsGameRunning = false;
        }

        public void DefaultItems()
        {
            ListSelect.ClearItems();

            if(IsGameRunning)
            {
                ListSelect.AddItem("Continue Game", "Keep playing DwarfCorp");
            }

            ListSelect.AddItem("New Game", "Start a new game of DwarfCorp.");
            ListSelect.AddItem("Load Game", "Load DwarfCorp game from a file.");
            ListSelect.AddItem("Options", "Change game settings.");
            ListSelect.AddItem("Quit", "Exit the game.");
        }

        public void PlayItems()
        {
            ListSelect.ClearItems();
            ListSelect.AddItem("Load World", "Load a continent from an existing file!");
            ListSelect.AddItem("Generate World", "Create a new world from scratch!");
            ListSelect.AddItem("Flat World", "Create a flat (Debug) world.");
            ListSelect.AddItem("Create Company", "Customize your own Dwarf Corporation!");
            ListSelect.AddItem("Back", "Back to the Main Menu");
        }

        public void OnItemClicked(ListItem item)
        {
            if(item.Label == "Continue Game")
            {
                StateManager.PopState();
            }
            else if(item.Label == "New Game")
            {
                PlayItems();
            }
            else if(item.Label == "Quit")
            {
                Game.Exit();
            }
            else if(item.Label == "Create Company")
            {
                StateManager.PushState("CompanyMakerState");
            }
            else if(item.Label == "Generate World")
            {
                StateManager.PushState("WorldGeneratorState");
            }
            else if(item.Label == "Options")
            {
                StateManager.PushState("OptionsState");
            }
            else if(item.Label == "Back")
            {
                DefaultItems();
            }
            else if(item.Label == "Flat World")
            {
                Overworld.CreateUniformLand(Game.GraphicsDevice);
                StateManager.PushState("PlayState");
                PlayState play = (PlayState) StateManager.States["PlayState"];

                IsGameRunning = true;
            }
            else if(item.Label == "Load World")
            {
                StateManager.PushState("WorldLoaderState");
            }
            else if(item.Label == "Load Game")
            {
                StateManager.PushState("GameLoaderState");
            }
        }

        public override void OnEnter()
        {
            
            DefaultFont = Game.Content.Load<SpriteFont>(Program.CreatePath("Fonts", "Default"));
            GUI = new DwarfGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>(Program.CreatePath("Fonts", "Title")), Game.Content.Load<SpriteFont>(Program.CreatePath("Fonts","Small")), Input);
            IsInitialized = true;
            Logo = TextureManager.GetTexture(Program.CreatePath("Logos", "gamelogo"));

            ListSelect = new ListSelector(GUI, GUI.RootComponent)
            {
                Label = "- Main Menu -",
                LocalBounds = new Rectangle(Game.GraphicsDevice.Viewport.Width / 2 - 100, Game.GraphicsDevice.Viewport.Height / 2, 150, 150)
            };
            DefaultItems();

            ListSelect.OnItemClicked += ItemClicked;
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
            Input = new InputManager();

            base.OnEnter();
        }

        public void ItemClicked()
        {
            ListItem selectedItem = ListSelect.SelectedItem;
            OnItemClicked(selectedItem);
        }

        public override void Update(GameTime gameTime)
        {
            Input.Update();
            GUI.Update(gameTime);
            Game.IsMouseVisible = true;

            base.Update(gameTime);
        }


        private void DrawGUI(GameTime gameTime, float dx)
        {
            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizerState);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));
            DwarfGame.SpriteBatch.Draw(Logo, new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - Logo.Width / 2 + dx, 10), null, Color.White);
            DwarfGame.SpriteBatch.DrawString(GUI.DefaultFont, Program.Version, new Vector2(15, 15), Color.White);

            DwarfGame.SpriteBatch.End();
            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
            GUI.PostRender(gameTime);
        }

        public override void Render(GameTime gameTime)
        {

            if(Transitioning == TransitionMode.Running)
            {
                DrawGUI(gameTime, 0);
            }
            else if(Transitioning == TransitionMode.Entering)
            {
                float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }
            else if(Transitioning == TransitionMode.Exiting)
            {
                float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }

            base.Render(gameTime);
        }
    }

}