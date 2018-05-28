// PlayState.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using BloomPostprocess;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using Newtonsoft.Json;
using DwarfCorp.Goals;

namespace DwarfCorp
{
    /// <summary>
    /// This is the main game state for actually playing the game.
    /// </summary>
    public partial class WorldManager : IDisposable
    {
        #region fields

        // The random seed of the whole game
        public int Seed { get; set; }

        // Defines the number of pixels in the overworld to number of voxels conversion
        public float WorldScale
        {
            get { return GameSettings.Default.WorldScale; }
            set { GameSettings.Default.WorldScale = value; }
        }

        public float SlicePlane = 0;

        // Used to pass WorldOrigin from the WorldGenState into 
        public Vector2 WorldGenerationOrigin { get; set; }

        // The origin of the overworld in pixels [(0, 0, 0) in world space.]
        public Vector2 WorldOrigin { get; set; }

        // The current coordinate of the cursor light
        public Vector3 CursorLightPos = Vector3.Zero;

        public Vector3[] LightPositions = new Vector3[16];

        // The texture used for the terrain tiles.
        public Texture2D Tilesheet;

        // The shader used to draw the terrain and most entities
        public Shader DefaultShader;

        // The player's view into the world.
        public OrbitCamera Camera;

        // Gives the number of antialiasing multisamples. 0 means no AA. 
        public int MultiSamples
        {
            get { return GameSettings.Default.AntiAliasing; }
            set { GameSettings.Default.AntiAliasing = value; }
        }

        public bool UseFXAA
        {
            get { return MultiSamples == -1; }
        }

        // The ratio of width to height in screen pixels. (ie 16/9 or 4/3)
        public float AspectRatio = 0.0f;

        // Responsible for managing terrain
        public ChunkManager ChunkManager = null;
        public ChunkRenderer ChunkRenderer = null;

        // Maps a set of voxel types to assets and properties
        public VoxelLibrary VoxelLibrary = null;

        // Responsible for creating terrain
        public ChunkGenerator ChunkGenerator = null;

        // Responsible for managing game entities
        public ComponentManager ComponentManager = null;

        public FactionLibrary Factions = null;

        [JsonIgnore]
        public OctTreeNode<Body> OctTree = null;
        public ParticleManager ParticleManager = null;

        // Handles interfacing with the player and sending commands to dwarves
        public GameMaster Master = null;

        public Goals.GoalManager GoalManager;

        #region Tutorial Hooks

        public Tutorial.TutorialManager TutorialManager;
        
        public void Tutorial(String Name)
        {
            if (TutorialManager != null)
                TutorialManager.ShowTutorial(Name);
        }

        #endregion

        public Diplomacy Diplomacy;

        // If the game was loaded from a file, this contains the name of that file.
        public string ExistingFile = "";

        // Just a helpful 1x1 white pixel texture
        private Texture2D pixel;

        // A shader which draws fancy light blooming to the screen
        private BloomComponent bloom;
        
        private FXAA fxaa;

        // Responsible for drawing liquids.
        public WaterRenderer WaterRenderer;

        // Responsible for drawing the skybox
        public SkyRenderer Sky;

        // Draws shadow maps
        public ShadowRenderer Shadows;

        // Draws a selection buffer (for pixel-perfect selection)
        public SelectionBuffer SelectionBuffer;

        // Responsible for handling instances of particular primitives (or models)
        // and drawing them to the screen
        public InstanceRenderer InstanceRenderer;

        // Handles loading of game assets
        public ContentManager Content;

        // Reference to XNA Game
        public DwarfGame Game;

        // Interfaces with the graphics card
        public GraphicsDevice GraphicsDevice { get { return GameState.Game.GraphicsDevice; } }

        // Loads the game in the background while a loading message displays
        public Thread LoadingThread { get; set; }

        // Callback to set message on loading screen.
        public Action<String> OnSetLoadingMessage = null;

        public void SetLoadingMessage(String Message)
        {
            if (OnSetLoadingMessage != null)
                OnSetLoadingMessage(Message);
        }

        private bool paused_ = false;
        // True if the game's update loop is paused, false otherwise
        public bool Paused
        {
            get { return paused_; }
            set
            {
                paused_ = value;

                if (DwarfTime.LastTime != null)
                    DwarfTime.LastTime.IsPaused = paused_;
            }
        }

        // Handles a thread which constantly runs A* plans for whoever needs them.
        public PlanService PlanService = null;

        // Maintains a dictionary of biomes (forest, desert, etc.)
        public BiomeLibrary BiomeLibrary = new BiomeLibrary();

        // Contains the storm forecast
        public Weather Weather = new Weather();

        // The current calendar date/time of the game.
        public WorldTime Time = new WorldTime();

        // Hack to smooth water reflections TODO: Put into water manager
        private float lastWaterHeight = 8.0f;

        private SaveGame gameFile;

        public Point3 WorldSize { get; set; }

        public Action<QueuedAnnouncement> OnAnnouncement;

