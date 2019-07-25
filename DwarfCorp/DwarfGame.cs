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
    public class HandledException: Exception
    {
        public HandledException(Exception e) : base("Exception handled, user aborting", e)
        { }
    }

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

        public DwarfGame()
        {
            GameState.Game = this;
            Graphics = new GraphicsDeviceManager(this);
            Window.Title = "DwarfCorp";
            Window.AllowUserResizing = false;
            MainThreadID = Thread.CurrentThread.ManagedThreadId;
            GameSettings.Load();

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
            Graphics.ApplyChanges();

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
            lock (_actionMutex)
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

        // Todo: Kill passthrough
        public static void LogSentryBreadcrumb(string category, string message, BreadcrumbLevel level = BreadcrumbLevel.Info)
        {
            Program.LogSentryBreadcrumb(category, message, level);
        }

        protected override void Initialize()
        {
            var dir = GetGameDirectory();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            InitializeLogger();
            Thread.CurrentThread.Name = "Main";
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            base.Initialize();
        }

        protected override void LoadContent()
        {
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
        }

        public static void RebuildConsole()
        {
            ConsoleGui.RootItem.Layout();
        }

        protected override void Update(GameTime time)
        {
            if (!IsActive)
            {
                base.Update(time);
                return;
            }

#if !DEBUG
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
#if !DEBUG
            }
            catch (HandledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Program.CaptureException(exception);
                if (Program.ShowErrorDialog(exception.Message))
                    throw new HandledException(exception);
            }
#endif
            HasRendered = false;
        }

        protected override void Draw(GameTime time)
        {
            if (GraphicsDevice.IsDisposed) return;

            HasRendered = true;

#if !DEBUG
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

#if !DEBUG
            }
            catch (Exception exception)
            {
                Program.CaptureException(exception);
                if (Program.ShowErrorDialog(exception.Message))
                    throw new HandledException(exception);
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
                    BackgroundColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                    Tag = Name
                }) as Gui.Widgets.DwarfConsole;

                DwarfGame.RebuildConsole();
            }

            return display;
        }
    }
}
