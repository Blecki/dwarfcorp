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
        public Vector3 CursorLightPos
        {
            get { return LightPositions[0]; }
            set { LightPositions[0] = value; }
        }

        public Vector3[] LightPositions = new Vector3[16];

        // When true, the minimap will be drawn.
        public bool DrawMap = true;

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
        public CollisionManager CollisionManager = null;
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
        public InstanceManager InstanceManager;

        // Handles loading of game assets
        public ContentManager Content;

        // Reference to XNA Game
        public DwarfGame Game;

        // Interfaces with the graphics card
        public GraphicsDevice GraphicsDevice;

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

        private GameFile gameFile;

        public Point3 WorldSize { get; set; }

        // More statics. Hate this.
        public Action<String, Action> OnAnnouncement;

        public void MakeAnnouncement(String Message, Action ClickAction = null, string sound = null)
        {
            if (OnAnnouncement != null)
                OnAnnouncement(Message, ClickAction);

            if (!string.IsNullOrEmpty(sound))
                SoundManager.PlaySound(sound, 0.15f);
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

        public Economy PlayerEconomy
        {
            get { return Master.Faction.Economy; }
        }

        public List<Faction> Natives { get; set; }

        private bool SleepPrompt = false;

        public CraftLibrary CraftLibrary = null;

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
        private bool firstIter = true;
        #endregion

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="Game">The program currently running</param>
        public WorldManager(DwarfGame Game)
        {
            InitialEmbark = Embarkment.DefaultEmbarkment;
            this.Game = Game;
            Content = Game.Content;
            GraphicsDevice = Game.GraphicsDevice;
            Seed = MathFunctions.Random.Next();
            WorldOrigin = WorldGenerationOrigin;
            Time = new WorldTime();
        }

        public void Pause()
        {
            ChunkManager.PauseThreads = true;
        }

        public void Unpause()
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
                    GraphicsDevice.SetRenderTarget(renderTarget);
                    DrawSky(new DwarfTime(), Camera.ViewMatrix, 1.0f, Color.CornflowerBlue);
                    Draw3DThings(new DwarfTime(), DefaultShader, Camera.ViewMatrix);

                    DefaultShader.View = Camera.ViewMatrix;
                    InstanceManager.Render(GraphicsDevice, DefaultShader, Camera, true);
                    ComponentRenderer.Render(ComponentManager.GetRenderables(), new DwarfTime(), ChunkManager, Camera,
                        DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader,
                        ComponentRenderer.WaterRenderType.None, 0);

                    GraphicsDevice.SetRenderTarget(null);
                    renderTarget.SaveAsPng(new FileStream(filename, FileMode.Create), resolution.X, resolution.Y);
                    GraphicsDevice.Textures[0] = null;
                    GraphicsDevice.Indices = null;
                    GraphicsDevice.SetVertexBuffer(null);
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
            var handle = new TemporaryVoxelHandle(ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(Camera.Position));
            return handle.IsValid && handle.WaterCell.WaterLevel > 0;
        }
        
        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public void Update(DwarfTime gameTime)
        {
            firstIter = false;

            EntityFactory.DoLazyActions();
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

            //Drawer3D.DrawPlane(0, Camera.Position.X - 1500, Camera.Position.Z - 1500, Camera.Position.X + 1500, Camera.Position.Z + 1500, Color.Black);
            FillClosestLights(gameTime);
            IndicatorManager.Update(gameTime);
            AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            Camera.AspectRatio = AspectRatio;

            Camera.Update(gameTime, ChunkManager);

            if (KeyManager.RotationEnabled())
                Mouse.SetPosition(Game.GraphicsDevice.Viewport.Width / 2,
                    Game.GraphicsDevice.Viewport.Height / 2);

            Master.Update(Game, gameTime);
            GoalManager.Update(this);
            TutorialManager.Update(Gui);
            Time.Update(gameTime);


            // If not paused, we want to just update the rest of the game.
            if (!Paused)
            {
                //GamePerformance.Instance.StartTrackPerformance("Diplomacy");
                Diplomacy.Update(gameTime, Time.CurrentDate, this);
                //GamePerformance.Instance.StopTrackPerformance("Diplomacy");

                //GamePerformance.Instance.StartTrackPerformance("Components");
                Factions.Update(gameTime);
                ComponentManager.Update(gameTime, ChunkManager, Camera);
                //GamePerformance.Instance.StopTrackPerformance("Components");

                Sky.TimeOfDay = Time.GetSkyLightness();

                Sky.CosTime = (float)(Time.GetTotalHours() * 2 * Math.PI / 24.0f);
                DefaultShader.TimeOfDay = Sky.TimeOfDay;

                //GamePerformance.Instance.StartTrackPerformance("Monster Spawner");
                MonsterSpawner.Update(gameTime);
                //GamePerformance.Instance.StopTrackPerformance("Monster Spawner");

                //GamePerformance.Instance.StartTrackPerformance("All Asleep");
                bool allAsleep = Master.AreAllEmployeesAsleep();
                if (SleepPrompt && allAsleep && !FastForwardToDay && Time.IsNight())
                {
                    var sleepingPrompt = Gui.ConstructWidget(new Gui.Widgets.Confirm
                    {
                        Text = "All of your employees are asleep. Skip to daytime?",
                        OkayText = "Skip to Daytime",
                        CancelText = "Don't Skip",
                        OnClose = (sender) =>
                        {
                            if ((sender as Gui.Widgets.Confirm).DialogResult == DwarfCorp.Gui.Widgets.Confirm.Result.OKAY)
                                FastForwardToDay = true;
                        }
                    });
                    Gui.ShowModalPopup(sleepingPrompt);
                    SleepPrompt = false;
                }
                else if (!allAsleep)
                {
                    SleepPrompt = true;
                }
                //GamePerformance.Instance.StopTrackPerformance("All Asleep");
            }

            // These things are updated even when the game is paused

            //GamePerformance.Instance.StartTrackPerformance("Chunk Manager");
            ChunkManager.Update(gameTime, Camera, GraphicsDevice);
            ChunkRenderer.Update(gameTime, Camera, GraphicsDevice);
            //GamePerformance.Instance.StopTrackPerformance("Chunk Manager");

            //GamePerformance.Instance.StartTrackPerformance("Instance Manager");
            InstanceManager.Update(gameTime, Camera, GraphicsDevice);
            //GamePerformance.Instance.StopTrackPerformance("Instance Manager");

            //GamePerformance.Instance.StartTrackPerformance("Sound Manager");
            SoundManager.Update(gameTime, Camera, Time);
            //GamePerformance.Instance.StopTrackPerformance("Sound Manager");

            //GamePerformance.Instance.StartTrackPerformance("Weather");
            Weather.Update(this.Time.CurrentDate, this);
            //GamePerformance.Instance.StopTrackPerformance("Weather");

            // Make sure that the slice slider snaps to the current viewing level (an integer)
            //if(!LevelSlider.IsMouseOver)
            {
                //   LevelSlider.SliderValue = ChunkManager.ChunkData.MaxViewingLevel;
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

        public void Save(string filename, SaveCallback callback = null)
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
            //try
            {
                System.Threading.Thread.CurrentThread.Name = "Save";
                DirectoryInfo worldDirectory =
                    Directory.CreateDirectory(DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Worlds" +
                                              Path.DirectorySeparatorChar + Overworld.Name);

                // This is a hack. Why does the overworld have this as a static field??
                Overworld.NativeFactions = this.Natives;
                OverworldFile file = new OverworldFile(Game.GraphicsDevice, Overworld.Map, Overworld.Name, SeaLevel);
                file.WriteFile(
                    worldDirectory.FullName + Path.DirectorySeparatorChar + "world." + (DwarfGame.COMPRESSED_BINARY_SAVES ? OverworldFile.CompressedExtension : OverworldFile.Extension),
                    DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES);
                file.SaveScreenshot(worldDirectory.FullName + Path.DirectorySeparatorChar + "screenshot.png");

                gameFile = new GameFile(Overworld.Name, GameID, this);
                gameFile.WriteFile(
                    DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Saves" + Path.DirectorySeparatorChar +
                    filename, DwarfGame.COMPRESSED_BINARY_SAVES);
                ComponentManager.CleanupSaveData();

                lock (ScreenshotLock)
                {
                    Screenshots.Add(new Screenshot()
                    {
                        FileName = DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Saves" +
                                   Path.DirectorySeparatorChar + filename + Path.DirectorySeparatorChar +
                                   "screenshot.png",
                        Resolution = new Point(640, 480)
                    });
                }
            }
            //catch (Exception exception)
            {
                //throw new WaitStateException(exception.Message);
            }
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

            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            effect.View = view;
            effect.Projection = Camera.ProjectionMatrix;
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Textured];
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
                float dA = MathFunctions.L1(a, Camera.Position);
                float dB = MathFunctions.L1(b, Camera.Position);
                return dA.CompareTo(dB);
            });
            int numLights = Math.Min(16, positions.Count + 1);
            for (int i = 1; i < numLights; i++)
            {
                if (i > positions.Count)
                {
                    LightPositions[i] = new Vector3(0, 0, 0);
                }
                else
                {
                    LightPositions[i] = positions[i - 1];
                }
            }

            for (int j = numLights; j < 16; j++)
            {
                LightPositions[j] = new Vector3(0, 0, 0);
            }

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

            GamePerformance.Instance.StartTrackPerformance("Render - Prep");

            var renderables = ComponentRenderer.EnumerateVisibleRenderables(ComponentManager.GetRenderables(),
                ChunkManager,
                Camera);

            // Controls the sky fog
            float x = (1.0f - Sky.TimeOfDay);
            x = x * x;
            DefaultShader.FogColor = new Color(0.32f * x, 0.58f * x, 0.9f * x);
            DefaultShader.LightPositions = LightPositions;

            CompositeLibrary.Render(GraphicsDevice, DwarfGame.SpriteBatch);
            CompositeLibrary.Update();
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            if (GameSettings.Default.UseDynamicShadows)
            {
                ChunkRenderer.RenderShadowmap(DefaultShader, GraphicsDevice, Shadows, Matrix.Identity, Tilesheet);
            }

            if (GameSettings.Default.UseLightmaps)
            {
                ChunkRenderer.RenderLightmaps(Camera, gameTime, GraphicsDevice, DefaultShader, Matrix.Identity);
            }

            // Computes the water height.
            float wHeight = WaterRenderer.GetVisibleWaterHeight(ChunkManager, Camera, GraphicsDevice.Viewport,
                lastWaterHeight);
            lastWaterHeight = wHeight;

            // Draw reflection/refraction images
            WaterRenderer.DrawReflectionMap(renderables, gameTime, this, wHeight - 0.1f, 
                GetReflectedCameraMatrix(wHeight),
                DefaultShader, GraphicsDevice);

            GamePerformance.Instance.StopTrackPerformance("Render - Prep");
            GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer");

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

                GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer - Chunks");
                ChunkRenderer.RenderSelectionBuffer(DefaultShader, GraphicsDevice, Camera.ViewMatrix);
                GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer - Chunks");

                GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer - Components");
                ComponentRenderer.RenderSelectionBuffer(renderables, gameTime, ChunkManager, Camera,
                    DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader);
                GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer - Components");

                GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer - Instances");
                InstanceManager.RenderSelectionBuffer(GraphicsDevice, DefaultShader, Camera, false);
                GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer - Instances");

                SelectionBuffer.End(GraphicsDevice);
            }

            #endregion

            GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer");
            GamePerformance.Instance.StartTrackPerformance("Render - BG Stuff");


            // Start drawing the bloom effect
            if (GameSettings.Default.EnableGlow)
            {
                bloom.BeginDraw();
            }
            else if (UseFXAA)
            {
                fxaa.BeginDraw();
            }

            // Draw the sky
            GraphicsDevice.Clear(DefaultShader.FogColor);
            DrawSky(gameTime, Camera.ViewMatrix, 1.0f, DefaultShader.FogColor);

            // Defines the current slice for the GPU
            float level = ChunkManager.ChunkData.MaxViewingLevel + 2.0f;
            if (level > VoxelConstants.ChunkSizeY)
            {
                level = 1000;
            }

            GamePerformance.Instance.StopTrackPerformance("Render - BG Stuff");
            GamePerformance.Instance.StartTrackPerformance("Render - Chunks");



            SlicePlane = SlicePlane * 0.5f + level * 0.5f;

            DefaultShader.WindDirection = Weather.CurrentWind;
            DefaultShader.WindForce = 0.0005f * (1.0f + (float)Math.Sin(Time.GetTotalSeconds()*0.001f));
            // Draw the whole world, and make sure to handle slicing
            DefaultShader.ClipPlane = new Vector4(slicePlane.Normal, slicePlane.D);
            DefaultShader.ClippingEnabled = true;
            //Blue ghost effect above the current slice.
            DefaultShader.GhostClippingEnabled = true;
            Draw3DThings(gameTime, DefaultShader, Camera.ViewMatrix);

            GamePerformance.Instance.StopTrackPerformance("Render - Chunks");
            GamePerformance.Instance.StartTrackPerformance("Render - Components");


            // Now we want to draw the water on top of everything else
            DefaultShader.ClippingEnabled = true;
            DefaultShader.GhostClippingEnabled = false;

            //ComponentManager.CollisionManager.DebugDraw();

            DefaultShader.View = Camera.ViewMatrix;
            DefaultShader.Projection = Camera.ProjectionMatrix;
            // Render simple geometry (boxes, etc.)
            Drawer3D.Render(GraphicsDevice, DefaultShader, true);

            // Now draw all of the entities in the game
            DefaultShader.ClipPlane = new Vector4(slicePlane.Normal, slicePlane.D);
            DefaultShader.ClippingEnabled = true;
            DefaultShader.GhostClippingEnabled = true;
            DefaultShader.EnableShadows = GameSettings.Default.UseDynamicShadows;

            if (GameSettings.Default.UseDynamicShadows)
            {
                Shadows.BindShadowmapEffect(DefaultShader);
            }

            DefaultShader.View = Camera.ViewMatrix;
            InstanceManager.Render(GraphicsDevice, DefaultShader, Camera, true);
            ComponentRenderer.Render(renderables, gameTime, ChunkManager,
                Camera,
                DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader,
                ComponentRenderer.WaterRenderType.None, lastWaterHeight);

            GamePerformance.Instance.StopTrackPerformance("Render - Components");
            GamePerformance.Instance.StartTrackPerformance("Render - Tools");



            if (Master.CurrentToolMode == GameMaster.ToolMode.Build)
            {
                DefaultShader.View = Camera.ViewMatrix;
                DefaultShader.Projection = Camera.ProjectionMatrix;
                DefaultShader.CurrentTechnique = DefaultShader.Techniques[Shader.Technique.Textured];
                GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Master.Faction.WallBuilder.Render(gameTime, GraphicsDevice, DefaultShader);
                Master.Faction.CraftBuilder.Render(gameTime, GraphicsDevice, DefaultShader);
            }

            GamePerformance.Instance.StopTrackPerformance("Render - Tools");
            GamePerformance.Instance.StartTrackPerformance("Render - Water");


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

            GamePerformance.Instance.StopTrackPerformance("Render - Water");
            GamePerformance.Instance.StartTrackPerformance("Render - Misc");


            DefaultShader.ClippingEnabled = false;

            if (GameSettings.Default.EnableGlow)
            {
                bloom.DrawTarget = UseFXAA ? fxaa.RenderTarget : null;
                bloom.Draw(gameTime.ToGameTime());
                if (UseFXAA)
                    fxaa.End();
            }
            else if (UseFXAA)
            {
                fxaa.End();
            }

            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            
            //if (CompositeLibrary.Composites.ContainsKey("resources"))
            //    CompositeLibrary.Composites["resources"].DebugDraw(DwarfGame.SpriteBatch, 0, 0);
            //SelectionBuffer.DebugDraw(GraphicsDevice.Viewport.Bounds);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp,
                null, rasterizerState);
            //DwarfGame.SpriteBatch.Draw(Shadows.ShadowTexture, Vector2.Zero, Color.White);
            if (IsCameraUnderwater())
            {
                Drawer2D.FillRect(DwarfGame.SpriteBatch, GraphicsDevice.Viewport.Bounds, new Color(10, 40, 60, 200));
            }

            Drawer2D.Render(DwarfGame.SpriteBatch, Camera, GraphicsDevice.Viewport);

            IndicatorManager.Render(gameTime);
            DwarfGame.SpriteBatch.End();

            Master.Render(Game, gameTime, GraphicsDevice);

            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle =
                DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;


            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            GamePerformance.Instance.StopTrackPerformance("Render - Misc");


            lock (ScreenshotLock)
            {
                foreach (Screenshot shot in Screenshots)
                {
                    TakeScreenshot(shot.FileName, shot.Resolution);
                }

                Screenshots.Clear();
            }
        }


        /// <summary>
        /// Called when the GPU is getting new settings
        /// </summary>
        /// <param name="sender">The object requesting new device settings</param>
        /// <param name="e">The device settings that are getting set</param>
        private void GraphicsPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            PresentationParameters pp = e.GraphicsDeviceInformation.PresentationParameters;
            GraphicsAdapter adapter = e.GraphicsDeviceInformation.Adapter;
            SurfaceFormat format = adapter.CurrentDisplayMode.Format;

            if (MultiSamples > 0 && MultiSamples != pp.MultiSampleCount)
            {
                pp.MultiSampleCount = MultiSamples;
            }
            else if (MultiSamples <= 0 && MultiSamples != pp.MultiSampleCount)
            {
                pp.MultiSampleCount = 0;
            }

            if (bloom != null)
            {
                bloom.sceneRenderTarget = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight,
                    false,
                    format, pp.DepthStencilFormat, pp.MultiSampleCount,
                    RenderTargetUsage.DiscardContents);
            }
        }

        public void Dispose()
        {
            Tilesheet.Dispose();
            pixel.Dispose();
        }

        public void InvokeLoss()
        {
            OnLoseEvent();
        }
    }
}