        public void MakeAnnouncement(String Message, Action<Gui.Root, QueuedAnnouncement> ClickAction = null, Func<bool> Keep = null)
        {
            if (OnAnnouncement != null)
                OnAnnouncement(new QueuedAnnouncement
                {
                    Text = Message,
                    ClickAction = ClickAction,
                    ShouldKeep = Keep
                });
        }

        public void MakeAnnouncement(QueuedAnnouncement Announcement)
        {
            if (OnAnnouncement != null)
                OnAnnouncement(Announcement);
        }

        public void AwardBux(DwarfBux Bux)
        {
            PlayerFaction.AddMoney(Bux);
            MakeAnnouncement(String.Format("Gained {0}", Bux));
        }

        public void LoseBux(DwarfBux Bux)
        {
            PlayerFaction.AddMoney(-Bux);
            MakeAnnouncement(String.Format("Lost {0}", Bux));
        }

        public MonsterSpawner MonsterSpawner { get; set; }

        public Company PlayerCompany
        {
            get { return Master.Faction.Economy.Company; }
        }

        public Faction PlayerFaction
        {
            get { return Master.Faction; }
        }

        public DesignationDrawer DesignationDrawer = new DesignationDrawer();

        public Economy PlayerEconomy
        {
            get { return Master.Faction.Economy; }
        }

        public List<Faction> Natives { get; set; }

        public int GameID = -1;

        public struct Screenshot
        {
            public string FileName { get; set; }
            public Point Resolution { get; set; }
        }

        public List<Screenshot> Screenshots { get; set; }
        private object ScreenshotLock = new object();

        public bool ShowingWorld { get; set; }

        public GameState gameState;

        public Gui.Root Gui;
        private QueuedAnnouncement SleepPrompt = null;

        public Action<String> ShowTooltip = null;
        public Action<String> ShowInfo = null;
        public Action<String> ShowToolPopup = null;
        
        public Action<Gui.MousePointer> SetMouse = null;
        public Action<String, int> SetMouseOverlay = null;
        public Gui.MousePointer MousePointer = new Gui.MousePointer("mouse", 1, 0);
        
        public bool IsMouseOverGui
        {
            get
            {
                return Gui.HoverItem != null;
                // Don't detect tooltips and tool popups.
            }
        }

        // event that is called when the world is done loading
        public delegate void OnLoaded();
        public event OnLoaded OnLoadedEvent;

        // event that is called when the player loses in the world
        public delegate void OnLose();
        public event OnLose OnLoseEvent;

        // Lazy actions - needed occasionally to spawn entities from threads among other things.
        private static List<Action> LazyActions = new List<Action>();

        public static void DoLazy(Action action)
        {
            LazyActions.Add(action);
        }
        
        public class WorldPopup
        {
            public Widget Widget;
            public Body BodyToTrack;
            public Vector2 ScreenOffset;

            public void Update(DwarfTime time, Camera camera, Viewport viewport)
            {
                if (Widget == null || BodyToTrack == null || BodyToTrack.IsDead)
                {
                    return;
                }

                var projectedPosition = viewport.Project(BodyToTrack.Position, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
                if (projectedPosition.Z > 0.999f)
                {
                    Widget.Hidden = true;
                    return;
                }

                Vector2 projectedCenter = new Vector2(projectedPosition.X, projectedPosition.Y) + ScreenOffset - new Vector2(0, Widget.Rect.Height);
                if ((new Vector2(Widget.Rect.Center.X, Widget.Rect.Center.Y) - projectedCenter).Length() < 0.1f)
                {
                    return;
                }

                Widget.Rect = new Rectangle((int)projectedCenter.X - Widget.Rect.Width / 2, 
                    (int)projectedCenter.Y - Widget.Rect.Height / 2, Widget.Rect.Width, Widget.Rect.Height);

                if (!viewport.Bounds.Intersects(Widget.Rect))
                {
                    Widget.Hidden = true;
                }
                else
                {
                    Widget.Hidden = false;
                }
                Widget.Invalidate();
            }
        }

        private Dictionary<uint, WorldPopup> LastWorldPopup = new Dictionary<uint, WorldPopup>();
        private Splasher Splasher;
        #endregion

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="Game">The program currently running</param>
        public WorldManager(DwarfGame Game)
        {
            InitialEmbark = EmbarkmentLibrary.DefaultEmbarkment;
            this.Game = Game;
            Content = Game.Content;
            Seed = MathFunctions.Random.Next();
            WorldOrigin = WorldGenerationOrigin;
            Time = new WorldTime();
        }

        public void PauseThreads()
        {
            ChunkManager.PauseThreads = true;
        }

        public void UnpauseThreads()
        {
            if (ChunkManager != null)
            {
                ChunkManager.PauseThreads = false;
            }

            if (Camera != null)
                Camera.LastWheel = Mouse.GetState().ScrollWheelValue;
        }

        public float SeaLevel { get; set; }

