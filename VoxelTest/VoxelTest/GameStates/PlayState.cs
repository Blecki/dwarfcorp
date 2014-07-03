using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BloomPostprocess;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This is the main game state for actually playing the game.
    /// </summary>
    public class PlayState : GameState
    {
        #region fields
        // The random seed of the whole game
        public static int Seed { get; set; }

        // Defines the number of pixels in the overworld to number of voxels conversion
        public static float WorldScale
        {
            get { return GameSettings.Default.WorldScale; }
            set { GameSettings.Default.WorldScale = value; }
        }

        // The horizontal size of the overworld in pixels
        public static int WorldWidth
        {
            get { return GameSettings.Default.WorldWidth; }
            set { GameSettings.Default.WorldWidth = value; }
        }

        // The origin of the overworld in pixels [(0, 0, 0) in world space.]
        public static Vector2 WorldOrigin { get; set; }

        // The vertical size of the overworld in pixels
        public static int WorldHeight
        {
            get { return GameSettings.Default.WorldHeight; }
            set { GameSettings.Default.WorldHeight = value; }
        }

        // The number of voxels along x and z in a chunk
        public int ChunkWidth
        {
            get { return GameSettings.Default.ChunkWidth; }
            set { GameSettings.Default.ChunkWidth = value; }
        }

        // The number of voxels along y in a chunk.
        public int ChunkHeight
        {
            get { return GameSettings.Default.ChunkHeight; }
            set { GameSettings.Default.ChunkHeight = value; }
        }

        // The current coordinate of the cursor light
        public static Vector3 CursorLightPos = Vector3.Zero;

        // True when the game has begun loading. Set to false when the game is exited.
        public static bool HasStarted = false;

        // When true, the minimap will be drawn.
        public bool DrawMap = true;

        // The texture used for the terrain tiles.
        public Texture2D Tilesheet;

        // The shader used to draw the terrain and most entities
        public static Effect DefaultShader;

        // The player's view into the world.
        public static OrbitCamera Camera;

        // Gives the number of antialiasing multisamples. 0 means no AA. 
        public int MultiSamples
        {
            get { return GameSettings.Default.AntiAliasing; }
            set { GameSettings.Default.AntiAliasing = value; }
        }

        // The ratio of width to height in screen pixels. (ie 16/9 or 4/3)
        public static float AspectRatio = 0.0f;

        // Responsible for managing terrain
        public static ChunkManager ChunkManager = null;

        // Maps a set of voxel types to assets and properties
        public static VoxelLibrary VoxelLibrary = null;

        // Responsible for creating terrain
        public static ChunkGenerator ChunkGenerator = null;

        // Responsible for managing game entities
        public static ComponentManager ComponentManager = null;

        // Handles interfacing with the player and sending commands to dwarves
        public static GameMaster Master = null;

        // If the game was loaded from a file, this contains the name of that file.
        public string ExistingFile = "";

        // Draws and manages the user interface 
        public static DwarfGUI GUI = null;

        // Just a helpful 1x1 white pixel texture
        private Texture2D pixel;

        // Draws lines/boxes etc. to the screen
        private Drawer2D drawer2D;

        // A shader which draws fancy light blooming to the screen
        private BloomComponent bloom;

        // Responsible for drawing liquids.
        public static WaterRenderer WaterRenderer;

        // Responsible for drawing the skybox
        public static SkyRenderer Sky;

        // Used to generate all random numbers in the game.
        public static ThreadSafeRandom Random = new ThreadSafeRandom();

        // Responsible for handling instances of particular primitives (or models)
        // and drawing them to the screen
        public static InstanceManager InstanceManager;

        // Provides event-based keyboard and mouse input.
        public InputManager Input = new InputManager();

        // Handles loading of game assets
        public ContentManager Content;

        // Interfaces with the graphics card
        public GraphicsDevice GraphicsDevice;

        // Loads the game in the background while a loading message displays
        public Thread LoadingThread { get; set; }

        // When the game is loading, this message is displayed on the screen
        public string LoadingMessage = "";

        // True if the game's update loop is paused, false otherwise
        public static bool Paused { get; set; }

        // Handles a thread which constantly runs A* plans for whoever needs them.
        public static PlanService PlanService = null;

        // Maintains a dictionary of biomes (forest, desert, etc.)
        public static BiomeLibrary BiomeLibrary = new BiomeLibrary();

        // Handles the current game state (TODO: replace this with something more elegant)
        public GameCycle GameCycle { get; set; }

        // If true, the game will re-set itself when entered instead of just continuing
        public bool ShouldReset { get; set; }

        // Text displayed on the screen for the player's company
        public Label CompanyNameLabel { get; set; }

        // Text displayed on the screen for the player's logo
        public ImagePanel CompanyLogoPanel { get; set; }

        // Text displayed on the screen for the current amount of money the player has
        public Label MoneyLabel { get; set; }

        // Text displayed on the screen for the current game time
        public Label TimeLabel { get; set; }

        // The game is briefly simulated before starting so things have time to settle.
        public Timer PreSimulateTimer { get; set; }

        // Text displayed on the screen for the current slice
        public Label CurrentLevelLabel { get; set; }

        // When pressed, makes the current slice increase.
        public Button CurrentLevelUpButton { get; set; }

        //When pressed, makes the current slice decrease
        public Button CurrentLevelDownButton { get; set; }

        // When dragged, the current slice changes
        public Slider LevelSlider { get; set; }

        // Maintains a dictionary of particle emitters
        public static ParticleManager ParticleManager { get { return ComponentManager.ParticleManager; } set { ComponentManager.ParticleManager = value; } }

        // The current calendar date/time of th egame.
        public static WorldTime Time = new WorldTime();

        // Hacks to count frame rate TODO: Make a framerate counter class
        private uint frameCounter = 0;
        private readonly Timer frameTimer = new Timer(1.0f, false);
        
        // Hack to smooth water reflections TODO: Put into water manager
        private float lastWaterHeight = 8.0f;
        
        // Hack to bypass input manager TODO: replace with input manager
        private bool pausePressed = false;
        private bool bPressed = false;

        private readonly List<float> lastFps = new List<float>();
        private float fps = 0.0f;
        private GameFile gameFile;
        public Panel PausePanel;

        public Minimap MiniMap { get; set; }

        #endregion

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="game">The program currently running</param>
        /// <param name="stateManager">The game state manager this state will belong to</param>
        public PlayState(DwarfGame game, GameStateManager stateManager) :
            base(game, "PlayState", stateManager)
        {
            ShouldReset = true;
            Paused = false;
            Content = Game.Content;
            GraphicsDevice = Game.GraphicsDevice;
            Seed = Random.Next();
            RenderUnderneath = true;
            WorldOrigin = new Vector2(WorldWidth / 2, WorldHeight / 2);
            PreSimulateTimer = new Timer(3, false);
            Time = new WorldTime();
            
        }

        /// <summary>
        /// Called when the PlayState is entered from the state manager.
        /// </summary>
        public override void OnEnter()
        {
            // If the game should reset, we initialize everything
            if(ShouldReset)
            {
                PreSimulateTimer.Reset(3);
                ShouldReset = false;
                GameCycle = new GameCycle();
                GameCycle.OnCycleChanged += GameCycle_OnCycleChanged;
                Preload();
               
                Game.Graphics.PreferMultiSampling = GameSettings.Default.AntiAliasing > 1;

                // This is some grossness which tries to apply the current graphics settings
                // to the GPU.
                try
                {
                    Game.Graphics.ApplyChanges();
                }
                catch(NoSuitableGraphicsDeviceException exception)
                {
                    Console.Error.WriteLine(exception.Message);
                }

                Game.Graphics.PreparingDeviceSettings -= GraphicsPreparingDeviceSettings;
                Game.Graphics.PreparingDeviceSettings += GraphicsPreparingDeviceSettings;
                PlanService = new PlanService();
                
                LoadingThread = new Thread(Load);
                LoadingThread.Start();



                SoundManager.PlayMusic("dwarfcorp");


            }

            // Otherwise, we just unpause everything and re-enter the game.
            HasStarted = true;
            if(ChunkManager != null)
            {
                ChunkManager.PauseThreads = false;
            }
            base.OnEnter();
        }

        /// <summary>
        /// Called when the balloon state is changed
        /// </summary>
        /// <param name="cycle">The current balloon state</param>
        private void GameCycle_OnCycleChanged(GameCycle.OrderCylce cycle)
        {
        }

        /// <summary>
        /// Called when the PlayState is exited and another state (such as the main menu) is loaded.
        /// </summary>
        public override void OnExit()
        {
            ChunkManager.PauseThreads = true;
            base.OnExit();
        }

        /// <summary>
        /// Called by the loading thread just before the game is loaded.
        /// </summary>
        public void Preload()
        {
            drawer2D = new Drawer2D(Content, GraphicsDevice);
          
        }

        /// <summary>
        /// Generates a random set of dwarves in the given chunk.
        /// </summary>
        /// <param name="numDwarves">Number of dwarves to generate</param>
        /// <param name="c">The chunk the dwarves belong to</param>
        public void CreateInitialDwarves(int numDwarves, VoxelChunk c)
        {
            Vector3 g = c.WorldToGrid(Camera.Position);
            // Find the height of the world at the camera
            float h = c.GetFilledVoxelGridHeightAt((int) g.X, ChunkHeight - 1, (int) g.Z);

            // This is done just to make sure the camera is in the correct place.
            Camera.UpdateBasisVectors();
            Camera.UpdateProjectionMatrix();
            Camera.UpdateViewMatrix();

            // Spawn the dwarves above the terrain
            for(int i = 0; i < numDwarves; i++)
            {
                Vector3 dorfPos = new Vector3(Camera.Position.X + (float) Random.NextDouble(), h + 10, Camera.Position.Z + (float) Random.NextDouble());
                Physics creat = (Physics) EntityFactory.GenerateDwarf(dorfPos,
                    ComponentManager, Content, GraphicsDevice, ChunkManager, Camera, ComponentManager.Factions.Factions["Player"], PlanService, "Dwarf");

                creat.Velocity = new Vector3(1, 0, 0);
            }

            // Turn the camera to face the dwarves
            Camera.Target = new Vector3(Camera.Position.X, h + 10, Camera.Position.Z + 10);
            Camera.Phi = -(float) Math.PI * 0.3f;
        }

        /// <summary>
        /// Creates a bunch of stuff (such as the biome library, primitive library etc.) which won't change
        /// from game to game.
        /// </summary>
        public void InitializeStaticData()
        {
            if (SoundManager.Content == null)
            {
                SoundManager.Content = Content;
                SoundManager.LoadDefaultSounds();
            }
            new PrimitiveLibrary(GraphicsDevice, Content);
            InstanceManager = new InstanceManager();

            EntityFactory.InstanceManager = InstanceManager;
            InstanceManager.CreateStatics(Content);

            Color[] white = new Color[1];
            white[0] = Color.White;
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(white);

            Tilesheet = TextureManager.GetTexture("TileSet");
            AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            DefaultShader = Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders);

            VoxelLibrary = new VoxelLibrary();
            VoxelLibrary.InitializeDefaultLibrary(GraphicsDevice, Tilesheet);

            bloom = new BloomComponent(Game)
            {
                Settings = BloomSettings.PresetSettings[5]
            };
            bloom.Initialize();


            SoundManager.Content = Content;
            PlanService.Restart();

            ComponentManager = new ComponentManager();
            ComponentManager.RootComponent = new Body(ComponentManager, "root", null, Matrix.Identity, Vector3.Zero, Vector3.Zero, false);
            Vector3 origin = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y);
            Vector3 extents = new Vector3(1500, 1500, 1500);
            ComponentManager.CollisionManager = new CollisionManager(new BoundingBox(origin - extents, origin + extents));

            Alliance.Relationships = Alliance.InitializeRelationships();
        }

        /// <summary>
        /// Creates the terrain that is immediately around the player's spawn point.
        /// If loading from a file, loads the existing terrain from a file.
        /// </summary>
        public void GenerateInitialChunks()
        {
            gameFile = null;

            bool fileExists = !string.IsNullOrEmpty(ExistingFile);

            // If we already have a file, we need to load all the chunks from it.
            // This is preliminary stuff that just makes sure the file exists and can be loaded.
            if(fileExists)
            {
                LoadingMessage = "Loading " + ExistingFile;
                gameFile = new GameFile(ExistingFile, true);
                Sky.TimeOfDay = gameFile.Data.Metadata.TimeOfDay;
                WorldOrigin = gameFile.Data.Metadata.WorldOrigin;
                WorldScale = gameFile.Data.Metadata.WorldScale;
                ChunkWidth = gameFile.Data.Metadata.ChunkWidth;
                ChunkHeight = gameFile.Data.Metadata.ChunkHeight;

                if(gameFile.Data.Metadata.OverworldFile != null && gameFile.Data.Metadata.OverworldFile != "flat")
                {
                    LoadingMessage = "Loading world " + gameFile.Data.Metadata.OverworldFile;
                    Overworld.Name = gameFile.Data.Metadata.OverworldFile;
                    DirectoryInfo worldDirectory = Directory.CreateDirectory(DwarfGame.GetGameDirectory() + Program.DirChar + "Worlds" + Program.DirChar + Overworld.Name);
                    OverworldFile overWorldFile = new OverworldFile(worldDirectory.FullName + Program.DirChar +  "world." + OverworldFile.CompressedExtension, true);
                    Overworld.Map = overWorldFile.Data.CreateMap();
                    Overworld.Name = overWorldFile.Data.Name;
                    WorldWidth = Overworld.Map.GetLength(1);
                    WorldHeight = Overworld.Map.GetLength(0);
                }
                else
                {
                    LoadingMessage = "Generating flat world..";
                    Overworld.CreateUniformLand(Game.GraphicsDevice);
                }

            }

           
            ChunkGenerator = new ChunkGenerator(VoxelLibrary, Seed, 0.02f, ChunkHeight / 2.0f);

            Vector3 globalOffset = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y) * WorldScale;

            if(fileExists)
            {
                globalOffset /= WorldScale;
            }


            // If the file exists, we get the camera's pose from the file.
            // Otherwise, we set it to a pose above the center of the world (0, 0, 0)
            // facing down slightly.
            Camera = fileExists ? gameFile.Data.Camera : 
                new OrbitCamera(0, 0, 10f, new Vector3(ChunkWidth, ChunkHeight - 1.0f, ChunkWidth) + globalOffset, new Vector3(0, 50, 0) + globalOffset, MathHelper.PiOver4, AspectRatio, 0.1f, GameSettings.Default.VertexCullDistance);

            Drawer3D.Camera = Camera;

            // Creates the terrain management system.
            ChunkManager = new ChunkManager(Content, (uint) ChunkWidth, (uint) ChunkHeight, (uint) ChunkWidth, Camera, GraphicsDevice, Tilesheet,
                TextureManager.GetTexture(ContentPaths.Terrain.terrain_illumination),
                TextureManager.GetTexture(ContentPaths.Gradients.sungradient),
                TextureManager.GetTexture(ContentPaths.Gradients.ambientgradient),
                TextureManager.GetTexture(ContentPaths.Gradients.torchgradient),
                ChunkGenerator)
            {
                Components = ComponentManager
            };

            // Trying to determine the global offset from overworld coordinates (pixels in the overworld) to
            // voxel coordinates.
            globalOffset = ChunkManager.ChunkData.RoundToChunkCoords(globalOffset);
            globalOffset.X *= ChunkWidth;
            globalOffset.Y *= ChunkHeight;
            globalOffset.Z *= ChunkWidth;

            // If there's no file, we have to offset the camera relative to the global offset.
            if(!fileExists)
            {
                WorldOrigin = new Vector2(globalOffset.X, globalOffset.Z);
                Camera.Position = new Vector3(0, 10, 0) + globalOffset;
                Camera.Target = new Vector3(0, 10, 1) + globalOffset;
                Camera.Radius = 0.01f;
                Camera.Phi = -1.57f;
            }



            // If there's no file, we have to initialize the first chunk coordinate
            if(gameFile == null)
            {
                ChunkManager.PotentialChunks.Add(new BoundingBox(new Vector3(0, 0, 0)
                                                                 + globalOffset,
                    new Vector3(ChunkWidth, ChunkHeight, ChunkWidth)
                    + globalOffset));
                ChunkManager.GenerateInitialChunks(Camera, ref LoadingMessage);
            }
            // Otherwise, we just load all the chunks from the file.
            else
            {
                LoadingMessage = "Loading Chunks from Game File";
                ChunkManager.ChunkData.LoadFromFile(gameFile, ref LoadingMessage);
            }

            // If there's no file, for some reason we modify the camera position...
            // TODO: Figure out why the camera keeps needing to be reset.
            if(!fileExists)
            {
                Camera.Radius = 0.01f;
                Camera.Phi = -1.57f / 4.0f;
                Camera.Theta = 0.0f;
            }

            // Finally, the chunk manager's threads are started to allow it to 
            // dynamically rebuild terrain
            ChunkManager.RebuildList = new ConcurrentQueue<VoxelChunk>();
            ChunkManager.StartThreads();
        }


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
                using (RenderTarget2D renderTarget = new RenderTarget2D(GraphicsDevice, resolution.X, resolution.Y, false, SurfaceFormat.Color, DepthFormat.Depth24))
                {
                    GraphicsDevice.SetRenderTarget(renderTarget);
                    DrawSky(new GameTime(), Camera.ViewMatrix);
                    Draw3DThings(new GameTime(), DefaultShader, Camera.ViewMatrix);
                    DrawComponents(new GameTime(), DefaultShader, Camera.ViewMatrix, ComponentManager.WaterRenderType.None, 0);
                    GraphicsDevice.SetRenderTarget(null);
                    renderTarget.SaveAsPng(new FileStream(filename, FileMode.Create), resolution.X, resolution.Y);
                    GraphicsDevice.Textures[0] = null;
                    GraphicsDevice.Indices = null;
                    GraphicsDevice.SetVertexBuffer(null);
                }
            }
            catch(IOException e)
            {
                Console.Error.WriteLine(e.Message);
                return false;
            }

            return true;

        }

        /// <summary>
        /// Initializes water and lava asset definitions
        /// and liquid properties
        /// TODO: Move this to another file.
        /// </summary>
        public void CreateLiquids()
        {
            WaterRenderer = new WaterRenderer(GraphicsDevice);

            LiquidAsset waterAsset = new LiquidAsset
            {
                Type = LiquidType.Water,
                Opactiy = 0.3f,
                SloshOpacity = 0.7f,
                WaveHeight = 0.1f,
                WaveLength = 0.05f,
                WindForce = 0.001f,
                BumpTexture = TextureManager.GetTexture(ContentPaths.Terrain.water_normal),
                FoamTexture = TextureManager.GetTexture(ContentPaths.Terrain.foam),
                BaseTexture = TextureManager.GetTexture(ContentPaths.Terrain.cartoon_water),
                MinOpacity = 0.0f,
                RippleColor = new Vector4(0.1f, 0.1f, 0.1f, 0.0f),
                FlatColor = new Vector4(0.3f, 0.3f, 0.9f, 1.0f)
            };
            WaterRenderer.AddLiquidAsset(waterAsset);


            LiquidAsset lavaAsset = new LiquidAsset
            {
                Type = LiquidType.Lava,
                Opactiy = 0.99f,
                SloshOpacity = 1.0f,
                WaveHeight = 0.1f,
                WaveLength = 0.05f,
                WindForce = 0.001f,
                MinOpacity = 0.99f,
                BumpTexture = TextureManager.GetTexture(ContentPaths.Terrain.water_normal),
                FoamTexture = TextureManager.GetTexture(ContentPaths.Terrain.lavafoam),
                BaseTexture = TextureManager.GetTexture(ContentPaths.Terrain.lava),
                RippleColor = new Vector4(0.5f, 0.4f, 0.04f, 0.0f),
                FlatColor = new Vector4(0.9f, 0.7f, 0.2f, 1.0f)
            };

            WaterRenderer.AddLiquidAsset(lavaAsset);
        }


        /// <summary>
        /// Creates the sky renderer and loads all the cube maps
        /// for the sky box
        /// </summary>
        public void CreateSky()
        {
            Sky = new SkyRenderer(
                TextureManager.GetTexture(ContentPaths.Sky.moon),
                TextureManager.GetTexture(ContentPaths.Sky.sun),
                Content.Load<TextureCube>(ContentPaths.Sky.day_sky),
                Content.Load<TextureCube>(ContentPaths.Sky.night_sky),
                TextureManager.GetTexture(ContentPaths.Gradients.skygradient),
                Content.Load<Model>(ContentPaths.Models.sphereLowPoly),
                Content.Load<Effect>(ContentPaths.Shaders.SkySphere));
        }

        /// <summary>
        /// Creates the user interface + player controls.
        /// </summary>
        /// <param name="createMaster">True if the Game Master needs to be created as well.</param>
        public void CreateGUI(bool createMaster)
        {
            LoadingMessage = "Creating GUI";
            IndicatorManager.SetupStandards();

            GUI = new DwarfGUI(Game, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);
         
            if(!createMaster)
            {
                return;
            }

            Master = new GameMaster(ComponentManager.Factions.Factions["Player"], Game, ComponentManager, ChunkManager, Camera, GraphicsDevice, GUI);
            CreateGUIComponents();
        }


        /// <summary>
        /// Creates all of the sub-components of the GUI in for the PlayState (buttons, etc.)
        /// </summary>
        public void CreateGUIComponents()
        {
            GUI.RootComponent.ClearChildren();
            GridLayout layout = new GridLayout(GUI, GUI.RootComponent, 11, 11)
            {
                LocalBounds = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
                FitToParent =  false
            };

            GUI.RootComponent.AddChild(Master.Debugger.MainPanel);
            layout.AddChild(Master.ToolBar);
            Master.ToolBar.Parent = layout;
            layout.SetComponentPosition(Master.ToolBar, 7, 10, 4, 1);

            GUIComponent companyInfoComponent = new GUIComponent(GUI, layout);

            layout.SetComponentPosition(companyInfoComponent, 0, 0, 4, 2);

            GUIComponent resourceInfoComponent = new ResourceInfoComponent(GUI, layout, Master.Faction);
            layout.SetComponentPosition(resourceInfoComponent, 7, 0, 4, 2);

            GridLayout infoLayout = new GridLayout(GUI, companyInfoComponent, 3, 4);

            CompanyLogoPanel = new ImagePanel(GUI, infoLayout, new ImageFrame(TextureManager.GetTexture("CompanyLogo")));
            infoLayout.SetComponentPosition(CompanyLogoPanel, 0, 0, 1, 1);

            CompanyNameLabel = new Label(GUI, infoLayout, PlayerSettings.Default.CompanyName, GUI.DefaultFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100),
                ToolTip = "Our company Name."
            };
            infoLayout.SetComponentPosition(CompanyNameLabel, 1, 0, 1, 1);

            MoneyLabel = new Label(GUI, infoLayout, Master.Faction.Economy.CurrentMoney.ToString("C"), GUI.DefaultFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100),
                ToolTip = "Amount of money in our treasury."
            };
            infoLayout.SetComponentPosition(MoneyLabel, 3, 0, 1, 1);


            TimeLabel = new Label(GUI, layout, Time.CurrentDate.ToShortDateString() + " " + Time.CurrentDate.ToShortTimeString(), GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100),
                ToolTip = "Current time and date."
            };
            layout.SetComponentPosition(TimeLabel, 5, 0, 1, 1);

            CurrentLevelLabel = new Label(GUI, infoLayout, "Slice: " + ChunkManager.ChunkData.MaxViewingLevel, GUI.DefaultFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100),
                ToolTip = "The maximum height of visible terrain"
            };
            infoLayout.SetComponentPosition(CurrentLevelLabel, 0, 1, 1, 1);

            CurrentLevelUpButton = new Button(GUI, infoLayout, "up", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Go up one level of visible terrain"
            };

            infoLayout.SetComponentPosition(CurrentLevelUpButton, 2, 1, 1, 1);
            CurrentLevelUpButton.OnClicked += CurrentLevelUpButton_OnClicked;

            CurrentLevelDownButton = new Button(GUI, infoLayout, "down", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Go down one level of visible terrain"
            };
            infoLayout.SetComponentPosition(CurrentLevelDownButton, 1, 1, 1, 1);
            CurrentLevelDownButton.OnClicked += CurrentLevelDownButton_OnClicked;

            LevelSlider = new Slider(GUI, layout, "", ChunkManager.ChunkData.MaxViewingLevel, 0, ChunkManager.ChunkData.ChunkSizeY, Slider.SliderMode.Integer)
            {
                Orient = Slider.Orientation.Vertical,
                ToolTip = "Controls the maximum height of visible terrain",
                DrawLabel = false
            };

            layout.SetComponentPosition(LevelSlider, 0, 1, 1, 6);
            LevelSlider.OnClicked += LevelSlider_OnClicked;
            LevelSlider.InvertValue = true;

            MiniMap = new Minimap(GUI, layout, 192, 192, this, TextureManager.GetTexture(ContentPaths.Terrain.terrain_colormap), TextureManager.GetTexture(ContentPaths.GUI.gui_minimap))
            {
                IsVisible =  true
            };

            layout.SetComponentPosition(MiniMap, 0, 8, 4, 4);



            Button moneyButton = new Button(GUI, layout, "", GUI.DefaultFont, Button.ButtonMode.ImageButton, new ImageFrame(TextureManager.GetTexture("IconSheet"), 32, 2, 1))
            {
                KeepAspectRatio = true,
                ToolTip = "Opens the Economy Menu",
                DontMakeBigger = true,
                DrawFrame = true
            };


            moneyButton.OnClicked += moneyButton_OnClicked;

            layout.SetComponentPosition(moneyButton, 3, 10, 1, 1);


            InputManager.KeyReleasedCallback -= InputManager_KeyReleasedCallback;
            InputManager.KeyReleasedCallback += InputManager_KeyReleasedCallback;

            layout.UpdateSizes();

        }

        void moneyButton_OnClicked()
        {
            if (StateManager.NextState == "")
            {
                GUI.RootComponent.IsVisible = false;
                StateManager.PushState("EconomyState");
            }
            Paused = true;
        }


        /// <summary>
        /// Creates the balloon, the dwarves, and the initial balloon port.
        /// </summary>
        public void CreateInitialEmbarkment()
        {
            // If no file exists, we have to create the balloon and balloon port.
            if(string.IsNullOrEmpty(ExistingFile))
            {
                VoxelChunk c = ChunkManager.ChunkData.GetVoxelChunkAtWorldLocation(Camera.Position);
                GenerateInitialBalloonPort(Master.Faction.RoomBuilder, ChunkManager, Camera.Position.X, Camera.Position.Z, 3);
                CreateInitialDwarves(5, c);
                EntityFactory.CreateBalloon(Camera.Position + new Vector3(0, 1000, 0),  new Vector3(Camera.Position.X, ChunkHeight, Camera.Position.Z), ComponentManager, Content, GraphicsDevice, new ShipmentOrder(0, null), Master.Faction);
            }

            // Otherwise, we unfortunately need to take care of preliminaries to make sure
            // The game master was created correctly.
            else
            {
                InstanceManager.Clear();
                gameFile.LoadComponents(ExistingFile);
                ComponentManager = gameFile.Data.Components;
                Master = new GameMaster(ComponentManager.Factions.Factions["Player"], Game, ComponentManager, ChunkManager, Camera, GraphicsDevice, GUI);
                ChunkManager.Components = ComponentManager;
                Master.Faction.Components = ComponentManager;

                CreateGUIComponents();

            }
        }

        /// <summary>
        /// Executes the entire game loading sequence, and draws loading messages.
        /// </summary>
        public void Load()
        {
            EnableScreensaver = true;
            LoadingMessage = "Initializing Static Data...";
            InitializeStaticData();


            LoadingMessage = "Creating ParticleTrigger...";
            CreateParticles();

            LoadingMessage = "Creating Sky...";
            CreateSky();

            LoadingMessage = "Creating Liquids..";
            CreateLiquids();

            LoadingMessage = "Generating Initial Terrain Chunks...";
            GenerateInitialChunks();

            LoadingMessage = "Creating GUI";
            CreateGUI(string.IsNullOrEmpty(ExistingFile));

            LoadingMessage = "Embarking.";
            CreateInitialEmbarkment();


            IsInitialized = true;

            LoadingMessage = "Complete.";
            EnableScreensaver = false;
        }

        /// <summary>
        /// Called when the slice slider was moved.
        /// </summary>
        private void LevelSlider_OnClicked()
        {
            ChunkManager.ChunkData.SetMaxViewingLevel((int) LevelSlider.SliderValue, ChunkManager.SliceMode.Y);
        }

        /// <summary>
        /// Called when the "Slice -" button is pressed
        /// </summary>
        private void CurrentLevelDownButton_OnClicked()
        {
            ChunkManager.ChunkData.SetMaxViewingLevel(ChunkManager.ChunkData.MaxViewingLevel - 1, ChunkManager.SliceMode.Y);
        }


        /// <summary>
        /// Called when the "Slice +" button is pressed
        /// </summary>
        private void CurrentLevelUpButton_OnClicked()
        {
            ChunkManager.ChunkData.SetMaxViewingLevel(ChunkManager.ChunkData.MaxViewingLevel + 1, ChunkManager.SliceMode.Y);
        }


        /// <summary>
        /// Creates a flat, wooden balloon port for the balloon to land on, and Dwarves to sit on.
        /// TODO: Fix height to that it's not too tall when on a mountain.
        /// </summary>
        /// <param name="roomDes">The player's BuildRoom designator (so that we can create a balloon port)</param>
        /// <param name="chunkManager">The terrain handler</param>
        /// <param name="x">The position of the center of the balloon port</param>
        /// <param name="z">The position of the center of the balloon port</param>
        /// <param name="size">The size of the (square) balloon port in voxels on a side</param>
        public void GenerateInitialBalloonPort(RoomBuilder roomDes, ChunkManager chunkManager, float x, float z, int size)
        {
            Vector3 pos = new Vector3(x, ChunkHeight - 1, z);

            // First, compute the maximum height of the terrain in a square window.
            int maxHeight = int.MinValue;
            for(int dx = -size; dx <= size; dx++)
            {
                for(int dz = -size; dz <= size; dz++)
                {
                    Vector3 worldPos = new Vector3(pos.X + dx, pos.Y, pos.Z + dz);
                    VoxelChunk chunk = chunkManager.ChunkData.GetVoxelChunkAtWorldLocation(worldPos);

                    if(chunk == null)
                    {
                        continue;
                    }

                    Vector3 gridPos = chunk.WorldToGrid(worldPos);
                    int h = chunk.GetFilledHeightOrWaterAt((int) gridPos.X + dx, (int) gridPos.Y, (int) gridPos.Z + dz);


                    if(h >= maxHeight)
                    {
                        maxHeight = h;
                    }
                }
            }

            // Next, create the balloon port by deciding which voxels to fill.
            List<VoxelRef> designations = new List<VoxelRef>();
            for(int dx = -size; dx <= size; dx++)
            {
                for(int dz = -size; dz <= size; dz++)
                {
                    Vector3 worldPos = new Vector3(pos.X + dx, pos.Y, pos.Z + dz);
                    VoxelChunk chunk = chunkManager.ChunkData.GetVoxelChunkAtWorldLocation(worldPos);
                    Vector3 gridPos = chunk.WorldToGrid(worldPos);
                    int h = chunk.GetFilledVoxelGridHeightAt((int) gridPos.X, (int) gridPos.Y, (int) gridPos.Z);

                    if(h == -1)
                    {
                        continue;
                    }

                    // Fill from the top height down to the bottom.
                    for(int y = h - 1; y < maxHeight; y++)
                    {
                        Vector3 worldCoord2 = chunk.GridToWorld(new Vector3((int) gridPos.X, y, (int) gridPos.Z));
                        Voxel v = new Voxel(worldCoord2, VoxelLibrary.GetVoxelType("Scaffold"), VoxelLibrary.GetPrimitive("Scaffold"), true);
                        ;
                        chunk.VoxelGrid[(int) gridPos.X][y][(int) gridPos.Z] = v;
                        chunk.Water[(int) gridPos.X][y][(int) gridPos.Z].WaterLevel = 0;
                        v.Chunk = chunk;
                        v.Chunk.NotifyTotalRebuild(!v.IsInterior);

                        if(y == maxHeight - 1)
                        {
                            designations.Add(v.GetReference());
                        }
                    }
                }
            }

            // Actually create the BuildRoom.
            Room toBuild = new Room(designations, RoomLibrary.GetType("BalloonPort"), chunkManager);
            BuildRoomOrder buildDes = new BuildRoomOrder(toBuild, roomDes.Faction);
            buildDes.Build();
            roomDes.DesignatedRooms.Add(toBuild);
        }

        /// <summary>
        /// A library function which creates a "explosion" particle effect (bouncy particles)
        /// TODO: Move this to a different file
        /// </summary>
        /// <param name="assetName">Particle texture name</param>
        /// <param name="name">Name of the effect</param>
        /// <returns>A particle emitter which behaves like an explosion.</returns>
        public ParticleEmitter CreateGenericExplosion(string assetName, string name)
        {
            List<Point> frm = new List<Point>
            {
                new Point(0, 0)
            };
            Texture2D tex = TextureManager.GetTexture(assetName);
            EmitterData testData = new EmitterData
            {
                Animation = new Animation(GraphicsDevice, tex, assetName, tex.Width, tex.Height, frm, true, Color.White, 1.0f, 1.0f, 1.0f, false),
                ConstantAccel = new Vector3(0, -10, 0),
                LinearDamping = 0.9999f,
                AngularDamping = 0.9f,
                EmissionFrequency = 50.0f,
                EmissionRadius = 1.0f,
                EmissionSpeed = 5.0f,
                GrowthSpeed = -0.0f,
                MaxAngle = 3.14159f,
                MinAngle = 0.0f,
                MaxParticles = 1000,
                MaxScale = 0.2f,
                MinScale = 0.1f,
                MinAngular = -5.0f,
                MaxAngular = 5.0f,
                ParticleDecay = 0.5f,
                ParticlesPerFrame = 0,
                ReleaseOnce = true,
                Texture = tex,
                CollidesWorld = true,
                Sleeps = true
            };

            ParticleManager.RegisterEffect(name, testData);
            return ParticleManager.Emitters[name];
        }

        /// <summary>
        /// Creates a generic particle effect which is like a "puff" (cloudy particles which float)
        /// </summary>
        /// <param name="name">Name of the effect</param>
        /// <param name="assetName">Texture asset to use</param>
        /// <param name="state">Blend mode of the particles (alpha or additive)</param>
        /// <returns>A puff emitter</returns>
        public EmitterData CreatePuffLike(string name, string assetName, BlendState state)
        {
            List<Point> frm = new List<Point>
            {
                new Point(0, 0)
            };

            EmitterData data = new EmitterData
            {
                Animation = new Animation(GraphicsDevice, TextureManager.GetTexture(assetName), name, 32, 32, frm, true, Color.White, 1.0f, 1.0f, 1.0f, false),
                ConstantAccel = new Vector3(0, 3, 0),
                LinearDamping = 0.9f,
                AngularDamping = 0.99f,
                EmissionFrequency = 20.0f,
                EmissionRadius = 1.0f,
                EmissionSpeed = 2.0f,
                GrowthSpeed = -0.6f,
                MaxAngle = 3.14159f,
                MinAngle = 0.0f,
                MaxParticles = 1000,
                MaxScale = 1.0f,
                MinScale = 0.1f,
                MinAngular = -5.0f,
                MaxAngular = 5.0f,
                ParticleDecay = 0.8f,
                ParticlesPerFrame = 0,
                ReleaseOnce = true,
                Texture = TextureManager.GetTexture(assetName),
                Blend = state
            };

            return data;
        }

        /// <summary>
        /// Creates all the static particle emitters used in the game.
        /// </summary>
        public void CreateParticles()
        {
            ParticleManager = new ParticleManager(ComponentManager);

            // Smoke
            EmitterData puff = CreatePuffLike("puff", ContentPaths.Particles.puff, BlendState.AlphaBlend);

            // Bubbles
            EmitterData bubble = CreatePuffLike("splash2", ContentPaths.Particles.splash2, BlendState.AlphaBlend);
            bubble.ConstantAccel = new Vector3(0, -10, 0);
            bubble.EmissionSpeed = 5;
            bubble.LinearDamping = 0.999f;
            bubble.GrowthSpeed = 1.05f;
            bubble.ParticleDecay = 1.5f;

            // Fire
            EmitterData flame = CreatePuffLike("flame", ContentPaths.Particles.flame, BlendState.Additive);
            ParticleManager.RegisterEffect("puff", puff);
            ParticleManager.RegisterEffect("splash2", bubble);
            ParticleManager.RegisterEffect("flame", flame);

            List<Point> frm2 = new List<Point>
            {
                new Point(0, 0)
            };

            // Leaves
            EmitterData testData2 = new EmitterData
            {
                Animation = new Animation(GraphicsDevice, TextureManager.GetTexture(ContentPaths.Particles.leaf), "leaf", 32, 32, frm2, true, Color.White, 1.0f, 1.0f, 1.0f, false),
                ConstantAccel = new Vector3(0, -10, 0),
                LinearDamping = 0.99f,
                AngularDamping = 0.99f,
                EmissionFrequency = 1.0f,
                EmissionRadius = 2.0f,
                EmissionSpeed = 5.0f,
                GrowthSpeed = 0.0f,
                MaxAngle = 3.14159f,
                MinAngle = 0.0f,
                MaxParticles = 1000,
                MaxScale = 0.5f,
                MinScale = 0.1f,
                MinAngular = -5.0f,
                MaxAngular = 5.0f,
                ParticleDecay = 0.5f,
                ParticlesPerFrame = 0,
                ReleaseOnce = true,
                Texture = TextureManager.GetTexture(ContentPaths.Particles.leaf)
            };

            ParticleManager.RegisterEffect("Leaves", testData2);

            // Various resource explosions
            CreateGenericExplosion(ContentPaths.Particles.dirt_particle, "dirt_particle");
            CreateGenericExplosion(ContentPaths.Particles.stone_particle, "stone_particle");
            CreateGenericExplosion(ContentPaths.Particles.sand_particle, "sand_particle");

            // Blood explosion
            ParticleEmitter b = CreateGenericExplosion(ContentPaths.Particles.blood_particle, "blood_particle");
            b.Data.MinScale = 0.1f;
            b.Data.MaxScale = 0.15f;
            b.Data.GrowthSpeed = -0.1f;
            b.Data.EmissionSpeed = 5f;
        }


        /// <summary>
        /// Called when the user releases a key
        /// </summary>
        /// <param name="key">The keyboard key released</param>
        private void InputManager_KeyReleasedCallback(Keys key)
        {
            if(key == ControlSettings.Default.Map)
            {
                DrawMap = !DrawMap;
                MiniMap.SetMinimized(!DrawMap);
            }

            if(key == Keys.Escape)
            {
                if(Master.CurrentToolMode != GameMaster.ToolMode.SelectUnits)
                {
                    Master.ToolBar.ToolButtons[GameMaster.ToolMode.SelectUnits].InvokeClick();
                }
                else if(PausePanel != null && PausePanel.IsVisible)
                {
                    PausePanel.IsVisible = false;
                    Paused = false;
                }
                else
                {
                    OpenPauseMenu();   
                }


            }

                // Special case: number keys reserved for changing tool mode
            else if(InputManager.IsNumKey(key))
            {
                int index = InputManager.GetNum(key) - 1;

                List<GameMaster.ToolMode> modes = Enum.GetValues(typeof(GameMaster.ToolMode)).Cast<GameMaster.ToolMode>().ToList();

                // Last index reserved for god mode
                if(index < 0 || index >= modes.Count - 1)
                {
                    return;
                }

                // In this special case, all dwarves are selected
                if(index == 0 && Master.SelectedMinions.Count == 0)
                {
                    Master.SelectedMinions.AddRange(Master.Faction.Minions);
                }

                if(index == 0 || Master.SelectedMinions.Count > 0)
                {
                    Master.ToolBar.ToolButtons[modes[index]].InvokeClick();

                    //Master.ToolBar.CurrentMode = modes[index];
                }

            }

        }


        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void Update(GameTime gameTime)
        {
            // If this playstate is not supposed to be running,
            // just exit.
            if(!Game.IsActive || !IsActiveState)
            {
                return;
            }



            // Handles time foward + backward TODO: Replace with input manager
            if(Keyboard.GetState().IsKeyDown(ControlSettings.Default.TimeForward))
            {
                Time.Speed = 10000;
            }
            else if(Keyboard.GetState().IsKeyDown(ControlSettings.Default.TimeBackward))
            {
                Time.Speed = -10000;
            }
            else
            {
                Time.Speed = 100;
            }

            // If End is pressed, quit the game TODO: Replace with input manager.
            if(Keyboard.GetState().IsKeyDown(Keys.End))
            {
                DwarfGame.ExitGame = true;
                Game.Exit();
            }


            // Handles pausing and unpausing TODO: replace with input manager
            if(Keyboard.GetState().IsKeyDown(ControlSettings.Default.Pause))
            {
                if(!pausePressed)
                {
                    pausePressed = true;
                }
            }
            else
            {
                if(pausePressed)
                {
                    pausePressed = false;
                    Paused = !Paused;
                }
            }

            // Turns the gui on and off TODO: replace with input manager
            if(Keyboard.GetState().IsKeyDown(ControlSettings.Default.ToggleGUI))
            {
                if(!bPressed)
                {
                    bPressed = true;
                }
            }
            else
            {
                if(bPressed)
                {
                    bPressed = false;
                    GUI.RootComponent.IsVisible = !GUI.RootComponent.IsVisible;
                }
            }

            // Hack to test the order screen TODO: get rid of
            /*
            if(Keyboard.GetState().IsKeyDown(ControlSettings.Default.OrderScreen))
            {
                if(StateManager.NextState == "")
                {
                    OrderState orderState = (OrderState) StateManager.States["OrderState"];
                    orderState.Mode = OrderState.OrderMode.Selling;
                    GUI.RootComponent.IsVisible = false;
                    StateManager.PushState("OrderState");
                }
                Paused = true;
            }
            */

            // If not paused, we want to just update the rest of the game.
            if (!Paused)
            {
                IndicatorManager.Update(gameTime);
                Time.Update(gameTime);
                GameCycle.Update(gameTime);
                ComponentManager.CollisionManager.Update(gameTime);
                Master.Update(Game, gameTime);
                ComponentManager.Update(gameTime, ChunkManager, Camera);
                Sky.TimeOfDay = Time.GetSkyLightness();
                Sky.CosTime = (float)(Time.GetTotalHours() * 2 * Math.PI / 24.0f);
                DefaultShader.Parameters["xTimeOfDay"].SetValue(Sky.TimeOfDay);

                if(KeyManager.RotationEnabled())
                    Mouse.SetPosition(GameState.Game.GraphicsDevice.Viewport.Width / 2, GameState.Game.GraphicsDevice.Viewport.Height / 2);

            }


            // These things are updated even when the game is paused
            GUI.Update(gameTime);
            ChunkManager.Update(gameTime, Camera, GraphicsDevice);
            InstanceManager.Update(gameTime, Camera, GraphicsDevice);
            Input.Update();

            SoundManager.Update(gameTime, Camera);

            // Updates some of the GUI status
            if(!Paused && Game.IsActive)
            {
                /*
                TimeSpan t = TimeSpan.FromSeconds(GameCycle.CycleTimers[GameCycle.CurrentCycle].TargetTimeSeconds - GameCycle.CycleTimers[GameCycle.CurrentCycle].CurrentTimeSeconds);

                string answer = string.Format("{0:D2}m:{1:D2}s",
                    t.Minutes,
                    t.Seconds);
                OrderStatusLabel.Text = "Balloon: " + GameCycle.GetStatusString(GameCycle.CurrentCycle) + " ETA: " + answer;
                OrderStatusLabel.TextColor = GameCycle.GetColor(GameCycle.CurrentCycle, (float) gameTime.TotalGameTime.TotalSeconds);
                OrderStatusLabel.OnClicked += OrderStatusLabel_OnClicked;
                 */
                CurrentLevelLabel.Text = "Slice: " + ChunkManager.ChunkData.MaxViewingLevel + "/" + ChunkHeight;
                TimeLabel.Text = Time.CurrentDate.ToShortDateString() + " " + Time.CurrentDate.ToShortTimeString();
                MoneyLabel.Text = Master.Faction.Economy.CurrentMoney.ToString("C");
            }

            // Make sure that the slice slider snaps to the current viewing level (an integer)
            if(!LevelSlider.IsMouseOver)
            {
                LevelSlider.SliderValue = ChunkManager.ChunkData.MaxViewingLevel;
            }

            base.Update(gameTime);
        }


        /// <summary>
        /// Called whenever the escape button is pressed. Opens a small menu for saving/loading, etc.
        /// </summary>
        public void OpenPauseMenu()
        {
            Paused = true;

            int w = 200;
            int h = 200;
            
            PausePanel = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(GraphicsDevice.Viewport.Width / 2 - w / 2, GraphicsDevice.Viewport.Height / 2 - h / 2, w, h)
            };

            GridLayout pauseLayout = new GridLayout(GUI, PausePanel, 1, 1);

            ListSelector pauseSelector = new ListSelector(GUI, pauseLayout)
            {
                Label = "-Menu-",
                DrawPanel = false,
                Mode = ListItem.SelectionMode.Selector
            };
            pauseLayout.SetComponentPosition(pauseSelector, 0, 0, 1, 1);
            pauseLayout.UpdateSizes();

            pauseSelector.AddItem("Continue");
            pauseSelector.AddItem("Save");
            pauseSelector.AddItem("Quit");
            
            pauseSelector.OnItemClicked += () => pauseSelector_OnItemClicked(pauseSelector);


        }

        /// <summary>
        /// Called whenever the pause menu is clicked.
        /// </summary>
        /// <param name="selector">The list of things the user could have clicked on.</param>
        void pauseSelector_OnItemClicked(ListSelector selector)
        {
            string selected = selector.SelectedItem.Label;
            switch(selected)
            {
                case "Continue":
                    GUI.RootComponent.RemoveChild(PausePanel);
                    Paused = false;
                    break;
                case "Save":
                    SaveGame(DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                    break;
                case "Quit":
                    StateManager.StateStack.Clear();
                    MainMenuState menuState = StateManager.GetState<MainMenuState>("MainMenuState");
                    menuState.IsGameRunning = false;
                    StateManager.States["PlayState"] = new PlayState(Game, StateManager);
                    StateManager.CurrentState = "";
                    StateManager.PushState("MainMenuState");
                    break;

            }
        }


        /// <summary>
        /// Saves the game state to a file.
        /// </summary>
        /// <param name="filename">The file to save to</param>
        public void SaveGame(string filename)
        {
            DirectoryInfo worldDirectory = Directory.CreateDirectory(DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Worlds" + Path.DirectorySeparatorChar + Overworld.Name);

            if(Overworld.Name != "flat")
            {
                OverworldFile file = new OverworldFile(Overworld.Map, Overworld.Name);
                file.WriteFile(worldDirectory.FullName + Path.DirectorySeparatorChar + "world." + OverworldFile.CompressedExtension, true);
                file.SaveScreenshot(worldDirectory.FullName + Path.DirectorySeparatorChar + "screenshot.png");
            }

            gameFile = new GameFile(Overworld.Name);
            gameFile.WriteFile(DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Saves" + Path.DirectorySeparatorChar + filename, true);
            TakeScreenshot(DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Saves" + Path.DirectorySeparatorChar + filename + Path.DirectorySeparatorChar + "screenshot.png", new Point(GraphicsDevice.Viewport.Width / 4, GraphicsDevice.Viewport.Height / 4));


            Dialog.Popup(GUI, "Save", "File saved.", Dialog.ButtonType.OK);
        
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
        /// <param name="cubeEffect">The textured shader</param>
        /// <param name="view">The view matrix of the camera</param> 
        public void Draw3DThings(GameTime gameTime, Effect cubeEffect, Matrix view)
        {
            Matrix viewMatrix = Camera.ViewMatrix;
            Camera.ViewMatrix = view;

            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            cubeEffect.Parameters["xView"].SetValue(view);

            cubeEffect.Parameters["xProjection"].SetValue(Camera.ProjectionMatrix);
            cubeEffect.CurrentTechnique = cubeEffect.Techniques["Textured"];

            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            ChunkManager.Render(Camera, gameTime, GraphicsDevice, cubeEffect, Matrix.Identity);


            if(Master.CurrentToolMode == GameMaster.ToolMode.Build)
            {
                Master.Faction.PutDesignator.Render(gameTime, GraphicsDevice, cubeEffect);
            }

            //ComponentManager.CollisionManager.DebugDraw();
            Camera.ViewMatrix = viewMatrix;
        }


        /// <summary>
        /// Draws all of the game entities
        /// </summary>
        /// <param name="gameTime">The current time</param>
        /// <param name="effect">The shader</param>
        /// <param name="view">The view matrix</param>
        /// <param name="waterRenderType">Whether we are rendering for reflection/refraction or nothing</param>
        /// <param name="waterLevel">The estimated height of water</param>
        public void DrawComponents(GameTime gameTime, Effect effect, Matrix view, ComponentManager.WaterRenderType waterRenderType, float waterLevel)
        {
            ComponentManager.Render(gameTime, ChunkManager, Camera, DwarfGame.SpriteBatch, GraphicsDevice, effect, waterRenderType, waterLevel);

            bool reset = waterRenderType == ComponentManager.WaterRenderType.None;

            InstanceManager.Render(GraphicsDevice, effect, Camera, reset);
        }

        /// <summary>
        /// Draws the sky box
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="view">The camera view matrix</param>
        public void DrawSky(GameTime time, Matrix view)
        {
            Matrix oldView = Camera.ViewMatrix;
            Camera.ViewMatrix = view;
            Sky.Render(time, GraphicsDevice, Camera);
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Camera.ViewMatrix = oldView;
        }


        /// <summary>
        /// If the game is not loaded yet, just draws a loading message centered
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void RenderUnitialized(GameTime gameTime)
        {
            DwarfGame.SpriteBatch.Begin();
            float t = (float) (Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2.0f) + 1.0f) * 0.5f + 0.5f;
            Color toDraw = new Color(t, t, t);
            SpriteFont font = Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
            Vector2 measurement = Datastructures.SafeMeasure(font, LoadingMessage);
            Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, LoadingMessage, font, new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - measurement.X / 2, Game.GraphicsDevice.Viewport.Height / 2), toDraw, new Color(50, 50, 50));
            DwarfGame.SpriteBatch.End();

            base.RenderUnitialized(gameTime);
        }


        /// <summary>
        /// Called when a frame is to be drawn to the screen
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void Render(GameTime gameTime)
        {
            // If we are simulating the game before starting, just display black.
            if(!PreSimulateTimer.HasTriggered)
            {
                PreSimulateTimer.Update(gameTime);
                base.Render(gameTime);
                return;
            }

            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            // Keeping track of a running FPS buffer (averaged)
            if(lastFps.Count > 100)
            {
                lastFps.RemoveAt(0);
            }


            // Controls the sky fog
            float x = (1.0f - Sky.TimeOfDay);
            x = x * x;
            DefaultShader.Parameters["xFogColor"].SetValue(new Vector3(0.32f * x, 0.58f * x, 0.9f * x));

            // Computes the water height.
            float wHeight = WaterRenderer.GetVisibleWaterHeight(ChunkManager, Camera, GraphicsDevice.Viewport, lastWaterHeight);
            lastWaterHeight = wHeight;

            // Draw reflection/refraction images
            WaterRenderer.DrawRefractionMap(gameTime, this, wHeight + 1.0f, Camera.ViewMatrix, DefaultShader, GraphicsDevice);
            WaterRenderer.DrawReflectionMap(gameTime, this, wHeight - 0.1f, GetReflectedCameraMatrix(wHeight), DefaultShader, GraphicsDevice);

            // Start drawing the bloom effect
            if(GameSettings.Default.EnableGlow)
            {
                bloom.BeginDraw();
            }

            // Draw the sky
            GraphicsDevice.Clear(Color.CornflowerBlue);
            DrawSky(gameTime, Camera.ViewMatrix);

            // Defines the current slice for the GPU
            Plane slicePlane = WaterRenderer.CreatePlane(ChunkManager.ChunkData.MaxViewingLevel + 1.3f, new Vector3(0, -1, 0), Camera.ViewMatrix, false);

            // Draw the whole world, and make sure to handle slicing
            DefaultShader.Parameters["ClipPlane0"].SetValue(new Vector4(slicePlane.Normal, slicePlane.D));
            DefaultShader.Parameters["Clipping"].SetValue(true);
            //Blue ghost effect above the current slice.
            DefaultShader.Parameters["GhostMode"].SetValue(true);
            Draw3DThings(gameTime, DefaultShader, Camera.ViewMatrix);

            // Now we want to draw the water on top of everything else
            DefaultShader.Parameters["Clipping"].SetValue(true);
            DefaultShader.Parameters["GhostMode"].SetValue(false);
            WaterRenderer.DrawWater(
                GraphicsDevice,
                (float) gameTime.TotalGameTime.TotalSeconds,
                DefaultShader,
                Camera.ViewMatrix,
                GetReflectedCameraMatrix(wHeight),
                Camera.ProjectionMatrix,
                new Vector3(0.1f, 0.0f, 0.1f),
                Camera,
                ChunkManager);

            DefaultShader.CurrentTechnique = DefaultShader.Techniques["Textured"];
            DefaultShader.Parameters["Clipping"].SetValue(false);

            //Body.CollisionManager.DebugDraw();

            // Render simple geometry (boxes, etc.)
            Drawer3D.Render(GraphicsDevice, DefaultShader, true);

            // Now draw all of the entities in the game
            DefaultShader.Parameters["ClipPlane0"].SetValue(new Vector4(slicePlane.Normal, slicePlane.D));
            DefaultShader.Parameters["Clipping"].SetValue(true);
            DefaultShader.Parameters["GhostMode"].SetValue(true);
            DrawComponents(gameTime, DefaultShader, Camera.ViewMatrix, ComponentManager.WaterRenderType.None, lastWaterHeight);
            DefaultShader.Parameters["Clipping"].SetValue(false);
            
            if(GameSettings.Default.EnableGlow)
            {
                bloom.Draw(gameTime);
            }

            frameTimer.Update(gameTime);

            if(frameTimer.HasTriggered)
            {
                fps = frameCounter;

                lastFps.Add(fps);
                frameCounter = 0;
                frameTimer.Reset(1.0f);
            }
            else
            {
                frameCounter++;
            }

            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };


            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizerState);

            drawer2D.Render(DwarfGame.SpriteBatch, Camera, GraphicsDevice.Viewport);

            GUI.Render(gameTime, DwarfGame.SpriteBatch, Vector2.Zero);


            bool drawDebugData = GameSettings.Default.DrawDebugData;
            if(drawDebugData)
            {
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "Num Chunks " + ChunkManager.ChunkData.ChunkMap.Values.Count, new Vector2(5, 5), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "Max Viewing Level " + ChunkManager.ChunkData.MaxViewingLevel, new Vector2(5, 20), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "FPS " + Math.Round(fps), new Vector2(5, 35), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "60", new Vector2(5, 150 - 65), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "30", new Vector2(5, 150 - 35), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "10", new Vector2(5, 150 - 15), Color.White);
                for(int i = 0; i < lastFps.Count; i++)
                {
                    DwarfGame.SpriteBatch.Draw(pixel, new Rectangle(30 + i * 2, 150 - (int) lastFps[i], 2, (int) lastFps[i]), new Color(1.0f - lastFps[i] / 60.0f, lastFps[i] / 60.0f, 0.0f, 0.5f));
                }
            }

            Vector3 frustrumNormal = Camera.GetFrustrum().Far.Normal;



            if(Paused)
            {
                Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, "Paused", GUI.DefaultFont, new Vector2(GraphicsDevice.Viewport.Width - 100, 10), Color.White, Color.Black);
            }

            DwarfGame.SpriteBatch.End();
            Master.Render(Game, gameTime, GraphicsDevice);
            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;

            GUI.PostRender(gameTime);

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;



            base.Render(gameTime);
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
            pp.MultiSampleCount = MultiSamples;

            if(bloom != null)
            {
                bloom.sceneRenderTarget = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false,
                    format, pp.DepthStencilFormat, pp.MultiSampleCount,
                    RenderTargetUsage.DiscardContents);
            }
        }
    }

}