using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Newtonsoft.Json;
#if !XNA_BUILD && !GEMMONO
using SDL2;
#endif
using SharpRaven;
using SharpRaven.Data;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class DwarfGame : Game
    {
        public GraphicsDeviceManager Graphics;
        public static SpriteBatch SpriteBatch { get; set; }
        public Terrain2D ScreenSaver { get; set; }

        public static Gui.Input.GumInputMapper GumInputMapper;
        public static Gui.Input.Input GumInput;
        public static Gui.RenderData GuiSkin;

        private static Gui.Root ConsoleGui;
        private static bool ConsoleVisible = false;
        public static bool IsConsoleVisible { get { return ConsoleVisible; } }
        public static Gui.Widget ConsolePanel { get { return ConsoleGui.RootItem.GetChild(0); } }

        public const string GameName = "DwarfCorp";
        public static bool HasRendered = false;
        private static LogWriter _logwriter;
        private static TextWriter _initialOut;
        private static TextWriter _initialError;

        private static int MainThreadID;

        public class LazyAction
        {
            public Action Action;
            public Func<bool> Result;
        }

        private List<LazyAction> _lazyActions = new List<LazyAction>();
        private object _actionMutex = new object();
                
#if SHARP_RAVEN && !DEBUG
        private static RavenClient ravenClient;
#endif

        public DwarfGame()
        {
            GameState.Game = this;
            Graphics = new GraphicsDeviceManager(this);
            Window.Title = "DwarfCorp";
            Window.AllowUserResizing = false;
            MainThreadID = Thread.CurrentThread.ManagedThreadId;
            GameSettings.Load();

            try
            {
#if SHARP_RAVEN && !DEBUG
                if (GameSettings.Default.AllowReporting)
                {
                    ravenClient = new RavenClient("https://af78a676a448474dacee4c72a9197dd2:0dd0a01a9d4e4fa4abc6e89ac7538346@sentry.io/192119");
                    ravenClient.Tags["Version"] = Program.Version;
                    ravenClient.Tags["Commit"] = Program.Commit;

#if XNA_BUILD
                    ravenClient.Tags["Platform"] = "XNA";
                    ravenClient.Tags["OS"] = "Windows";
#else
                    ravenClient.Tags["Platform"] = "FNA";
                    ravenClient.Tags["OS"] = SDL.SDL_GetPlatform();
#endif
                }
#endif
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
            }

            // Check GUI scale - if the settings are bad, fix.
            if (GameSettings.Default.GuiScale * 480 > GameSettings.Default.ResolutionY)
                GameSettings.Default.GuiScale = 1;

            Graphics.IsFullScreen = GameSettings.Default.Fullscreen;
            Graphics.PreferredBackBufferWidth = GameSettings.Default.Fullscreen ? GameSettings.Default.ResolutionX : Math.Min(GameSettings.Default.ResolutionX, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width);
            Graphics.PreferredBackBufferHeight = GameSettings.Default.Fullscreen ? GameSettings.Default.ResolutionY : Math.Min(GameSettings.Default.ResolutionY,
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);
            Graphics.SynchronizeWithVerticalRetrace = GameSettings.Default.VSync;
            MathFunctions.Random = new ThreadSafeRandom(new Random().Next());
            Graphics.PreparingDeviceSettings += WorldRenderer.GraphicsPreparingDeviceSettings;
            Graphics.PreferMultiSampling = false;
            try
            {
                Graphics.ApplyChanges();
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                Console.Error.WriteLine(exception.Message);
#if SHARP_RAVEN && !DEBUG
                if (ravenClient != null)
                    ravenClient.Capture(new SentryEvent(exception));
#endif
            }

            if (AssetManagement.Steam.Steam.InitializeSteam() == AssetManagement.Steam.Steam.SteamInitializationResult.QuitImmediately)
                Exit();
        }

#if !XNA_BUILD && !GEMMONO
        public static string GetGameDirectory()
        {
            string platform = SDL.SDL_GetPlatform();
            if (platform.Equals("Windows"))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameName);
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

#if XNA_BUILD || GEMMONO
        public static string GetGameDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + GameName;
        }
