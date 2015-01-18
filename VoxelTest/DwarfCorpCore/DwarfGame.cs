using System;
using DwarfCorp.GameStates;
using DwarfCorpCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


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
            Graphics.PreferredBackBufferWidth = GameSettings.Default.ResolutionX;
            Graphics.PreferredBackBufferHeight = GameSettings.Default.ResolutionY;

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
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + ProgramData.DirChar + "DwarfCorp";
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

        protected override void Update(GameTime time)
        {
            if (Act.LastTime == null)
            {
                Act.LastTime = new DwarfTime(time);
            }
            Act.LastTime.Update(time);
            StateManager.Update(Act.LastTime);
            base.Update(time);
        }

        protected override void Draw(GameTime time)
        {
            StateManager.Render(Act.LastTime);
            base.Draw(time);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            ExitGame = true;
            Program.SignalShutdown();
            base.OnExiting(sender, args);
        }

        public static bool ExitGame = false;
    }

}