        /// <summary>
        /// Creates a screenshot of the game and saves it to a file.
        /// </summary>
        /// <param name="filename">The file to save the screenshot to</param>
        /// <param name="resolution">The width/height of the image</param>
        /// <returns>True if the screenshot could be taken, false otherwise</returns>
        public bool TakeScreenshot(string filename, Point resolution)
        {
            try
            {
                using (
                    RenderTarget2D renderTarget = new RenderTarget2D(GraphicsDevice, resolution.X, resolution.Y, false,
                        SurfaceFormat.Color, DepthFormat.Depth24))
                {
                    var frustum = Camera.GetDrawFrustum();
                    var renderables = ComponentManager.GetRenderables()
                        .Where(r => r.IsVisible && !ChunkManager.IsAboveCullPlane(r.GetBoundingBox()))
                        .Where(r => frustum.Intersects(r.GetBoundingBox()));
                    //var renderables = EnumerateIntersectingObjects(Camera.GetDrawFrustum())
                    //    .Where(r => r.IsVisible && !ChunkManager.IsAboveCullPlane(r.GetBoundingBox()))
                    //    .Where(c => Object.ReferenceEquals(c.Parent, ComponentManager.RootComponent) && c.IsVisible)
                    //    .SelectMany(c => c.EnumerateAll())
                    //    .OfType<IRenderableComponent>();


                    var oldProjection = Camera.ProjectionMatrix;
                    Matrix projectionMatrix = Matrix.CreatePerspectiveFieldOfView(Camera.FOV, ((float)resolution.X) / resolution.Y, Camera.NearPlane, Camera.FarPlane);
                    Camera.ProjectionMatrix = projectionMatrix;
                    GraphicsDevice.SetRenderTarget(renderTarget);
                    DrawSky(new DwarfTime(), Camera.ViewMatrix, 1.0f, Color.CornflowerBlue);
                    Draw3DThings(new DwarfTime(), DefaultShader, Camera.ViewMatrix);

                    DefaultShader.View = Camera.ViewMatrix;
                    DefaultShader.Projection = Camera.ProjectionMatrix;

                    ComponentRenderer.Render(renderables, new DwarfTime(), ChunkManager, Camera,
                        DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader,
                        ComponentRenderer.WaterRenderType.None, 0);
                    InstanceRenderer.Flush(GraphicsDevice, DefaultShader, Camera,
                        InstanceRenderMode.Normal);


                    GraphicsDevice.SetRenderTarget(null);
                    renderTarget.SaveAsPng(new FileStream(filename, FileMode.Create), resolution.X, resolution.Y);
                    GraphicsDevice.Textures[0] = null;
                    GraphicsDevice.Indices = null;
                    GraphicsDevice.SetVertexBuffer(null);
                    Camera.ProjectionMatrix = oldProjection;
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        public bool IsCameraUnderwater()
        {
            var handle = new VoxelHandle(ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(Camera.Position));
            return handle.IsValid && handle.WaterCell.WaterLevel > 0;
        }

        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public void Update(DwarfTime gameTime)
        {
            foreach (var func in LazyActions)
            {
                if (func != null)
                    func.Invoke();
            }
            LazyActions.Clear();

            if (FastForwardToDay)
            {
                if (Time.IsDay())
                {
                    FastForwardToDay = false;
                    foreach (CreatureAI minion in Master.Faction.Minions)
                    {
                        minion.Status.Energy.CurrentValue = minion.Status.Energy.MaxValue;
                    }
                    //Master.ToolBar.SpeedButton.SetSpeed(1);
                    Time.Speed = 100;
                }
                else
                {
                    //Master.ToolBar.SpeedButton.SetSpecialSpeed(3);
                    Time.Speed = 1000;
                }
            }
            //ParticleManager.Trigger("feather", CursorLightPos + Vector3.Up, Color.White, 1);

            FillClosestLights(gameTime);
            IndicatorManager.Update(gameTime);
            AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            Camera.AspectRatio = AspectRatio;

            Camera.Update(gameTime, ChunkManager);
            HandleAmbientSound();

            Master.Update(Game, gameTime);
            GoalManager.Update(this);
            Time.Update(gameTime);
            if (LastWorldPopup != null)
            {
                List<uint> removals = new List<uint>();
                foreach (var popup in LastWorldPopup)
                {
                    popup.Value.Update(gameTime, Camera, GraphicsDevice.Viewport);
                    if (popup.Value.Widget == null || !Gui.RootItem.Children.Contains(popup.Value.Widget) 
                        || popup.Value.BodyToTrack == null || popup.Value.BodyToTrack.IsDead)
                    {
                        removals.Add(popup.Key);
                    }
                }

                foreach (var removal in removals)
                {
                    if (LastWorldPopup[removal].Widget != null && Gui.RootItem.Children.Contains(LastWorldPopup[removal].Widget))
                    {
                        Gui.DestroyWidget(LastWorldPopup[removal].Widget);
                    }
                    LastWorldPopup.Remove(removal);
                }
            }

            if (Paused)
            {
                ComponentManager.UpdatePaused();
                TutorialManager.Update(Gui);
            }
            // If not paused, we want to just update the rest of the game.
            else
            {
                ParticleManager.Update(gameTime, this);
                TutorialManager.Update(Gui);

                Diplomacy.Update(gameTime, Time.CurrentDate, this);
                Factions.Update(gameTime);
                ComponentManager.Update(gameTime, ChunkManager, Camera);
                Sky.TimeOfDay = Time.GetSkyLightness();
                Sky.CosTime = (float)(Time.GetTotalHours() * 2 * Math.PI / 24.0f);
                DefaultShader.TimeOfDay = Sky.TimeOfDay;
                MonsterSpawner.Update(gameTime);
                bool allAsleep = Master.AreAllEmployeesAsleep();

#if !UPTIME_TEST
                if (SleepPrompt == null && allAsleep && !FastForwardToDay && Time.IsNight())
                {
                    SleepPrompt = new QueuedAnnouncement()
                    {
                        Text = "All your employees are asleep. Click here to skip to day.",
                        ClickAction = (sender, args) =>
                        {
                            FastForwardToDay = true;
                            SleepPrompt = null;
                        },
                        ShouldKeep = () =>
                        {
                            return FastForwardToDay == false && Time.IsNight() && Master.AreAllEmployeesAsleep();
                        }
                    };
                    MakeAnnouncement(SleepPrompt);
                }
                else if (!allAsleep)
                {
                    Time.Speed = 100;
                    FastForwardToDay = false;
                    SleepPrompt = null;
                }
#endif
            }

            // These things are updated even when the game is paused

            Splasher.Splash(gameTime, ChunkManager.Water.GetSplashQueue());
            Splasher.HandleTransfers(gameTime, ChunkManager.Water.GetTransferQueue());

            ChunkManager.Update(gameTime, Camera, GraphicsDevice);
            ChunkRenderer.Update(gameTime, Camera, GraphicsDevice);
            SoundManager.Update(gameTime, Camera, Time);
            Weather.Update(this.Time.CurrentDate, this);

            if (gameFile != null)
            {
                // Cleanup game file.
                gameFile = null;
            }

        }

        public bool FastForwardToDay { get; set; }
        public Embarkment InitialEmbark { get; set; }

        public void Quit()
        {
            Game.Graphics.PreparingDeviceSettings -= GraphicsPreparingDeviceSettings;

            ChunkManager.Destroy();
            ComponentManager = null;

            Master.Destroy();
            Master = null;

            ChunkManager = null;
            ChunkGenerator = null;
            GC.Collect();
            PlanService.Die();
        }

        public delegate void SaveCallback(bool success, Exception e);

        public void Save(string filename, WorldManager.SaveCallback callback = null)
        {
            Paused = true;
            WaitState waitforsave = new WaitState(Game, "Saving...", gameState.StateManager,
                () => SaveThreadRoutine(filename));
            if (callback != null)
                waitforsave.OnFinished += (bool b, WaitStateException e) => callback(b, e);
            gameState.StateManager.PushState(waitforsave);
        }

        private bool SaveThreadRoutine(string filename)
        {
#if !DEBUG
            try
            {
#endif
                System.Threading.Thread.CurrentThread.Name = "Save";
                // Ensure we're using the invariant culture.
                System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                DirectoryInfo worldDirectory =
                    Directory.CreateDirectory(DwarfGame.GetWorldDirectory() +
                                              Path.DirectorySeparatorChar + Overworld.Name);

                // This is a hack. Why does the overworld have this as a static field??
                Overworld.NativeFactions = this.Natives;
                NewOverworldFile file = new NewOverworldFile(Game.GraphicsDevice, Overworld.Map, Overworld.Name, SeaLevel);
                file.WriteFile(worldDirectory.FullName);

                try
                {
                    file.SaveScreenshot(worldDirectory.FullName + Path.DirectorySeparatorChar + "screenshot.png");
                }
                catch(Exception exception)
                {
                    Console.Error.WriteLine(exception.ToString());
                }

                gameFile = SaveGame.CreateFromWorld(this);
            var path = DwarfGame.GetSaveDirectory() + Path.DirectorySeparatorChar +
                filename;
                SaveGame.DeleteOldestSave(path, GameSettings.Default.MaxSaves, "Autosave");
                gameFile.WriteFile(path);
                ComponentManager.CleanupSaveData();

                lock (ScreenshotLock)
                {
                    Screenshots.Add(new Screenshot()
                    {
                        FileName = DwarfGame.GetSaveDirectory() +
                                   Path.DirectorySeparatorChar + filename + Path.DirectorySeparatorChar +
                                   "screenshot.png",
                        Resolution = new Point(128, 128)
                    });
                }
#if !DEBUG
            }
            catch (Exception exception)
            {
                Console.Error.Write(exception.ToString());
                Game.CaptureException(exception);
                throw new WaitStateException(exception.Message);
            }
#endif
                return true;
        }

        /// <summary>
        /// Reflects a camera beneath a water surface for reflection drawing TODO: move to water manager
        /// </summary>
        /// <param name="waterHeight">The height of the water (Y)</param>
        /// <returns>A reflection matrix</returns>
        public Matrix GetReflectedCameraMatrix(float waterHeight)
        {
            Vector3 reflCameraPosition = Camera.Position;
            reflCameraPosition.Y = -Camera.Position.Y + waterHeight * 2;
            Vector3 reflTargetPos = Camera.Target;
            reflTargetPos.Y = -Camera.Target.Y + waterHeight * 2;

            Vector3 cameraRight = Vector3.Cross(Camera.Target - Camera.Position, Camera.UpVector);
            cameraRight.Normalize();
            Vector3 invUpVector = Vector3.Cross(cameraRight, reflTargetPos - reflCameraPosition);
            invUpVector.Normalize();
            return Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);
        }

        /// <summary>
        /// Draws all the 3D terrain and entities
        /// </summary>
        /// <param name="gameTime">The current time</param>
        /// <param name="effect">The textured shader</param>
        /// <param name="view">The view matrix of the camera</param> 
        public void Draw3DThings(DwarfTime gameTime, Shader effect, Matrix view)
        {
            Matrix viewMatrix = Camera.ViewMatrix;
            Camera.ViewMatrix = view;

            GraphicsDevice.SamplerStates[0] = Drawer2D.PointMagLinearMin;
            effect.View = view;
            effect.Projection = Camera.ProjectionMatrix;
            effect.SetTexturedTechnique();
            effect.ClippingEnabled = true;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;

                ChunkRenderer.Render(Camera, gameTime, GraphicsDevice, effect, Matrix.Identity);

            Camera.ViewMatrix = viewMatrix;
            effect.ClippingEnabled = true;
        }

        /// <summary>
        /// Draws the sky box
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="view">The camera view matrix</param>
        /// <param name="scale">The scale for the sky drawing</param>
        /// <param name="fogColor"></param>
        /// <param name="drawBackground"></param>
        public void DrawSky(DwarfTime time, Matrix view, float scale, Color fogColor, bool drawBackground = true)
        {
            Matrix oldView = Camera.ViewMatrix;
            Camera.ViewMatrix = view;
            Sky.Render(time, GraphicsDevice, Camera, scale ,fogColor, ChunkManager.Bounds, drawBackground);
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Camera.ViewMatrix = oldView;
        }

        public void RenderUninitialized(DwarfTime gameTime, String tip = null)
        {
            Render(gameTime);
        }

        public void FillClosestLights(DwarfTime time)
        {
            List<Vector3> positions = (from light in DynamicLight.Lights select light.Position).ToList();
            positions.AddRange((from light in DynamicLight.TempLights select light.Position));
            positions.Sort((a, b) =>
            {
                float dA = (a - Camera.Position).LengthSquared();
                float dB = (b - Camera.Position).LengthSquared();
                return dA.CompareTo(dB);
            });

            if (!GameSettings.Default.CursorLightEnabled)
            {
                LightPositions[0] = new Vector3(-99999, -99999, -99999);
            }
            else
            {
                LightPositions[0] = CursorLightPos;
            }

            int numLights = GameSettings.Default.CursorLightEnabled ? Math.Min(16, positions.Count + 1) : Math.Min(16, positions.Count);
            for (int i = GameSettings.Default.CursorLightEnabled ? 1 : 0; i < numLights; i++)
            {
                if (i > positions.Count)
                {
                    LightPositions[i] = new Vector3(-99999, -99999, -99999);
                }
                else
                {
                    LightPositions[i] = GameSettings.Default.CursorLightEnabled ? positions[i - 1] : positions[i];
                }
            }

            for (int j = numLights; j < 16; j++)
            {
                LightPositions[j] = new Vector3(0, 0, 0);
            }
            DefaultShader.CurrentNumLights = Math.Max(Math.Min(GameSettings.Default.CursorLightEnabled ? numLights - 1 : numLights, 15), 0);
            DynamicLight.TempLights.Clear();
        }

        /// <summary>
        /// Called when a frame is to be drawn to the screen
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public void Render(DwarfTime gameTime)
        {
            if (!ShowingWorld)
                return;

            var frustum = Camera.GetDrawFrustum();
            var renderables = ComponentManager.GetRenderables()
                .Where(r => r.IsVisible && !ChunkManager.IsAboveCullPlane(r.GetBoundingBox()))
                .Where(r => frustum.Intersects(r.GetBoundingBox()));
            //var renderables = EnumerateIntersectingObjects(Camera.GetDrawFrustum())
            //    .Where(r => r.IsVisible && !ChunkManager.IsAboveCullPlane(r.GetBoundingBox()))
            //    .Where(c => Object.ReferenceEquals(c.Parent, ComponentManager.RootComponent) && c.IsVisible)
            //    .SelectMany(c => c.EnumerateAll())
            //    .OfType<IRenderableComponent>();

            // Controls the sky fog
            float x = (1.0f - Sky.TimeOfDay);
            x = x * x;
            DefaultShader.FogColor = new Color(0.32f * x, 0.58f * x, 0.9f * x);
            DefaultShader.LightPositions = LightPositions;

            CompositeLibrary.Render(GraphicsDevice);
            CompositeLibrary.Update();

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            // Computes the water height.
            float wHeight = WaterRenderer.GetVisibleWaterHeight(ChunkManager, Camera, GraphicsDevice.Viewport,
                lastWaterHeight);
            lastWaterHeight = wHeight;

            // Draw reflection/refraction images
            WaterRenderer.DrawReflectionMap(renderables, gameTime, this, wHeight - 0.1f, 
                GetReflectedCameraMatrix(wHeight),
                DefaultShader, GraphicsDevice);


#region Draw Selection Buffer.

            if (SelectionBuffer == null)
                SelectionBuffer = new SelectionBuffer(8, GraphicsDevice);

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            Plane slicePlane = WaterRenderer.CreatePlane(SlicePlane, new Vector3(0, -1, 0), Camera.ViewMatrix, false);

            if (SelectionBuffer.Begin(GraphicsDevice))
            {
                // Draw the whole world, and make sure to handle slicing
                DefaultShader.ClipPlane = new Vector4(slicePlane.Normal, slicePlane.D);
                DefaultShader.ClippingEnabled = true;
                DefaultShader.View = Camera.ViewMatrix;
                DefaultShader.Projection = Camera.ProjectionMatrix;
                DefaultShader.World = Matrix.Identity;

                //GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer - Chunks");
                ChunkRenderer.RenderSelectionBuffer(DefaultShader, GraphicsDevice, Camera.ViewMatrix);
                //GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer - Chunks");

                //GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer - Components");
                ComponentRenderer.RenderSelectionBuffer(renderables, gameTime, ChunkManager, Camera,
                    DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader);
                //GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer - Components");

                //GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer - Instances");
                InstanceRenderer.Flush(GraphicsDevice, DefaultShader, Camera,
                    InstanceRenderMode.SelectionBuffer);
                //GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer - Instances");

                SelectionBuffer.End(GraphicsDevice);
            }


#endregion



            // Start drawing the bloom effect
            if (GameSettings.Default.EnableGlow)
            {
                bloom.BeginDraw();
            }

            // Draw the sky
            GraphicsDevice.Clear(DefaultShader.FogColor);
            DrawSky(gameTime, Camera.ViewMatrix, 1.0f, DefaultShader.FogColor);

            // Defines the current slice for the GPU
            float level = ChunkManager.World.Master.MaxViewingLevel + 0.25f;
            if (level > VoxelConstants.ChunkSizeY)
            {
                level = 1000;
            }

            SlicePlane = level;

            DefaultShader.WindDirection = Weather.CurrentWind;
            DefaultShader.WindForce = 0.0005f * (1.0f + (float)Math.Sin(Time.GetTotalSeconds()*0.001f));
            // Draw the whole world, and make sure to handle slicing
            DefaultShader.ClipPlane = new Vector4(slicePlane.Normal, slicePlane.D);
            DefaultShader.ClippingEnabled = true;
            //Blue ghost effect above the current slice.
            DefaultShader.GhostClippingEnabled = true;
            Draw3DThings(gameTime, DefaultShader, Camera.ViewMatrix);

            // Now we want to draw the water on top of everything else
            DefaultShader.ClippingEnabled = true;
            DefaultShader.GhostClippingEnabled = false;

            //ComponentManager.CollisionManager.DebugDraw();

            DefaultShader.View = Camera.ViewMatrix;
            DefaultShader.Projection = Camera.ProjectionMatrix;
            DefaultShader.GhostClippingEnabled = true;
            // Now draw all of the entities in the game
            DefaultShader.ClipPlane = new Vector4(slicePlane.Normal, slicePlane.D);
            DefaultShader.ClippingEnabled = true;

            if (Debugger.Switches.DrawOcttree)
                foreach (var box in OctTree.EnumerateBounds())
                    Drawer3D.DrawBox(box.Item2, Color.Yellow, 1.0f / (float)(box.Item1 + 1), false);

            // Render simple geometry (boxes, etc.)
            Drawer3D.Render(GraphicsDevice, DefaultShader, Camera, DesignationDrawer, PlayerFaction.Designations, this);

            DefaultShader.EnableShadows = false;

            DefaultShader.View = Camera.ViewMatrix;

            ComponentRenderer.Render(renderables, gameTime, ChunkManager,
                Camera,
                DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader,
                ComponentRenderer.WaterRenderType.None, lastWaterHeight);
            InstanceRenderer.Flush(GraphicsDevice, DefaultShader, Camera, InstanceRenderMode.Normal);

            if (Master.CurrentToolMode == GameMaster.ToolMode.BuildZone
                || Master.CurrentToolMode == GameMaster.ToolMode.BuildWall ||
                Master.CurrentToolMode == GameMaster.ToolMode.BuildObject)
            {
                DefaultShader.View = Camera.ViewMatrix;
                DefaultShader.Projection = Camera.ProjectionMatrix;
                DefaultShader.SetTexturedTechnique();
                GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            }

            WaterRenderer.DrawWater(
                GraphicsDevice,
                (float)gameTime.TotalGameTime.TotalSeconds,
                DefaultShader,
                Camera.ViewMatrix,
                GetReflectedCameraMatrix(wHeight),
                Camera.ProjectionMatrix,
                new Vector3(0.1f, 0.0f, 0.1f),
                Camera,
                ChunkManager);
            ParticleManager.Render(this, GraphicsDevice);
            DefaultShader.ClippingEnabled = false;

            if (GameSettings.Default.EnableGlow)
            {
                bloom.DrawTarget = UseFXAA ? fxaa.RenderTarget : null;

                if (UseFXAA)
                {
                    fxaa.Begin(DwarfTime.LastTime);
                }
                bloom.Draw(gameTime.ToRealTime());
                if (UseFXAA)
                    fxaa.End(DwarfTime.LastTime);
            }
            else if (UseFXAA)
            {
                fxaa.End(DwarfTime.LastTime);
            }

            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };


            if (Debugger.Switches.DrawSelectionBuffer)
                SelectionBuffer.DebugDraw(GraphicsDevice.Viewport.Bounds);

            try
            {
                DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Drawer2D.PointMagLinearMin,
                    null, rasterizerState, null, Matrix.Identity);
                //DwarfGame.SpriteBatch.Draw(Shadows.ShadowTexture, Vector2.Zero, Color.White);
                if (IsCameraUnderwater())
                {
                    Drawer2D.FillRect(DwarfGame.SpriteBatch, GraphicsDevice.Viewport.Bounds, new Color(10, 40, 60, 200));
                }

                Drawer2D.Render(DwarfGame.SpriteBatch, Camera, GraphicsDevice.Viewport);

                IndicatorManager.Render(gameTime);
            }
            finally
            {
                DwarfGame.SpriteBatch.End();
            }

            if (Debugger.Switches.DrawComposites)
            {
                Vector2 offset = Vector2.Zero;
                foreach (var composite in CompositeLibrary.Composites)
                {
                    offset = composite.Value.DebugDraw(DwarfGame.SpriteBatch, (int)offset.X, (int)offset.Y);
                }
            }
            

            Master.Render(Game, gameTime, GraphicsDevice);

            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle =
                DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;


            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            lock (ScreenshotLock)
            {
                foreach (Screenshot shot in Screenshots)
                {
                    TakeScreenshot(shot.FileName, shot.Resolution);
                }

                Screenshots.Clear();
            }
        }