#endif

        public static string GetSaveDirectory()
        {
            if (String.IsNullOrEmpty(GameSettings.Default.SaveLocation))
                return DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Saves";
            else
                return GameSettings.Default.SaveLocation + Path.DirectorySeparatorChar + "Saves";
        }

        public static string GetWorldDirectory()
        {
            if (String.IsNullOrEmpty(GameSettings.Default.SaveLocation))
                return DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Worlds";
            else
                return GameSettings.Default.SaveLocation + Path.DirectorySeparatorChar + "Worlds";
        }


        public void DoLazyAction(Action action, Func<bool> callback = null)
        {
            lock(_actionMutex)
            {
                _lazyActions.Add(new LazyAction() { Action = action, Result = callback });
            }
        }
     
        public static void InitializeLogger()
        {
#if DEBUG
            return;
#endif
            try
            {
                Trace.Listeners.Clear();
                var dir = GetGameDirectory();
                if (!global::System.IO.Directory.Exists(dir))
                {
                    global::System.IO.Directory.CreateDirectory(dir);
                }
                var path = ProgramData.CreatePath(dir, "log.txt");
                if (!File.Exists(path))
                {
                    File.Create(path).Close();
                }
                
                var logFile = new FileInfo(path);
                if (logFile.Length > 5e7)
                {
                    Console.Out.WriteLine("Log file at {0} was too large ({1} bytes). Clearing it.", path, logFile.Length);
                    global::System.IO.File.WriteAllText(path, string.Empty);
                }
                FileStream writerOutput = new FileStream(path, FileMode.Append, FileAccess.Write);
                _logwriter = new LogWriter(Console.Out, writerOutput);
                _initialOut = Console.Out;
                _initialError = Console.Error;
                Console.SetOut(_logwriter);
                Console.SetError(_logwriter);
                Console.Out.WriteLine("Game started at " + DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString());
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Failed to initialize logger: {0}", exception.ToString());
            }
        }

        public static void TriggerRavenEvent(string message, string details)
        {
#if SHARP_RAVEN && !DEBUG
            if (ravenClient == null)
                return;
            var exception = new Exception(message);
            exception.Data["Details"] = details;
            ravenClient.Capture(new SentryEvent(exception));
#endif
        }

        public static void LogSentryBreadcrumb(string category, string message, BreadcrumbLevel level = BreadcrumbLevel.Info)
        {
            Console.Out.WriteLine(String.Format("{0} : {1}", category, message));
#if SHARP_RAVEN && !DEBUG
            if (ravenClient != null)
            {
                ravenClient.AddTrail(new Breadcrumb(category) { Message = message, Type = BreadcrumbType.Navigation });
            }
#endif
        }

        protected override void Initialize()
        {
#if SHARP_RAVEN && !DEBUG
            try
            {
#endif
                var dir = GetGameDirectory();
                if (!global::System.IO.Directory.Exists(dir))
                {
                    global::System.IO.Directory.CreateDirectory(dir);
                }
                InitializeLogger();
                Thread.CurrentThread.Name = "Main";
                // Goes before anything else so we can track from the very start.

                SpriteBatch = new SpriteBatch(GraphicsDevice);
                base.Initialize();
#if SHARP_RAVEN && !DEBUG
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
#if SHARP_RAVEN && !DEBUG
            try
            {
#endif
            LogSentryBreadcrumb("Loading", "LoadContent was called.", BreadcrumbLevel.Info);
            AssetManager.Initialize(Content, GraphicsDevice, GameSettings.Default);

            // Prepare GemGui
            if (GumInputMapper == null)
            {
                GumInputMapper = new Gui.Input.GumInputMapper(Window.Handle);
                GumInput = new Gui.Input.Input(GumInputMapper);
            }

                GuiSkin = new RenderData(GraphicsDevice, Content);

            // Create console.
            ConsoleGui = new Gui.Root(GuiSkin);
            ConsoleGui.RootItem.AddChild(new Gui.Widgets.AutoGridPanel
            {
                Rows = 2,
                Columns = 4,
                AutoLayout = AutoLayout.DockFill
            });

            ConsoleGui.RootItem.Layout();
            if (_logwriter != null)
                _logwriter.SetConsole(GetConsoleTile("LOG"));

            Console.Out.WriteLine("Console created.");

                if (SoundManager.Content == null)
                {
                    SoundManager.Content = Content;
                    SoundManager.LoadDefaultSounds();
                }

                if (GameStateManager.StateStackIsEmpty)
                {
                    LogSentryBreadcrumb("GameState", "There was nothing in the state stack. Starting over.");
                    if (GameSettings.Default.DisplayIntro)
                        GameStateManager.PushState(new IntroState(this));
                    else
                        GameStateManager.PushState(new MainMenuState(this));
                }

                ControlSettings.Load();
                Drawer2D.Initialize(Content, GraphicsDevice);
            ScreenSaver = new Terrain2D(this);

            base.LoadContent();

#if SHARP_RAVEN && !DEBUG
            }
            catch (Exception exception)
            {
                if (ravenClient != null)
                    ravenClient.Capture(new SentryEvent(exception));
                throw;
            }
#endif
            }

        public static void RebuildConsole()
        {
            ConsoleGui.RootItem.Layout();
        }

        public void CaptureException(Exception exception)
        {
#if SHARP_RAVEN && !DEBUG
            if (ravenClient != null)
                ravenClient.Capture(new SentryEvent(exception));
#endif
        }

        protected override void Update(GameTime time)
        {
            if (!IsActive)
            {
                base.Update(time);
                return;
            }
           
#if SHARP_RAVEN && !DEBUG
            try
            {
#endif
            if (GumInputMapper.WasConsoleTogglePressed())
            {
                ConsoleVisible = !ConsoleVisible;

                if (ConsoleVisible)
                {
                    var commandPanel = GetConsoleTile("COMMAND");
                    commandPanel.AddCommandEntry();
                    ConsoleGui.SetFocus(commandPanel.Children[0]);
                }
            }

            if (ConsoleVisible)
            {
                ConsoleGui.Update(time);
                if (ConsoleGui.FocusItem != null)
                    DwarfGame.GumInput.FireKeyboardActionsOnly(ConsoleGui);
            }

            PerformanceMonitor.BeginFrame();
            PerformanceMonitor.PushFrame("Update");
            AssetManagement.Steam.Steam.Update();
            DwarfTime.LastTime.Update(time);
                GameStateManager.Update(DwarfTime.LastTime);

            lock (_actionMutex)
            {
                foreach (var action in _lazyActions)
                {
                    action.Action();
                    action.Result?.Invoke();
                }
                _lazyActions.Clear();
            }

            base.Update(time);
            PerformanceMonitor.PopFrame();
#if SHARP_RAVEN && !DEBUG
            }
            catch (Exception exception)
            {
                if (ravenClient != null)
                    ravenClient.Capture(new SentryEvent(exception));
                throw;
            }
#endif
            HasRendered = false;
        }

        protected override void Draw(GameTime time)
        {

            if (GraphicsDevice.IsDisposed) return;

            HasRendered = true;
#if SHARP_RAVEN && !DEBUG
            try
            {
#endif
            PerformanceMonitor.PushFrame("Render");

            GraphicsDevice.Clear(Color.Black);

            if (GameStateManager.DrawScreensaver)
                ScreenSaver.Render(GraphicsDevice, DwarfTime.LastTime);

                GameStateManager.Render(DwarfTime.LastTime);

            GraphicsDevice.SetRenderTarget(null);
                base.Draw(time);
            PerformanceMonitor.PopFrame();
            PerformanceMonitor.Render();

            if (ConsoleVisible)
                ConsoleGui.Draw();

#if SHARP_RAVEN && !DEBUG
            }
            catch (Exception exception)
            {
                if (ravenClient != null)
                    ravenClient.Capture(new SentryEvent(exception));
                throw;
            }
#endif
        }

        public static void SafeSpriteBatchBegin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerstate, 
            DepthStencilState depthState, RasterizerState rasterState, Effect effect, Matrix world)
        {
            Debug.Assert(IsMainThread);
            if (SpriteBatch.GraphicsDevice.IsDisposed || SpriteBatch.IsDisposed)
            {
                SpriteBatch = new SpriteBatch(GameState.Game.GraphicsDevice);
            }

            try
            {
                SpriteBatch.Begin(sortMode,
                    blendState,
                    samplerstate,
                    depthState,
                    rasterState,
                    effect,
                    world);
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.Write(exception);
                SpriteBatch.Dispose();
                SpriteBatch = new SpriteBatch(GameState.Game.GraphicsDevice);
                SpriteBatch.Begin(sortMode,
                    blendState,
                    samplerstate,
                    depthState,
                    rasterState,
                    effect,
                    world);
            }
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            if (_initialOut != null)
                Console.SetOut(_initialOut);
            if (_initialError != null)
                Console.SetError(_initialError);
            if (_logwriter != null)
                _logwriter.Dispose();
            ExitGame = true;
            Program.SignalShutdown();
            base.OnExiting(sender, args);
        }

        // If called in the non main thread, will return false;
        public static bool IsMainThread
        {
            get { return Thread.CurrentThread.ManagedThreadId == MainThreadID; }
        }

        public static bool ExitGame = false;
        
        public static Gui.Widgets.DwarfConsole GetConsoleTile(String Name)
        {
            var display = DwarfGame.ConsolePanel.EnumerateChildren().Where(c =>
            {
                if (c.Tag is String tag) return tag == Name;
                return false;
            }).FirstOrDefault() as Gui.Widgets.DwarfConsole;

            if (display == null)
            {
                display = DwarfGame.ConsolePanel.AddChild(new Gui.Widgets.DwarfConsole
                {
                    Background = new TileReference("basic", 1),
                    BackgroundColor = new Vector4(1.0f, 1.0f, 1.0f, 0.25f),
                    Tag = Name
                }) as Gui.Widgets.DwarfConsole;

                DwarfGame.RebuildConsole();
            }

            return display;
        }
    }
}
