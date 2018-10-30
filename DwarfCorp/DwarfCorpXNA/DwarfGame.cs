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
using System.Collections.Generic;

namespace DwarfCorp
{

    public class DwarfGame : Game
    {
#if XNA_BUILD
        public static bool COMPRESSED_BINARY_SAVES = true;
#else
        public static bool COMPRESSED_BINARY_SAVES = false;
#endif

        private class LogWriter : StreamWriter
        {
            private Gui.Widgets.DwarfConsole ConsoleLogOutput = null;
            private System.Text.StringBuilder PreConsoleLogQueue = new System.Text.StringBuilder();
            private TextWriter _mainOut;
            public void SetConsole(Gui.Widgets.DwarfConsole Console)
            {
                this.ConsoleLogOutput = Console;
                Console.AddMessage(PreConsoleLogQueue.ToString());
            }

            public LogWriter(TextWriter mainOut, FileStream Output) : base(Output)
            {
                _mainOut = mainOut;
                AutoFlush = true;
            }

            public override void Write(char value)
            {
                _mainOut.Write(value);
                if (ConsoleLogOutput != null)
                    ConsoleLogOutput.Append(value);
                else
                    PreConsoleLogQueue.Append(value);

                base.Write(value);
            }

            //public override void Write(string value)
            //{
            //    if (Console != null) Console.AddMessage(value);
            //    base.Write(value);
            //}

            //public override void Write(char[] buffer)
            //{
            //    if (Console != null) foreach (var c in buffer) Console.Append(c);
            //    base.Write(buffer);
            //}

            public override void Write(char[] buffer, int index, int count)
            {
                _mainOut.Write(buffer, index, count);
                if (ConsoleLogOutput != null)
                    for (var x = index; x < index + count; ++x)
                        ConsoleLogOutput.Append(buffer[x]);
                else
                    PreConsoleLogQueue.Append(buffer, index, count);

                base.Write(buffer, index, count);
            }

            public override void WriteLine(string value)
            {
                foreach (var c in value) Write(c);
                Write('\n');
            }
        }

        public GameStateManager StateManager { get; set; }
        public GraphicsDeviceManager Graphics;
        public AssetManager TextureManager { get; set; }
        public static SpriteBatch SpriteBatch { get; set; }

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

        public class SaveLoadTester
        {
            public enum State
            {
                StartingNewGame,
                WaitingForGameStart,
                SavingNewGame,
                LoadingSavedGame,
                WaitingForLoading,
                Done
            }


            public State LoadState = State.StartingNewGame;

            public void Update(DwarfGame game)
            {
                switch (LoadState)
                {
                    case State.StartingNewGame:
                        Console.Error.WriteLine("Save/load test: starting new game...");
                        game.StateManager.PushState(new LoadState(game, game.StateManager, new WorldGenerationSettings() { GenerateFromScratch = true }));
                        LoadState = State.WaitingForGameStart;
                        break;
                    case State.WaitingForGameStart:
                        if (game.StateManager.CurrentState is PlayState)
                        {
                            PlayState state = game.StateManager.CurrentState as PlayState;
                            if (state.IsInitialized)
                            {
                                Console.Error.WriteLine("Save/load test: doing autosave...");
                                state.AutoSave();
                                LoadState = State.SavingNewGame;
                            }
                        }
                        break;
                    case State.SavingNewGame:
                        {
                            PlayState state = game.StateManager.CurrentState as PlayState;
                            if (state != null && state.IsInitialized && !state.Paused)
                            {
                                Console.Error.WriteLine("Save/load test: loading the saved game...");
                                LoadState = State.LoadingSavedGame;
                            }
                            break;
                        }
                    case State.LoadingSavedGame:
                        {
                            string latestSave = SaveGame.GetLatestSaveFile();

                            if (latestSave != null)
                            {
                                Console.Error.WriteLine("Save/load test: starting to load...");
                                PlayState state = game.StateManager.CurrentState as PlayState;
                                if (state != null && state.IsInitialized && !state.Paused)
                                {
                                    state.QuitGame(new LoadState(game, game.StateManager, new WorldGenerationSettings()
                                    {
                                        ExistingFile = latestSave
                                    }));
                                }
                                LoadState = State.WaitingForLoading;
                            }
                            break;
                        }
                    case State.WaitingForLoading:
                        if (game.StateManager.CurrentState is PlayState)
                        {
                            PlayState state = game.StateManager.CurrentState as PlayState;
                            if (state.IsInitialized)
                            {
                                Console.Error.WriteLine("Save/load test completed successfully.");
                                LoadState = State.Done;
                            }
                        }
                        break;
                    case State.Done:
                        return;
                }

            }
        }

