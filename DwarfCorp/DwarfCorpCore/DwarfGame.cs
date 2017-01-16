// DwarfGame.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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

        public static Gem.GumInputMapper GumInput;
        public static Gum.RenderData GumSkin;
 
        public DwarfGame()
        {
            GameState.Game = this;
            Content.RootDirectory = "Content";
            StateManager = new GameStateManager(this);
            Graphics = new GraphicsDeviceManager(this);
            Window.Title = "DwarfCorp";
            Window.AllowUserResizing = false;
            TextureManager = new TextureManager(Content, GraphicsDevice);
            GameSettings.Load();
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
            // Prepare GemGui
            GumInput = new Gem.GumInputMapper(Window.Handle);
            GumSkin = new Gum.RenderData(GraphicsDevice,  Content,
                    "newgui/xna_draw", "Content/newgui/sheets.txt");

            if (SoundManager.Content == null)
            {
                SoundManager.Content = Content;
                SoundManager.LoadDefaultSounds();
#if XNA_BUILD
                SoundManager.SetActiveSongs(ContentPaths.Music.dwarfcorp, ContentPaths.Music.dwarfcorp_2,
                    ContentPaths.Music.dwarfcorp_3, ContentPaths.Music.dwarfcorp_4, ContentPaths.Music.dwarfcorp_5);
#endif
            }
            PlayState playState = new PlayState(this, StateManager);
            BiomeLibrary.InitializeStatics();
            StateManager.States["IntroState"] = new IntroState(this, StateManager);
            StateManager.States["PlayState"] = playState;
            StateManager.States["MainMenuState"] = new MainMenuState(this, StateManager);
            StateManager.States["WorldSetupState"] = new WorldSetupState(this, StateManager);
            StateManager.States["WorldGeneratorState"] = new WorldGeneratorState(this, StateManager);
            StateManager.States["OptionsState"] = new OptionsState(this, StateManager);
            StateManager.States["NewOptionsState"] = new NewOptionsState(this, StateManager);
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
            if (DwarfTime.LastTime == null)
            {
                DwarfTime.LastTime = new DwarfTime(time);
            }
            DwarfTime.LastTime.Update(time);
            StateManager.Update(DwarfTime.LastTime);
            base.Update(time);
        }

        protected override void Draw(GameTime time)
        {
            StateManager.Render(DwarfTime.LastTime);
            GraphicsDevice.SetRenderTarget(null);
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