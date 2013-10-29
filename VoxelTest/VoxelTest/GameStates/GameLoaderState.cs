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
                foreach (System.IO.DirectoryInfo file in worldDirectory.EnumerateDirectories())
                {
                    Saves.Add(file.Name);
                }
            }
            catch (System.IO.IOException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }

        public void CreateGUI()
        {
            int EdgePadding = 32;
            Panel MainWindow = new Panel(GUI, GUI.RootComponent);
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            GridLayout layout = new GridLayout(GUI, MainWindow, 10, 4);

            Label title = new Label(GUI, layout, "Load Game", GUI.TitleFont);
            layout.SetComponentPosition(title, 0, 0, 1, 1);

            ScrollView scroller = new ScrollView(GUI, layout);
            layout.SetComponentPosition(scroller, 0, 1, 4, 8);

            layout.UpdateSizes();
            CreateSaves();

            ListSelector selector = new ListSelector(GUI, scroller);
            selector.DrawPanel = false;
            selector.Label = "-Saved Games-";
            selector.Mode = ListItem.SelectionMode.ButtonList;
            Saves.Add("Foo");
            Saves.Add("Bar");
            foreach (string s in Saves)
            {
                selector.AddItem(s);
            }

           

            selector.LocalBounds = new Rectangle(32, 32, scroller.GlobalBounds.Width - 64, Saves.Count * 80);


        

            Button back = new Button(GUI, layout, "Back", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            layout.SetComponentPosition(back, 3, 9, 1, 1);
            back.OnClicked += new ClickedDelegate(back_OnClicked);
        }

        void back_OnClicked()
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


        int iter = 0;
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
            RasterizerState rasterizerState = new RasterizerState() { ScissorTestEnable = true };
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizerState);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));
            DwarfGame.SpriteBatch.End();
            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
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