        private void GraphicsDeviceReset(object sender, EventArgs e)
        {
            ResetGraphics();
        }

        private void ResetGraphics()
        {
            /*
            if (bloom != null)
            {
                bloom.sceneRenderTarget = new RenderTarget2D(GraphicsDevice, Game.Graphics.PreferredBackBufferWidth, Game.Graphics.PreferredBackBufferHeight,
                    false, Game.Graphics.PreferredBackBufferFormat, Game.Graphics.PreferredDepthStencilFormat, MultiSamples,
                    RenderTargetUsage.DiscardContents);
            }

            foreach (var composite in CompositeLibrary.Composites)
            {
                composite.Value.Initialize();
                composite.Value.HasChanged = true;
            }

            if (WaterRenderer != null)
            {
                WaterRenderer = new WaterRenderer(GraphicsDevice);
            }
            
            AssetManager.ResetCache();
            DwarfGame.SpriteBatch = new SpriteBatch(Game.GraphicsDevice);

            Color[] white = new Color[1];
            white[0] = Color.White;
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(white);

            Tilesheet = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
            AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            DefaultShader = new Shader(Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders), true);
            DefaultShader.ScreenWidth = GraphicsDevice.Viewport.Width;
            DefaultShader.ScreenHeight = GraphicsDevice.Viewport.Height;
            PrimitiveLibrary.Reinitialize(GraphicsDevice, Content);
            */
        }