        private SaveLoadTester _tester = null;

        [ConsoleCommandHandler("PALETTE")]
        private static String DumpPalette(String Path)
        {
            var palette = TextureTool.ExtractPaletteFromDirectoryRecursive(Path);
            var paletteTexture = TextureTool.Texture2DFromMemoryTexture(DwarfGame.GuiSkin.Device, TextureTool.MemoryTextureFromPalette(palette));
            paletteTexture.SaveAsPng(System.IO.File.OpenWrite("palette.png"), paletteTexture.Width, paletteTexture.Height);
            return "Dumped.";
        }

#if SHARP_RAVEN && !DEBUG
        private RavenClient ravenClient;
#endif

        public void DoSaveLoadtest()
        {
            _tester = new SaveLoadTester();
        }

        public DwarfGame()
        {
            //BoundingBox foo = new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
            //string serialized = FileUtils.SerializeBasicJSON(foo);
            //BoundingBox deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<BoundingBox>(serialized, new BoxConverter());
            //string code = ContentPathGenerator.GenerateCode();
            //Console.Out.Write(code);
            GameState.Game = this;
            //Content.RootDirectory = "Content";
            StateManager = new GameStateManager(this);
            Graphics = new GraphicsDeviceManager(this);
            Window.Title = "DwarfCorp";
            Window.AllowUserResizing = false;
            MainThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            GameSettings.Load();

            try
            {
#if SHARP_RAVEN && !DEBUG
                if (GameSettings.Default.AllowReporting)
                {
                    ravenClient =
                        new RavenClient(
                            "https://af78a676a448474dacee4c72a9197dd2:0dd0a01a9d4e4fa4abc6e89ac7538346@sentry.io/192119");
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
            Graphics.PreparingDeviceSettings += WorldManager.GraphicsPreparingDeviceSettings;
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
            {
                Exit();
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

     
        public static void InitializeLogger()
        {
#if DEBUG
            return;
#endif
            try
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
                
                var logFile = new FileInfo(path);
                if (logFile.Length > 5e7)
                {
                    Console.Out.WriteLine("Log file at {0} was too large ({1} bytes). Clearing it.", path, logFile.Length);
                    System.IO.File.WriteAllText(path, string.Empty);
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

        public void TriggerRavenEvent(string message, string details)
        {
#if SHARP_RAVEN && !DEBUG
            if (ravenClient == null)
                return;
            var exception = new Exception(message);
            exception.Data["Details"] = details;
            ravenClient.Capture(new SentryEvent(exception));
#endif
        }

        protected override void Initialize()
        {
#if SHARP_RAVEN && !DEBUG
            try
            {
#endif
                var dir = GetGameDirectory();
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
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
            AssetManager.Initialize(Content, GraphicsDevice, GameSettings.Default);

            //var palette = TextureTool.ExtractPaletteFromDirectoryRecursive("Entities/Dwarf");
            //var paletteTexture = TextureTool.Texture2DFromMemoryTexture(GraphicsDevice, TextureTool.MemoryTextureFromPalette(palette));
            //paletteTexture.SaveAsPng(System.IO.File.OpenWrite("palette.png"), paletteTexture.Width, paletteTexture.Height);

            // Prepare GemGui
            GumInputMapper = new Gui.Input.GumInputMapper(Window.Handle);
                GumInput = new Gui.Input.Input(GumInputMapper);

                // Register all bindable actions with the input system.
                //GumInput.AddAction("TEST", Gui.Input.KeyBindingType.Pressed);

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
#if XNA_BUILD
                    //SoundManager.SetActiveSongs(ContentPaths.Music.dwarfcorp, ContentPaths.Music.dwarfcorp_2,
                    //    ContentPaths.Music.dwarfcorp_3, ContentPaths.Music.dwarfcorp_4, ContentPaths.Music.dwarfcorp_5);
#endif
                }

                if (StateManager.StateStack.Count == 0)
                {
                    if (GameSettings.Default.DisplayIntro)
                    {
                        StateManager.PushState(new IntroState(this, StateManager));
                    }
                    else
                    {
                        StateManager.PushState(new MainMenuState(this, StateManager));
                    }
                }
                BiomeLibrary.InitializeStatics();
                EmbarkmentLibrary.InitializeDefaultLibrary();
                VoxelChunk.InitializeStatics();
                ControlSettings.Load();
                Drawer2D.Initialize(Content, GraphicsDevice);
                ResourceLibrary.Initialize();
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
            if (_tester != null)
            {
                _tester.Update(this);
                if (_tester.LoadState == SaveLoadTester.State.Done)
                {
                    Environment.Exit(0);
                }
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
            StateManager.Update(DwarfTime.LastTime);
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
                StateManager.Render(DwarfTime.LastTime);
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
            get { return System.Threading.Thread.CurrentThread.ManagedThreadId == MainThreadID; }
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

    public class EventLogState : GameStates.GameState
    {
        public EventLog Log { get; set; }
        public DateTime Now { get; set; }
        public Gui.Root GuiRoot { get; set; }
        public EventLogViewer Viewer { get; set; }

        public EventLogState(DwarfGame game, GameStateManager manager, EventLog log, DateTime now) :
            base(game, "EventLog", manager)
        {
            Log = log;
            Now = now;
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void OnEnter()
        {
            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            Viewer = GuiRoot.RootItem.AddChild(new EventLogViewer()
            {
                Log = Log,
                Now = Now,
                Rect = GuiRoot.RenderData.VirtualScreen,
                AutoLayout = AutoLayout.DockFill,
                InteriorMargin = new Margin(32, 32, 16, 16)
            }) as EventLogViewer;
            Viewer.CloseButton.OnClick = (sender, args) =>
            {
                StateManager.PopState();
            };
            // Must be true or Render will not be called.
            IsInitialized = true;
            GuiRoot.RootItem.Layout();
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnPopped()
        {
            base.OnPopped();
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }

        public override void RenderUnitialized(DwarfTime gameTime)
        {
            base.RenderUnitialized(gameTime);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                    if (@event.Message == InputEvents.KeyUp && @event.Args.KeyValue == (int)Microsoft.Xna.Framework.Input.Keys.Escape)
                    {
                        StateManager.PopState();
                    }
                }
            }

            GuiRoot.Update(gameTime.ToRealTime());
            SoundManager.Update(gameTime, null, null);
            base.Update(gameTime);
        }
    }


    public class EventLogViewer : Gui.Widget
    {
        public EventLog Log { get; set; }
        public DateTime Now { get; set; }
        public Widget CloseButton { get; set; }
        public override void Construct()
        {
            Border = "border-fancy";
            AddChild(new Gui.Widget()
            {
                Text = "Events",
                Font = "font16",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(256, 32)
            });
            Gui.Widgets.WidgetListView listView = AddChild(new Gui.Widgets.WidgetListView()
            {
                AutoLayout = AutoLayout.DockTop,
                SelectedItemForegroundColor = Color.Black.ToVector4(),
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0),
                ItemHeight = 32,
                MinimumSize = new Point(0, 3 * Root.RenderData.VirtualScreen.Height / 4)
            }) as Gui.Widgets.WidgetListView;
            foreach (var logged in Log.GetEntries().Reverse())
            {
                listView.AddItem(Root.ConstructWidget(new Widget()
                {
                    Background = new TileReference("basic", 0),
                    Text = TextGenerator.AgeToString(Now - logged.Date) + " " + logged.Text,
                    Tooltip = logged.Details,
                    TextColor = logged.TextColor.ToVector4(),
                    Font = "font10",
                    MinimumSize = new Point(640, 32),
                    Padding = new Margin(0, 0, 4, 4),
                    TextVerticalAlign = VerticalAlign.Center
                }));
            }
            CloseButton = AddChild(new Gui.Widgets.Button()
            {
                Text = "Close",
                Font = "font10",
                Border = "border-button",
                MinimumSize = new Point(128, 32),
                AutoLayout = AutoLayout.FloatBottomRight
            });
            Layout();
            base.Construct();
        }
    }

    public class EventLog
    {
        public struct LogEntry
        {
            public string Text;
            public string Details;
            public DateTime Date;
            public Color TextColor;
        }

        private List<LogEntry> Entries = new List<LogEntry>();
        private TimeSpan MaxDuration = new TimeSpan(10, 0, 0, 0, 0);

        public IEnumerable<LogEntry> GetEntries()
        {
            return Entries;
        }

        public void AddEntry(LogEntry entry)
        {
            // Deduplication of entries.
            if (Entries.Any(e => e.Text == entry.Text && (entry.Date - e.Date) < new TimeSpan(0, 1, 0, 0, 0)))
                return;

            Console.Out.WriteLine(entry.Text);
            Entries.Add(entry);
            Entries.RemoveAll(e => (entry.Date - e.Date) > MaxDuration);
        }

        public EventLog()
        {

        }
    }
}
