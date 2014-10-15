using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace DwarfCorp
{

    public class DwarfGame : Game
    {
        public GameStateManager StateManager { get; set; }
        public GraphicsDeviceManager Graphics;
        public TextureManager TextureManager { get; set; }
        public static SpriteBatch SpriteBatch { get; set; }

 
        public DwarfGame()
        {
            Content.RootDirectory = "Content";
            StateManager = new GameStateManager(this);
            Graphics = new GraphicsDeviceManager(this);
            Window.Title = "DwarfCorp";
            Window.AllowUserResizing = false;
            Graphics.IsFullScreen = GameSettings.Default.Fullscreen;
            Graphics.PreferredBackBufferWidth = GameSettings.Default.Resolution.Width;
            Graphics.PreferredBackBufferHeight = GameSettings.Default.Resolution.Height;

            try
            {
                Graphics.ApplyChanges();
            }
            catch(NoSuitableGraphicsDeviceException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }

        }

        public static string GetGameDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Program.DirChar + "DwarfCorp";
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
            StateManager.States["EconomyState"] = new EconomyState(this, StateManager, playState);
            StateManager.States["CompanyMakerState"] = new CompanyMakerState(this, StateManager);
            StateManager.States["WorldLoaderState"] = new WorldLoaderState(this, StateManager);
            StateManager.States["GameLoaderState"] = new GameLoaderState(this, StateManager);
            StateManager.States["LoseState"] = new LoseState(this, StateManager, playState);

            if(GameSettings.Default.DisplayIntro)
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


            //TestBehaviors.RunTests();

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            Act.LastTime = gameTime;
            StateManager.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            StateManager.Render(gameTime);
            base.Draw(gameTime);
        }

        public static bool ExitGame = false;
    }

}