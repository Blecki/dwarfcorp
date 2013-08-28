using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace DwarfCorp
{

    public class DwarfGame : Microsoft.Xna.Framework.Game
    {
        public GameStateManager StateManager { get; set; }
        public GraphicsDeviceManager graphics;
        public TextureManager TextureManager { get; set; }
        public static SpriteBatch SpriteBatch { get; set; }

        public DwarfGame()
        {
            
            Content.RootDirectory = "Content";
            StateManager = new GameStateManager(this);
            graphics = new GraphicsDeviceManager(this);
            Window.Title = "DwarfCorp";
            Window.AllowUserResizing = false;
            graphics.IsFullScreen = GameSettings.Default.Fullscreen;
            graphics.PreferredBackBufferWidth = GameSettings.Default.Resolution.Width;
            graphics.PreferredBackBufferHeight = GameSettings.Default.Resolution.Height;

            try
            {
                graphics.ApplyChanges();
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                
                Console.Error.WriteLine(exception.Message);
            }

        }

        protected override void Initialize()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            TextureManager = new TextureManager(Content, GraphicsDevice);

            PlayState playState = new PlayState(this, StateManager);
            StateManager.States["IntroState"] = new IntroState(this, StateManager);
            StateManager.States["PlayState"] = playState;
            StateManager.States["MainMenuState"] = new MainMenuState(this, StateManager);
            StateManager.States["WorldGeneratorState"] = new WorldGeneratorState(this, StateManager);
            StateManager.States["OptionsState"] = new OptionsState(this, StateManager);
            StateManager.States["OrderScreen"] = new OrderScreen(this, StateManager, playState);
            StateManager.States["CompanyMakerState"] = new CompanyMakerState(this, StateManager);

            if (GameSettings.Default.DisplayIntro)
            {
                StateManager.PushState("IntroState");
            }
            else
            {
                StateManager.PushState("MainMenuState");
            }

            StateManager.States["IntroState"].OnEnter();
            StateManager.States["MainMenuState"].OnEnter();
            StateManager.States["OptionsState"].OnEnter();
            StateManager.States["CompanyMakerState"].OnEnter();
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            StateManager.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            StateManager.Render(gameTime);
            base.Draw(gameTime);
        }
    }
}
