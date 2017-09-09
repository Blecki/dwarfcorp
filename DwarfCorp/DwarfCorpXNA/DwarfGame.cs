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
using System.Diagnostics;
using System.IO;
using System.Threading;
using ContentGenerator;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Newtonsoft.Json;
#if !XNA_BUILD
using SDL2;
#endif
using SharpRaven;
using SharpRaven.Data;

namespace DwarfCorp
{

    public class DwarfGame : Game
    {
#if XNA_BUILD
        public static bool COMPRESSED_BINARY_SAVES = true;
#else
        public static bool COMPRESSED_BINARY_SAVES = false;
#endif

        public GameStateManager StateManager { get; set; }
        public GraphicsDeviceManager Graphics;
        public TextureManager TextureManager { get; set; }
        public static SpriteBatch SpriteBatch { get; set; }

        public static Gui.Input.GumInputMapper GumInputMapper;
        public static Gui.Input.Input GumInput;
        public static Gui.RenderData GumSkin;

        public const string GameName = "DwarfCorp";
        private static StreamWriter _logwriter;
#if SHARP_RAVEN
        private RavenClient ravenClient;
#endif
        public DwarfGame()
        {
            try
            {
#if SHARP_RAVEN
                if (GameSettings.Default.AllowReporting)
                {
                    ravenClient =
                        new RavenClient(
                            "https://af78a676a448474dacee4c72a9197dd2:0dd0a01a9d4e4fa4abc6e89ac7538346@sentry.io/192119");
                    ravenClient.Tags["Version"] = Program.Version;
                }
#if XNA_BUILD
                ravenClient.Tags["Platform"] = "XNA";
#else
                ravenClient.Tags["Platform"] = "FNA";
#endif
#endif
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
            }

            //BoundingBox foo = new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
            //string serialized = FileUtils.SerializeBasicJSON(foo);
            //BoundingBox deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<BoundingBox>(serialized, new BoxConverter());
            //string code = ContentPathGenerator.GenerateCode();
            //Console.Out.Write(code);
            GameState.Game = this;
            Content.RootDirectory = "Content";
            StateManager = new GameStateManager(this);
            Graphics = new GraphicsDeviceManager(this);
            Window.Title = "DwarfCorp";
            Window.AllowUserResizing = false;
            TextureManager = new TextureManager(Content, GraphicsDevice);
            GameSettings.Load();
            Graphics.IsFullScreen = GameSettings.Default.Fullscreen;
            Graphics.PreferredBackBufferWidth = GameSettings.Default.Fullscreen ? GameSettings.Default.ResolutionX : Math.Min(GameSettings.Default.ResolutionX, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width);
            Graphics.PreferredBackBufferHeight = GameSettings.Default.Fullscreen ? GameSettings.Default.ResolutionY : Math.Min(GameSettings.Default.ResolutionY,
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);
            Graphics.SynchronizeWithVerticalRetrace = GameSettings.Default.VSync;
            MathFunctions.Random = new ThreadSafeRandom(new Random().Next());
            try
            {
                Graphics.ApplyChanges();
            }
            catch(NoSuitableGraphicsDeviceException exception)
            {
                Console.Error.WriteLine(exception.Message);
#if SHARP_RAVEN
                if (ravenClient != null)
                    ravenClient.Capture(new SentryEvent(exception));
#endif
            }
        }

#if !XNA_BUILD
        public static string GetGameDirectory()
        {
            string platform = SDL.SDL_GetPlatform();
            if (platform.Equals("Windows"))
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments
                    ),
                    "SavedGames",
                    GameName
                );
            }
            else if (platform.Equals("Mac OS X"))
            {
                string osConfigDir = Environment.GetEnvironmentVariable("HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    return "."; // Oh well.
                }
                osConfigDir += "/Library/Application Support";
                return Path.Combine(osConfigDir, GameName);
            }
            else if (platform.Equals("Linux"))
            {
                string osConfigDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    osConfigDir = Environment.GetEnvironmentVariable("HOME");
                    if (String.IsNullOrEmpty(osConfigDir))
                    {
                        return "."; // Oh well.
                    }
                    osConfigDir += "/.local/share";
                }
                return Path.Combine(osConfigDir, GameName);
            }
            throw new Exception("SDL platform unhandled: " + platform);
        }
#endif