        /// <summary>
        /// Called when the GPU is getting new settings
        /// </summary>
        /// <param name="sender">The object requesting new device settings</param>
        /// <param name="e">The device settings that are getting set</param>
        private void GraphicsPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            if (e == null)
            {
                Console.Error.WriteLine("Preparing device settings given null event args.");
                return;
            }
            
            if (e.GraphicsDeviceInformation == null)
            {
                Console.Error.WriteLine("Somehow, GraphicsDeviceInformation is null!");
                return;
            }

            PresentationParameters pp = e.GraphicsDeviceInformation.PresentationParameters;
            if (pp == null)
            {
                Console.Error.WriteLine("Presentation parameters invalid.");
                return;
            }

            GraphicsAdapter adapter = e.GraphicsDeviceInformation.Adapter;
            if (adapter == null)
            {
                Console.Error.WriteLine("Somehow, graphics adapter is null!");
                return;
            }

            if (adapter.CurrentDisplayMode == null)
            {
                Console.Error.WriteLine("Somehow, CurrentDisplayMode is null!");
                return;
            }

            SurfaceFormat format = adapter.CurrentDisplayMode.Format;

            if (MultiSamples > 0 && MultiSamples != pp.MultiSampleCount)
            {
                pp.MultiSampleCount = MultiSamples;
            }
            else if (MultiSamples <= 0 && MultiSamples != pp.MultiSampleCount)
            {
                pp.MultiSampleCount = 0;
            }

