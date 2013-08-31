using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace DwarfCorp
{
    public class MainMenuState : GameState
    {
        public Texture2D Logo { get; set; }
        public SillyGUI GUI { get; set; }
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

            if (IsGameRunning)
            {
                ListSelect.AddItem("Continue Game");
            }

            ListSelect.AddItem("New Game");
            ListSelect.AddItem("Options");
            ListSelect.AddItem("Quit");
        }

        public void PlayItems()
        {
            ListSelect.ClearItems();
            ListSelect.AddItem("Flat World");
            ListSelect.AddItem("Generate World");
            ListSelect.AddItem("Create Company");
            ListSelect.AddItem("Cancel");
        }

        public void OnItemClicked(ListItem item)
        {
            if (item.Label == "Continue Game")
            {
                StateManager.PopState();
            }
            else if (item.Label == "New Game")
            {
                PlayItems();
            }
            else if (item.Label == "Quit")
            {
                Game.Exit();
            }
            else if (item.Label == "Create Company")
            {
                StateManager.PushState("CompanyMakerState");
            }
            else if (item.Label == "Generate World")
            {
                StateManager.PushState("WorldGeneratorState");
            }
            else if (item.Label == "Options")
            {
                StateManager.PushState("OptionsState");
            }
            else if (item.Label == "Cancel")
            {
                DefaultItems();
            }
            else if (item.Label == "Flat World")
            {
                Overworld.CreateUniformLand();
                StateManager.PushState("PlayState");
                PlayState play = (PlayState)StateManager.States["PlayState"];

                IsGameRunning = true;
            }
           
        }

        public override void OnEnter()
        {
            DefaultFont = Game.Content.Load<SpriteFont>("Default");
            GUI = new SillyGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>("Title"),  Game.Content.Load<SpriteFont>("Small"), Input);
            IsInitialized = true;
            Logo = Game.Content.Load<Texture2D>("banner3");
            
            ListSelect = new ListSelector(GUI, GUI.RootComponent);
            ListSelect.Label = "- Main Menu -";
            ListSelect.LocalBounds = new Rectangle(Game.GraphicsDevice.Viewport.Width / 2 - 100, Game.GraphicsDevice.Viewport.Height / 2 - 150, 150, 120);
            if (IsGameRunning)
            {
                ListSelect.AddItem("Continue Game");
            }
            ListSelect.AddItem("New Game");
            ListSelect.AddItem("Options");
            ListSelect.AddItem("Quit");

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
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));
            DwarfGame.SpriteBatch.Draw(Logo, new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - Logo.Width / 2 + dx, 30), null, Color.White);
            DwarfGame.SpriteBatch.DrawString(GUI.DefaultFont, Program.Version, new Vector2(15, 15), Color.White);

            DwarfGame.SpriteBatch.End();
        }

        public override void Render(GameTime gameTime)
        {
            if (Transitioning == TransitionMode.Running)
            {
                Game.GraphicsDevice.Clear(Color.Black);


                DrawGUI(gameTime, 0);
            }
            else if (Transitioning == TransitionMode.Entering)
            {
                float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }
            else if (Transitioning == TransitionMode.Exiting)
            {
                float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }

            base.Render(gameTime);
        }
    }
}