#if XNA_BUILD
        public static string GetGameDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + ProgramData.DirChar + GameName;
        }
#endif

        public static void InitializeLogger()
        {
            Trace.Listeners.Clear();
            var dir = GetGameDirectory();
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            var path = ProgramData.CreatePath(dir, "log.txt");
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }

            FileStream writerOutput = new FileStream(ProgramData.CreatePath(dir, "log.txt"), FileMode.Append, FileAccess.Write);
            _logwriter = new StreamWriter(writerOutput) {AutoFlush = true};
            Console.SetOut(_logwriter);
            Console.SetError(_logwriter);
            Console.Out.WriteLine("Game started at " + DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString()); 
        }

        protected override void Initialize()
        {
#if SHARP_RAVEN
            try
            {
#endif
                InitializeLogger();
                Thread.CurrentThread.Name = "Main";
                // Goes before anything else so we can track from the very start.
                GamePerformance.Initialize(this);

                SpriteBatch = new SpriteBatch(GraphicsDevice);
                base.Initialize();
#if SHARP_RAVEN
            }
            catch (Exception exception)
            {
                if (ravenClient != null)
                    ravenClient.Capture(new SentryEvent(exception));
                throw;
            }
#endif
        }

        protected override void LoadContent()
        {
#if SHARP_RAVEN
            try
            {
#endif
                // Prepare GemGui
                GumInputMapper = new Gui.Input.GumInputMapper(Window.Handle);
                GumInput = new Gui.Input.Input(GumInputMapper);

                // Register all bindable actions with the input system.
                GumInput.AddAction("TEST", Gui.Input.KeyBindingType.Pressed);

                GumSkin = new RenderData(GraphicsDevice, Content,
                        "newgui/xna_draw", "Content/newgui/sheets.txt");

                if (SoundManager.Content == null)
                {
                    SoundManager.Content = Content;
                    SoundManager.LoadDefaultSounds();
#if XNA_BUILD
                    //SoundManager.SetActiveSongs(ContentPaths.Music.dwarfcorp, ContentPaths.Music.dwarfcorp_2,
                    //    ContentPaths.Music.dwarfcorp_3, ContentPaths.Music.dwarfcorp_4, ContentPaths.Music.dwarfcorp_5);
#endif
                }

                if (GameSettings.Default.DisplayIntro)
                {
                    StateManager.PushState(new IntroState(this, StateManager));
                }
                else
                {
                    StateManager.PushState(new MainMenuState(this, StateManager));
                }

                BiomeLibrary.InitializeStatics();
                Embarkment.Initialize();
                VoxelChunk.InitializeStatics();
                ControlSettings.Load();
                Drawer2D.Initialize(Content, GraphicsDevice);
                ResourceLibrary.Initialize();
                base.LoadContent();
#if SHARP_RAVEN
            }
            catch (Exception exception)
            {
                if (ravenClient != null)
                    ravenClient.Capture(new SentryEvent(exception));
                throw;
            }
#endif
        }

        protected override void Update(GameTime time)
        {
            if (!IsActive)
            {
                base.Update(time);
                return;
            }
#if SHARP_RAVEN
            try
            {
#endif
                GamePerformance.Instance.PreUpdate();
                DwarfTime.LastTime.Update(time);
                StateManager.Update(DwarfTime.LastTime);
                base.Update(time);
                GamePerformance.Instance.PostUpdate();
#if SHARP_RAVEN
            }
            catch (Exception exception)
            {
                if (ravenClient != null)
                    ravenClient.Capture(new SentryEvent(exception));
                throw;
            }
#endif
        }

        protected override void Draw(GameTime time)
        {
#if SHARP_RAVEN
            try
            {
#endif
                GamePerformance.Instance.PreRender();
                StateManager.Render(DwarfTime.LastTime);
                GraphicsDevice.SetRenderTarget(null);
                base.Draw(time);
                GamePerformance.Instance.PostRender();
                GamePerformance.Instance.Render(SpriteBatch);
#if SHARP_RAVEN
            }
            catch (Exception exception)
            {
                if (ravenClient != null)
                    ravenClient.Capture(new SentryEvent(exception));
                throw;
            }
#endif
            
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            _logwriter.Dispose();
            ExitGame = true;
            Program.SignalShutdown();
            base.OnExiting(sender, args);
        }

        public static bool ExitGame = false;
    }

}