            if (GraphicsDevice != null)
            {
                ResetGraphics();
            }
        }

        public void Dispose()
        {
            Tilesheet.Dispose();
            pixel.Dispose();
            bloom.Dispose();
            foreach(var composite in CompositeLibrary.Composites)
            {
                composite.Value.Dispose();
            }
            WaterRenderer.Dispose();
            CompositeLibrary.Composites.Clear();
        }

        public void InvokeLoss()
        {
            OnLoseEvent();
        }

        private string[] prevAmbience = { null, null };
        private Timer AmbienceTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
        private bool firstAmbience = true;
        private void PlaySpecialAmbient(string sound)
        {
            if (prevAmbience[0] != sound)
            {
                SoundManager.PlayAmbience(sound);

                if (!string.IsNullOrEmpty(prevAmbience[0]) && prevAmbience[0] != sound)
                    SoundManager.StopAmbience(prevAmbience[0]);
                if (!string.IsNullOrEmpty(prevAmbience[1]) && prevAmbience[1] != sound)
                    SoundManager.StopAmbience(prevAmbience[1]);

                prevAmbience[0] = sound;
                prevAmbience[1] = sound;
            }
        }

        public void HandleAmbientSound()
        {
            AmbienceTimer.Update(DwarfTime.LastTime);
            if (!AmbienceTimer.HasTriggered && !firstAmbience)
            {
                return;
            }
            firstAmbience = false;

            // Before doing anything, determine if there is a rain or snow storm.
            if (Weather.IsRaining())
            {
                PlaySpecialAmbient("sfx_amb_rain_storm");
                return;
            }

            if (Weather.IsSnowing())
            {
                PlaySpecialAmbient("sfx_amb_snow_storm");
                return;
            }

            // First check voxels to see if we're underground or underwater.
            var vox = VoxelHelpers.FindFirstVisibleVoxelOnScreenRay(ChunkManager.ChunkData, GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2, Camera, GraphicsDevice.Viewport, 100.0f, false, null);

            if (vox.IsValid)
            {
                float height = WaterRenderer.GetTotalWaterHeightCells(vox);
                if (height > 0)
                {
                    PlaySpecialAmbient("sfx_amb_ocean");
                    return;
                }
                else
                {
                    // Unexplored voxels assumed to be cave.
                    if (vox.IsValid && !vox.IsExplored)
                    {
                        PlaySpecialAmbient("sfx_amb_cave");
                        return;
                    }

                    var above = VoxelHelpers.GetVoxelAbove(vox);
                    // Underground, do the cave test.
                    if (above.IsValid && above.IsEmpty && above.Sunlight == false)
                    {
                        PlaySpecialAmbient("sfx_amb_cave");
                        return;
                    }

                }
                
            }
            else
            {
                return;
            }

            // Now check for biome ambience.
            var pos = vox.WorldPosition;
            var biome = Overworld.GetBiomeAt(pos, WorldScale, WorldOrigin);

            if (!string.IsNullOrEmpty(biome.DayAmbience))
            {
                if (prevAmbience[0] != biome.DayAmbience)
                {
                    if (!string.IsNullOrEmpty(prevAmbience[0]))
                    {
                        SoundManager.StopAmbience(prevAmbience[0]);
                        prevAmbience[0] = null;
                    }
                    if (!string.IsNullOrEmpty(prevAmbience[1]))
                    {
                        SoundManager.StopAmbience(prevAmbience[1]);
                        prevAmbience[1] = null;
                    }
                    SoundManager.PlayAmbience(biome.DayAmbience);
                }

                prevAmbience[0] = biome.DayAmbience;
            }

            if (!string.IsNullOrEmpty(biome.NightAmbience) && prevAmbience[1] != biome.NightAmbience)
            {
                prevAmbience[1] = biome.NightAmbience;

                SoundManager.PlayAmbience(biome.NightAmbience);
            }
        }

        public WorldPopup MakeWorldPopup(string text, Body body, float screenOffset = -10, float time = 30.0f)
        {
            return MakeWorldPopup(new TimedIndicatorWidget() { Text = text, DeathTimer = new Timer(time, true, Timer.TimerMode.Real) }, body, new Vector2(0, screenOffset));
        }

        public WorldPopup MakeWorldPopup(Widget widget, Body body, Vector2 ScreenOffset)
        {
            if (LastWorldPopup.ContainsKey(body.GlobalID))
            {
                Gui.DestroyWidget(LastWorldPopup[body.GlobalID].Widget);
            }
            Gui.RootItem.AddChild(widget);
            LastWorldPopup[body.GlobalID] = new WorldPopup()
            {
                Widget = widget,
                BodyToTrack = body,
                ScreenOffset = ScreenOffset 
            };
            return LastWorldPopup[body.GlobalID];
        }
    }
}
