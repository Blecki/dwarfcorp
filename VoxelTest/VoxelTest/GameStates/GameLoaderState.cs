using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    public class GameLoaderState : GameState
    {
        public SillyGUI GUI { get; set; }
        public InputManager Input { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public string SaveDirectory = "Saves";
        public List<string> Saves { get; set; }
        public bool ExitThreads { get; set; }

        public GameLoaderState(DwarfGame game, GameStateManager stateManager) :
            base(game, "GameLoaderState", stateManager)
        {
            IsInitialized = false;
            Saves = new List<string>();
            ExitThreads = false;
        }


        public void CreateSaves()
        {
            ExitThreads = false;
            try
            {
                System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + System.IO.Path.DirectorySeparatorChar + SaveDirectory);
                foreach(System.IO.DirectoryInfo file in worldDirectory.EnumerateDirectories())
                {
                    Saves.Add(file.Name);
                }
            }
            catch(System.IO.IOException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }

        public void CreateGUI()
        {
            const int edgePadding = 32;
            Panel mainWindow = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(edgePadding, edgePadding, Game.GraphicsDevice.Viewport.Width - edgePadding * 2, Game.GraphicsDevice.Viewport.Height - edgePadding * 2)
            };
            GridLayout layout = new GridLayout(GUI, mainWindow, 10, 4);

            Label title = new Label(GUI, layout, "Load Game", GUI.TitleFont);
            layout.SetComponentPosition(title, 0, 0, 1, 1);

            ScrollView scroller = new ScrollView(GUI, layout);
            layout.SetComponentPosition(scroller, 0, 1, 4, 8);

            layout.UpdateSizes();
            CreateSaves();

            ListSelector selector = new ListSelector(GUI, scroller)
            {
                LocalBounds = new Rectangle(32, 32, scroller.GlobalBounds.Width - 64, Saves.Count * 80),
                DrawPanel = false,
                Label = "-Saved Games-",
                Mode = ListItem.SelectionMode.ButtonList
            };
            foreach(string s in Saves)
            {
                selector.AddItem(s);
            }

            selector.OnItemClicked += () => selector_OnItemClicked(selector);


            Button back = new Button(GUI, layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.LeftArrow));
            layout.SetComponentPosition(back, 3, 9, 1, 1);
            back.OnClicked += back_OnClicked;
        }

        private void selector_OnItemClicked(ListSelector selector)
        {
            ListItem item = selector.SelectedItem;
            string save = item.Label;

            PlayState playState = StateManager.States["PlayState"] as PlayState;

            if(playState == null)
            {
                return;
            }

            playState.ShouldReset = true;
            playState.ExistingFile = DwarfGame.GetGameDirectory() + System.IO.Path.DirectorySeparatorChar + "Saves" + System.IO.Path.DirectorySeparatorChar + save;
            StateManager.PushState("PlayState");
        }


        private void back_OnClicked()
        {
            StateManager.PopState();
        }

        public override void OnEnter()
        {
            DefaultFont = Game.Content.Load<SpriteFont>("Default");
            GUI = new SillyGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>("Title"), Game.Content.Load<SpriteFont>("Small"), Input);
            Input = new InputManager();

            CreateGUI();

            IsInitialized = true;
            base.OnEnter();
        }


        private int iter = 0;

        public override void Update(GameTime gameTime)
        {
            iter++;
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
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizerState);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));
            DwarfGame.SpriteBatch.End();
            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
        }

        public override void Render(GameTime gameTime)
        {
            switch(Transitioning)
            {
                case TransitionMode.Running:
                    Game.GraphicsDevice.Clear(Color.Black);
                    DrawGUI(gameTime, 0);
                    break;
                case TransitionMode.Entering:
                {
                    float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                    DrawGUI(gameTime, dx);
                }
                    break;
                case TransitionMode.Exiting:
                {
                    float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                    DrawGUI(gameTime, dx);
                }
                    break;
            }

            base.Render(gameTime);
        }
    }

}