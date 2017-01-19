﻿// PlayState.cs
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
using DwarfCorpCore;
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
    public class WorldManager : IDisposable
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

        public static float SlicePlane = 0;

        // The horizontal size of the overworld in pixels
        public static int WorldWidth = 800;

        // The origin of the overworld in pixels [(0, 0, 0) in world space.]
        public static Vector2 WorldOrigin { get; set; }

        // The vertical size of the overworld in pixels
        public static int WorldHeight = 800;

        // The number of voxels along x and z in a chunk
        public static int ChunkWidth { get { return GameSettings.Default.ChunkWidth; } }

        // The number of voxels along y in a chunk.
        public static int ChunkHeight { get { return GameSettings.Default.ChunkHeight; } }

        // The current coordinate of the cursor light
        public static Vector3 CursorLightPos
        {
            get { return LightPositions[0]; }
            set { LightPositions[0] = value; }
        }

        public static Vector3[] LightPositions = new Vector3[16];

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

        public bool UseFXAA
        {
            get { return MultiSamples == -1; }
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

        // Just a helpful 1x1 white pixel texture
        private Texture2D pixel;

        // Draws lines/boxes etc. to the screen
        private Drawer2D drawer2D;

        // A shader which draws fancy light blooming to the screen
        private BloomComponent bloom;

        private FXAA fxaa;

        // Responsible for drawing liquids.
        public static WaterRenderer WaterRenderer;

        // Responsible for drawing the skybox
        public static SkyRenderer Sky;

        // Draws shadow maps
        public static ShadowRenderer Shadows;

        // Used to generate all random numbers in the game.
        public static ThreadSafeRandom Random = new ThreadSafeRandom();

        // Responsible for handling instances of particular primitives (or models)
        // and drawing them to the screen
        public static InstanceManager InstanceManager;

        // Handles loading of game assets
        public ContentManager Content;

        // Reference to XNA Game
        public DwarfGame Game;

        // Interfaces with the graphics card
        public GraphicsDevice GraphicsDevice;

        // Loads the game in the background while a loading message displays
        public Thread LoadingThread { get; set; }

        // When the game is loading, this message is displayed on the screen
        public string LoadingMessage = "";
        // loading messages running across the bottom
        public string LoadingMessageBottom = "";

        private static bool paused_ = false;
        // True if the game's update loop is paused, false otherwise
        public static bool Paused
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
        public static PlanService PlanService = null;

        // Maintains a dictionary of biomes (forest, desert, etc.)
        public static BiomeLibrary BiomeLibrary = new BiomeLibrary();

        // Contains the storm forecast
        public static Weather Weather = new Weather();

        // Maintains a dictionary of particle emitters
        public static ParticleManager ParticleManager
        {
            get { return ComponentManager.ParticleManager; }
            set { ComponentManager.ParticleManager = value; }
        }

        // The current calendar date/time of the game.
        public static WorldTime Time = new WorldTime();

        // Hack to smooth water reflections TODO: Put into water manager
        private float lastWaterHeight = 8.0f;

        private GameFile gameFile;

        public static Point3 WorldSize { get; set; }

        public static AnnouncementManager AnnouncementManager = new AnnouncementManager();

        public static MonsterSpawner MonsterSpawner { get; set; }

        public static Company PlayerCompany
        {
            get { return Master.Faction.Economy.Company; }
        }

        public static Faction PlayerFaction
        {
            get { return Master.Faction; }
        }

        public static Economy PlayerEconomy
        {
            get { return Master.Faction.Economy; }
        }

        public static List<Faction> Natives { get; set; }

        private bool SleepPrompt = false;

        public static CraftLibrary CraftLibrary = null;

        public static int GameID = -1;

        public struct Screenshot
        {
            public string FileName { get; set; }
            public Point Resolution { get; set; }
        }

        public List<Screenshot> Screenshots { get; set; }
        private object ScreenshotLock = new object();

        public bool ShowingWorld { get; set; }

        public GameState gameState;

        public static DwarfGUI GUI;

        // Since world, like many of the other classes, is pretty much a singleton given how many static variables it has
        // this provides singleton access
        public static WorldManager world;

        // event that is called when the world is done loading
        public delegate void OnLoaded();
        public event OnLoaded OnLoadedEvent;

        // event that is called when the player loses in the world
        public delegate void OnLose();
        public event OnLose OnLoseEvent;

        #endregion

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="game">The program currently running</param>
        public WorldManager(DwarfGame Game)
        {
            world = this;
            this.Game = Game;
            Content = Game.Content;
            GraphicsDevice = Game.GraphicsDevice;
            Seed = Random.Next();
            WorldOrigin = new Vector2(WorldWidth / 2, WorldHeight / 2);
            Time = new WorldTime();
        }

        public void Setup(DwarfGUI outerGUI)
        {
            GUI = outerGUI;
            Screenshots = new List<Screenshot>();

            // In this code block we load some stuff that can't be done in a thread
            Game.Graphics.PreferMultiSampling = GameSettings.Default.AntiAliasing > 1;
            // This is some grossness which tries to apply the current graphics settings
            // to the GPU.
            try
            {
                Game.Graphics.ApplyChanges();
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
            Game.Graphics.PreparingDeviceSettings -= GraphicsPreparingDeviceSettings;
            Game.Graphics.PreparingDeviceSettings += GraphicsPreparingDeviceSettings;

            // Now we load everything else in a thread so we can see the progress on the screensaver
            LoadingThread = new Thread(LoadThreaded);
            LoadingThread.Start();
        }

        /// <summary>
        /// Executes the entire game loading sequence, and draws loading messages.
        /// </summary>
        private void LoadThreaded()
        {
            drawer2D = new Drawer2D(Content, GraphicsDevice);
            LoadingMessage = "Waiting for Graphics Device ...";

            WaitForGraphicsDevice();
#if CREATE_CRASH_LOGS
            try
#endif
            {
                LoadingMessage = "Initializing ...";
                InitializeStaticData(CompanyMakerState.CompanyName, CompanyMakerState.CompanyMotto,
                    CompanyMakerState.CompanyLogo,
                    CompanyMakerState.CompanyColor, Natives);

                LoadingMessage = "Creating Planner ...";
                PlanService = new PlanService();

                LoadingMessage = "Creating Particles ...";
                CreateParticles();

                LoadingMessage = "Creating Sky...";
                CreateSky();

                LoadingMessage = "Creating Shadows...";
                CreateShadows();

                LoadingMessage = "Creating Liquids ...";
                CreateLiquids();

                LoadingMessage = "Generating Initial Terrain Chunks ...";
                GenerateInitialChunks();

                LoadingMessage = "Creating GameMaster ...";
                CreateGameMaster();

                LoadingMessage = "Embarking ...";
                CreateInitialEmbarkment();

                LoadingMessage = "Presimulating ...";
                ShowingWorld = false;
                if (string.IsNullOrEmpty(ExistingFile))
                {
                    GenerateInitialObjects();
                }
                OnLoadedEvent();

                Thread.Sleep(1000);
                ShowingWorld = true;
                LoadingMessage = "Complete.";
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif
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

        /// <summary>
        /// Generates a random set of dwarves in the given chunk.
        /// </summary>
        /// <param name="c">The chunk the dwarves belong to</param>
        public void CreateInitialDwarves(VoxelChunk c)
        {
            Vector3 g = c.WorldToGrid(Camera.Position);
            // Find the height of the world at the camera
            float h = c.GetFilledVoxelGridHeightAt((int)g.X, ChunkHeight - 1, (int)g.Z);

            // This is done just to make sure the camera is in the correct place.
            Camera.UpdateBasisVectors();
            Camera.UpdateProjectionMatrix();
            Camera.UpdateViewMatrix();

            foreach (string ent in InitialEmbark.Party)
            {
                Vector3 dorfPos = new Vector3(Camera.Position.X + (float)Random.NextDouble(), h + 10,
                    Camera.Position.Z + (float)Random.NextDouble());
                Physics creat = (Physics)EntityFactory.CreateEntity<Physics>(ent, dorfPos);
                creat.Velocity = new Vector3(1, 0, 0);
            }

            Camera.Target = new Vector3(Camera.Position.X, h + 10, Camera.Position.Z + 10);
            Camera.Phi = -(float)Math.PI * 0.3f;
        }

        /// <summary>
        /// Creates a bunch of stuff (such as the biome library, primitive library etc.) which won't change
        /// from game to game.
        /// </summary>
        public void InitializeStaticData(string companyName, string companyMotto, NamedImageFrame companyLogo,
            Color companyColor, List<Faction> natives)
        {
            CompositeLibrary.Initialize();
            CraftLibrary = new CraftLibrary();

            new PrimitiveLibrary(GraphicsDevice, Content);
            InstanceManager = new InstanceManager();

            EntityFactory.InstanceManager = InstanceManager;
            InstanceManager.CreateStatics(Content);

            Color[] white = new Color[1];
            white[0] = Color.White;
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(white);

            Tilesheet = TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles);
            AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            DefaultShader = Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders);
            DefaultShader.Parameters["xFogStart"].SetValue(40.0f);
            DefaultShader.Parameters["xFogEnd"].SetValue(80.0f);

            VoxelLibrary = new VoxelLibrary();
            VoxelLibrary.InitializeDefaultLibrary(GraphicsDevice, Tilesheet);

            bloom = new BloomComponent(Game)
            {
                Settings = BloomSettings.PresetSettings[5]
            };
            bloom.Initialize();


            fxaa = new FXAA();
            fxaa.Initialize();

            SoundManager.Content = Content;
            if (PlanService != null)
                PlanService.Restart();

            ComponentManager = new ComponentManager(this, companyName, companyMotto, companyLogo, companyColor, natives);
            ComponentManager.RootComponent = new Body("root", null, Matrix.Identity, Vector3.Zero, Vector3.Zero, false);
            Vector3 origin = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y);
            Vector3 extents = new Vector3(1500, 1500, 1500);
            ComponentManager.CollisionManager = new CollisionManager(new BoundingBox(origin - extents, origin + extents));
            ComponentManager.Diplomacy = new Diplomacy(ComponentManager.Factions);
            ComponentManager.Diplomacy.Initialize(Time.CurrentDate);

            JobLibrary.Initialize();
            MonsterSpawner = new MonsterSpawner();
            EntityFactory.Initialize();
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
            if (fileExists)
            {
                LoadingMessage = "Loading " + ExistingFile;
                gameFile = new GameFile(ExistingFile, true);
                Sky.TimeOfDay = gameFile.Data.Metadata.TimeOfDay;
                WorldManager.Time = gameFile.Data.Metadata.Time;
                WorldOrigin = gameFile.Data.Metadata.WorldOrigin;
                WorldScale = gameFile.Data.Metadata.WorldScale;
                GameSettings.Default.ChunkWidth = gameFile.Data.Metadata.ChunkWidth;
                GameSettings.Default.ChunkHeight = gameFile.Data.Metadata.ChunkHeight;

                if (gameFile.Data.Metadata.OverworldFile != null && gameFile.Data.Metadata.OverworldFile != "flat")
                {
                    LoadingMessage = "Loading world " + gameFile.Data.Metadata.OverworldFile;
                    Overworld.Name = gameFile.Data.Metadata.OverworldFile;
                    DirectoryInfo worldDirectory =
                        Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Worlds" +
                                                  ProgramData.DirChar + Overworld.Name);
                    OverworldFile overWorldFile =
                        new OverworldFile(
                            worldDirectory.FullName + ProgramData.DirChar + "world." + OverworldFile.CompressedExtension,
                            true, true);
                    Overworld.Map = overWorldFile.Data.CreateMap();
                    Overworld.Name = overWorldFile.Data.Name;
                    WorldWidth = Overworld.Map.GetLength(1);
                    WorldHeight = Overworld.Map.GetLength(0);
                }
                else
                {
                    LoadingMessage = "Generating flat world..";
                    Overworld.CreateUniformLand(GraphicsDevice);
                }

                GameID = gameFile.Data.GameID;
            }
            else
            {
                GameID = Random.Next(0, 1024);
            }


            ChunkGenerator = new ChunkGenerator(VoxelLibrary, Seed, 0.02f, ChunkHeight / 2.0f)
            {
                SeaLevel = SeaLevel
            };

            Vector3 globalOffset = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y) * WorldScale;

            if (fileExists)
            {
                globalOffset /= WorldScale;
            }


            // If the file exists, we get the camera's pose from the file.
            // Otherwise, we set it to a pose above the center of the world (0, 0, 0)
            // facing down slightly.
            Camera = fileExists
                ? gameFile.Data.Camera
                : new OrbitCamera(0, 0, 10f, new Vector3(ChunkWidth, ChunkHeight - 1.0f, ChunkWidth) + globalOffset,
                    new Vector3(0, 50, 0) + globalOffset, MathHelper.PiOver4, AspectRatio, 0.1f,
                    GameSettings.Default.VertexCullDistance);

            Drawer3D.Camera = Camera;

            // Creates the terrain management system.
            ChunkManager = new ChunkManager(Content, (uint)ChunkWidth, (uint)ChunkHeight, (uint)ChunkWidth, Camera,
                GraphicsDevice,
                ChunkGenerator, WorldSize.X, WorldSize.Y, WorldSize.Z);

            // Trying to determine the global offset from overworld coordinates (pixels in the overworld) to
            // voxel coordinates.
            globalOffset = ChunkManager.ChunkData.RoundToChunkCoords(globalOffset);
            globalOffset.X *= ChunkWidth;
            globalOffset.Y *= ChunkHeight;
            globalOffset.Z *= ChunkWidth;

            // If there's no file, we have to offset the camera relative to the global offset.
            if (!fileExists)
            {
                WorldOrigin = new Vector2(globalOffset.X, globalOffset.Z);
                Camera.Position = new Vector3(0, 10, 0) + globalOffset;
                Camera.Target = new Vector3(0, 10, 1) + globalOffset;
                Camera.Radius = 0.01f;
                Camera.Phi = -1.57f;
            }


            // If there's no file, we have to initialize the first chunk coordinate
            if (gameFile == null)
            {
                ChunkManager.GenerateInitialChunks(
                    ChunkManager.ChunkData.GetChunkID(new Vector3(0, 0, 0) + globalOffset), ref LoadingMessage);
            }
            // Otherwise, we just load all the chunks from the file.
            else
            {
                LoadingMessage = "Loading Chunks from Game File";
                ChunkManager.ChunkData.LoadFromFile(gameFile, ref LoadingMessage);
            }

            // If there's no file, for some reason we modify the camera position...
            // TODO: Figure out why the camera keeps needing to be reset.
            if (!fileExists)
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

        public static float SeaLevel { get; set; }


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
                    DrawSky(new DwarfTime(), Camera.ViewMatrix, 1.0f);
                    Draw3DThings(new DwarfTime(), DefaultShader, Camera.ViewMatrix);
                    DrawComponents(new DwarfTime(), DefaultShader, Camera.ViewMatrix,
                        ComponentManager.WaterRenderType.None, 0);
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
                Opactiy = 0.8f,
                Reflection = 1.0f,
                WaveHeight = 0.1f,
                WaveLength = 0.05f,
                WindForce = 0.001f,
                BumpTexture = TextureManager.GetTexture(ContentPaths.Terrain.water_normal),
                BaseTexture = TextureManager.GetTexture(ContentPaths.Terrain.cartoon_water),
                MinOpacity = 0.4f,
                RippleColor = new Vector4(0.6f, 0.6f, 0.6f, 0.0f),
                FlatColor = new Vector4(0.3f, 0.3f, 0.9f, 1.0f)
            };
            WaterRenderer.AddLiquidAsset(waterAsset);


            LiquidAsset lavaAsset = new LiquidAsset
            {
                Type = LiquidType.Lava,
                Opactiy = 0.95f,
                Reflection = 0.0f,
                WaveHeight = 0.1f,
                WaveLength = 0.05f,
                WindForce = 0.001f,
                MinOpacity = 0.8f,
                BumpTexture = TextureManager.GetTexture(ContentPaths.Terrain.water_normal),
                BaseTexture = TextureManager.GetTexture(ContentPaths.Terrain.lava),
                RippleColor = new Vector4(0.5f, 0.4f, 0.04f, 0.0f),
                FlatColor = new Vector4(0.9f, 0.7f, 0.2f, 1.0f)
            };

            WaterRenderer.AddLiquidAsset(lavaAsset);
        }


        public void CreateShadows()
        {
            Shadows = new ShadowRenderer(GraphicsDevice, 1024, 1024);
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

        public void CreateGameMaster()
        {
            // if we are loading reinitialize a bunch of stuff to make sure the game master is created correctly
            if (!string.IsNullOrEmpty(ExistingFile))
            {
                InstanceManager.Clear();
                gameFile.LoadComponents(ExistingFile);
                ComponentManager = gameFile.Data.Components;
                GameComponent.ResetMaxGlobalId(ComponentManager.GetMaxComponentID() + 1);
                Sky.TimeOfDay = gameFile.Data.Metadata.TimeOfDay;
                Time = gameFile.Data.Metadata.Time;
                WorldOrigin = gameFile.Data.Metadata.WorldOrigin;
                WorldScale = gameFile.Data.Metadata.WorldScale;
                GameSettings.Default.ChunkWidth = gameFile.Data.Metadata.ChunkWidth;
                GameSettings.Default.ChunkHeight = gameFile.Data.Metadata.ChunkHeight;
            }
            Master = new GameMaster(ComponentManager.Factions.Factions["Player"], Game, ComponentManager, ChunkManager,
                Camera, GraphicsDevice, GUI);
        }

        /// <summary>
        /// Creates the balloon, the dwarves, and the initial balloon port.
        /// </summary>
        public void CreateInitialEmbarkment()
        {
            // If no file exists, we have to create the balloon and balloon port.
            if (string.IsNullOrEmpty(ExistingFile))
            {
                VoxelChunk c = ChunkManager.ChunkData.GetVoxelChunkAtWorldLocation(Camera.Position);
                BalloonPort port = GenerateInitialBalloonPort(Master.Faction.RoomBuilder, ChunkManager,
                    Camera.Position.X, Camera.Position.Z, 3);
                CreateInitialDwarves(c);
                WorldManager.PlayerFaction.Economy.CurrentMoney = InitialEmbark.Money;

                foreach (var res in InitialEmbark.Resources)
                {
                    PlayerFaction.AddResources(new ResourceAmount(res.Key, res.Value));
                }
                EntityFactory.CreateBalloon(
                    new Vector3(Camera.Position.X, ChunkHeight - 2, Camera.Position.Z) + new Vector3(0, 1000, 0),
                    new Vector3(Camera.Position.X, ChunkHeight - 2, Camera.Position.Z), ComponentManager, Content,
                    GraphicsDevice, new ShipmentOrder(0, null), Master.Faction);
            }
        }

        public void WaitForGraphicsDevice()
        {
            while (GraphicsDevice == null)
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(1000);
        }

        private void GenerateInitialObjects()
        {
            foreach (var chunk in ChunkManager.ChunkData.ChunkMap)
            {
                ChunkManager.ChunkGen.GenerateVegetation(chunk.Value, ComponentManager, Content, GraphicsDevice);
                ChunkManager.ChunkGen.GenerateFauna(chunk.Value, ComponentManager, Content, GraphicsDevice,
                    ComponentManager.Factions);
            }
        }

        /// <summary>
        /// Creates a flat, wooden balloon port for the balloon to land on, and Dwarves to sit on.
        /// </summary>
        /// <param name="roomDes">The player's BuildRoom designator (so that we can create a balloon port)</param>
        /// <param name="chunkManager">The terrain handler</param>
        /// <param name="x">The position of the center of the balloon port</param>
        /// <param name="z">The position of the center of the balloon port</param>
        /// <param name="size">The size of the (square) balloon port in voxels on a side</param>
        public BalloonPort GenerateInitialBalloonPort(RoomBuilder roomDes, ChunkManager chunkManager, float x, float z,
            int size)
        {
            Vector3 pos = new Vector3(x, ChunkHeight - 1, z);

            // First, compute the maximum height of the terrain in a square window.
            int averageHeight = 0;
            int count = 0;
            for (int dx = -size; dx <= size; dx++)
            {
                for (int dz = -size; dz <= size; dz++)
                {
                    Vector3 worldPos = new Vector3(pos.X + dx, pos.Y, pos.Z + dz);
                    VoxelChunk chunk = chunkManager.ChunkData.GetVoxelChunkAtWorldLocation(worldPos);

                    if (chunk == null)
                    {
                        continue;
                    }

                    Vector3 gridPos = chunk.WorldToGrid(worldPos);
                    int h = chunk.GetFilledHeightOrWaterAt((int)gridPos.X + dx, (int)gridPos.Y, (int)gridPos.Z + dz);

                    if (h > 0)
                    {
                        averageHeight += h;
                        count++;
                    }
                }
            }

            averageHeight = (int)Math.Round(((float)averageHeight / (float)count));


            // Next, create the balloon port by deciding which voxels to fill.
            List<Voxel> designations = new List<Voxel>();
            for (int dx = -size; dx <= size; dx++)
            {
                for (int dz = -size; dz <= size; dz++)
                {
                    Vector3 worldPos = new Vector3(pos.X + dx, pos.Y, pos.Z + dz);
                    VoxelChunk chunk = chunkManager.ChunkData.GetVoxelChunkAtWorldLocation(worldPos);

                    if (chunk == null)
                    {
                        continue;
                    }

                    Vector3 gridPos = chunk.WorldToGrid(worldPos);
                    int h = chunk.GetFilledVoxelGridHeightAt((int)gridPos.X, (int)gridPos.Y, (int)gridPos.Z);

                    if (h == -1)
                    {
                        continue;
                    }

                    for (int y = averageHeight; y < h; y++)
                    {
                        Voxel v = chunk.MakeVoxel((int)gridPos.X, y, (int)gridPos.Z);
                        v.Type = VoxelLibrary.GetVoxelType(0);
                        chunk.Manager.ChunkData.Reveal(v);
                        chunk.Data.Water[v.Index].WaterLevel = 0;
                    }

                    if (averageHeight < h)
                    {
                        h = averageHeight;
                    }

                    bool isPosX = (dx == size && dz == 0);
                    bool isPosZ = (dz == size & dx == 0);
                    bool isNegX = (dx == -size && dz == 0);
                    bool isNegZ = (dz == -size && dz == 0);
                    bool isSide = (isPosX || isNegX || isPosZ || isNegZ);

                    Vector3 offset = Vector3.Zero;

                    if (isSide)
                    {
                        if (isPosX)
                        {
                            offset = Vector3.UnitX;
                        }
                        else if (isPosZ)
                        {
                            offset = Vector3.UnitZ;
                        }
                        else if (isNegX)
                        {
                            offset = -Vector3.UnitX;
                        }
                        else if (isNegZ)
                        {
                            offset = -Vector3.UnitZ;
                        }
                    }

                    // Fill from the top height down to the bottom.
                    for (int y = h - 1; y < averageHeight; y++)
                    {
                        Voxel v = chunk.MakeVoxel((int)gridPos.X, y, (int)gridPos.Z);
                        v.Type = VoxelLibrary.GetVoxelType("Scaffold");
                        chunk.Data.Water[v.Index].WaterLevel = 0;
                        v.Chunk = chunk;
                        v.Chunk.NotifyTotalRebuild(!v.IsInterior);

                        if (y == averageHeight - 1)
                        {
                            designations.Add(v);
                        }

                        if (isSide)
                        {
                            Voxel ladderVox = new Voxel();

                            Vector3 center = new Vector3(worldPos.X, y, worldPos.Z) + offset + Vector3.One * .5f;
                            if (chunk.Manager.ChunkData.GetVoxel(center, ref ladderVox) && ladderVox.IsEmpty)
                            {
                                EntityFactory.CreateEntity<Ladder>("Wooden Ladder", center);
                            }
                        }
                    }
                }
            }


            // Actually create the BuildRoom.
            BalloonPort toBuild = new BalloonPort(PlayerFaction, designations, chunkManager);
            BuildRoomOrder buildDes = new BuildRoomOrder(toBuild, roomDes.Faction);
            buildDes.Build(true);
            roomDes.DesignatedRooms.Add(toBuild);
            return toBuild;
        }


        public bool IsCameraUnderwater()
        {
            WaterCell water = ChunkManager.ChunkData.GetWaterCellAtLocation(Camera.Position);
            return water.WaterLevel > 0;
        }

        /// <summary>
        /// Creates all the static particle emitters used in the game.
        /// </summary>
        public void CreateParticles()
        {
            ParticleManager = new ParticleManager(ComponentManager);

            // Smoke
            EmitterData puff = ParticleManager.CreatePuffLike("puff", new SpriteSheet(ContentPaths.Particles.puff),
                Point.Zero, BlendState.NonPremultiplied);
            ParticleManager.RegisterEffect("puff", puff);

            EmitterData smoke = ParticleManager.CreatePuffLike("smoke", new SpriteSheet(ContentPaths.Particles.puff),
                Point.Zero, BlendState.NonPremultiplied);
            smoke.ConstantAccel = Vector3.Up * 2;
            smoke.GrowthSpeed = -0.1f;
            smoke.MaxAngular = 0.01f;
            smoke.MinAngular = -0.01f;
            smoke.MinScale = 0.2f;
            smoke.MaxScale = 0.6f;
            smoke.LinearDamping = 0.9999f;
            smoke.EmissionRadius = 0.1f;
            smoke.CollidesWorld = true;
            smoke.EmissionSpeed = 0.2f;

            ParticleManager.RegisterEffect("smoke", smoke);

            // Bubbles
            EmitterData bubble = ParticleManager.CreatePuffLike("splash2",
                new SpriteSheet(ContentPaths.Particles.splash2), Point.Zero, BlendState.NonPremultiplied);
            bubble.ConstantAccel = new Vector3(0, 5, 0);
            bubble.EmissionSpeed = 3;
            bubble.LinearDamping = 0.9f;
            bubble.GrowthSpeed = -2.5f;
            bubble.MinScale = 1.5f;
            bubble.MaxScale = 2.5f;
            bubble.ParticleDecay = 1.5f;
            bubble.HasLighting = false;
            ParticleManager.RegisterEffect("splash2", bubble);

            EmitterData splat = ParticleManager.CreatePuffLike("splat", new SpriteSheet(ContentPaths.Particles.splat),
                Point.Zero, BlendState.NonPremultiplied);
            splat.ConstantAccel = Vector3.Zero;
            splat.EmissionRadius = 0.01f;
            splat.EmissionSpeed = 0.0f;
            splat.GrowthSpeed = -1.75f;
            splat.MinAngle = -0.0f;
            splat.MaxAngle = 0.0f;
            splat.MinAngular = -0.01f;
            splat.MaxAngular = 0.01f;
            splat.MaxParticles = 500;
            splat.MinScale = 0.05f;
            splat.ParticleDecay = 1.5f;
            splat.HasLighting = false;
            splat.MaxScale = 1.1f;
            splat.EmitsLight = false;
            ParticleManager.RegisterEffect("splat", splat);

            EmitterData heart = ParticleManager.CreatePuffLike("heart", new SpriteSheet(ContentPaths.Particles.heart),
                Point.Zero, BlendState.NonPremultiplied);
            heart.MinAngle = 0.01f;
            heart.MaxAngle = 0.01f;
            heart.MinAngular = 0.0f;
            heart.MinAngular = 0.0f;
            heart.ConstantAccel = Vector3.Up * 20;
            ParticleManager.RegisterEffect("heart", heart);

            // Fire
            SpriteSheet fireSheet = new SpriteSheet(ContentPaths.Particles.more_flames, 32, 32);
            EmitterData flame = ParticleManager.CreatePuffLike("flame", fireSheet, Point.Zero, BlendState.NonPremultiplied);
            flame.ConstantAccel = Vector3.Up * 20;
            flame.EmissionSpeed = 2;
            flame.GrowthSpeed = -1.9f;
            flame.MinAngle = -0.2f;
            flame.MaxAngle = 0.2f;
            flame.MinAngular = -0.01f;
            flame.MaxAngular = 0.01f;
            flame.MaxParticles = 500;
            flame.MinScale = 0.2f;
            flame.HasLighting = false;
            flame.MaxScale = 2.0f;
            flame.EmitsLight = true;
            flame.Blend = new BlendState()
            {
                AlphaSourceBlend = Blend.One,
                AlphaDestinationBlend = Blend.InverseSourceAlpha,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                ColorSourceBlend = Blend.One
            };
            ParticleManager.RegisterEffect("flame", flame, flame.Clone(fireSheet, new Point(1, 0)),
                flame.Clone(fireSheet, new Point(2, 0)), flame.Clone(fireSheet, new Point(3, 0)));

            EmitterData greenFlame = ParticleManager.CreatePuffLike("green_flame",
                new SpriteSheet(ContentPaths.Particles.green_flame), new Point(0, 0), BlendState.Additive);
            greenFlame.ConstantAccel = Vector3.Up * 20;
            greenFlame.EmissionSpeed = 2;
            greenFlame.GrowthSpeed = -1.9f;
            greenFlame.MinAngle = -0.2f;
            greenFlame.MaxAngle = 0.2f;
            greenFlame.MinAngular = -0.01f;
            greenFlame.MaxAngular = 0.01f;
            greenFlame.HasLighting = false;

            ParticleManager.RegisterEffect("green_flame", greenFlame);

            List<Point> frm2 = new List<Point>
            {
                new Point(0, 0)
            };

            // Leaves
            EmitterData testData2 = new EmitterData
            {
                Animation =
                    new Animation(GraphicsDevice, new SpriteSheet(ContentPaths.Particles.leaf), "leaf", 32, 32, frm2,
                        true, Color.White, 1.0f, 1.0f, 1.0f, false),
                ConstantAccel = new Vector3(0, -10, 0),
                LinearDamping = 0.95f,
                AngularDamping = 0.99f,
                EmissionFrequency = 1.0f,
                EmissionRadius = 2.0f,
                EmissionSpeed = 5.0f,
                GrowthSpeed = -0.5f,
                MaxAngle = 3.14159f,
                MinAngle = 0.0f,
                MaxParticles = 1000,
                MaxScale = 0.5f,
                MinScale = 0.1f,
                MinAngular = -5.0f,
                MaxAngular = 5.0f,
                ParticleDecay = 0.5f,
                ParticlesPerFrame = 0,
                Sleeps = true,
                ReleaseOnce = true,
                CollidesWorld = true,
                Texture = TextureManager.GetTexture(ContentPaths.Particles.leaf)
            };

            ParticleManager.RegisterEffect("Leaves", testData2);

            // Various resource explosions
            ParticleManager.CreateGenericExplosion(ContentPaths.Particles.dirt_particle, "dirt_particle");
            EmitterData stars = ParticleManager.CreatePuffLike("star_particle",
                new SpriteSheet(ContentPaths.Particles.star_particle), new Point(0, 0), BlendState.Additive);
            stars.MinAngle = -0.1f;
            stars.MaxAngle = 0.1f;
            stars.MinScale = 0.2f;
            stars.MaxScale = 0.5f;
            stars.AngularDamping = 0.99f;
            stars.LinearDamping = 0.999f;
            stars.GrowthSpeed = -0.8f;
            stars.EmissionFrequency = 5;
            stars.CollidesWorld = false;
            stars.HasLighting = false;

            ParticleManager.RegisterEffect("star_particle", stars);

            ParticleManager.CreateGenericExplosion(ContentPaths.Particles.stone_particle, "stone_particle");
            ParticleManager.CreateGenericExplosion(ContentPaths.Particles.sand_particle, "sand_particle");
            ParticleManager.CreateGenericExplosion(ContentPaths.Particles.snow_particle, "snow_particle");
            ParticleManager.CreateGenericExplosion(ContentPaths.Particles.dirt_particle, "dirt_particle");

            SpriteSheet bloodSheet = new SpriteSheet(ContentPaths.Particles.gibs, 32, 32);
            // Blood explosion
            // ParticleEmitter b = ParticleManager.CreateGenericExplosion(ContentPaths.Particles.blood_particle, "blood_particle").Emitters[0];
            EmitterData b = ParticleManager.CreateExplosionLike("blood_particle", bloodSheet, Point.Zero,
                BlendState.NonPremultiplied);
            b.MinScale = 0.75f;
            b.MaxScale = 1.0f;
            b.Damping = 0.1f;
            b.GrowthSpeed = -0.8f;
            b.RotatesWithVelocity = true;

            ParticleManager.RegisterEffect("blood_particle", b);
            ParticleManager.RegisterEffect("gibs", b.Clone(bloodSheet, new Point(1, 0)),
                b.Clone(bloodSheet, new Point(2, 0)), b.Clone(bloodSheet, new Point(3, 0)));

            ParticleManager.RegisterEffect("rain", new EmitterData()
            {
                AngularDamping = 1.0f,
                Animation = new Animation(ContentPaths.Particles.raindrop, 16, 16, 0, 0),
                Texture = Content.Load<Texture2D>(ContentPaths.Particles.raindrop),
                ParticleDecay = 9.0f,
                Blend = BlendState.NonPremultiplied,
                CollidesWorld = false,
                ConstantAccel = Vector3.Zero,
                Damping = 1.0f,
                EmissionFrequency = 9999.0f,
                EmissionRadius = 0.001f,
                EmissionSpeed = 0.0f,
                EmitsLight = false,
                GrowthSpeed = 0.0f,
                ReleaseOnce = false,
                MinAngle = 0,
                MaxAngle = 0,
                MinAngular = 0,
                MaxAngular = 0,
                MinScale = 0.45f,
                MaxScale = 0.5f,
                MaxParticles = 500,
                HasLighting = true,
                UseManualControl = true
            });

            ParticleManager.RegisterEffect("snowflake", new EmitterData()
            {
                AngularDamping = 1.0f,
                Animation = new Animation(ContentPaths.Particles.snow_particle, 8, 8, 0, 0),
                Texture = Content.Load<Texture2D>(ContentPaths.Particles.snow_particle),
                ParticleDecay = 9.0f,
                Blend = BlendState.NonPremultiplied,
                CollidesWorld = false,
                ConstantAccel = Vector3.Zero,
                Damping = 1.0f,
                EmissionFrequency = 9999.0f,
                EmissionRadius = 0.001f,
                EmissionSpeed = 0.0f,
                EmitsLight = false,
                GrowthSpeed = 0.0f,
                ReleaseOnce = false,
                MinAngle = 0,
                MaxAngle = 0,
                MinAngular = 0,
                MaxAngular = 0,
                MinScale = 0.2f,
                MaxScale = 0.25f,
                MaxParticles = 500,
                HasLighting = true,
                UseManualControl = true
            });
        }

        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public void Update(DwarfTime gameTime)
        {
            if (FastForwardToDay)
            {
                if (Time.IsDay())
                {
                    FastForwardToDay = false;
                    foreach (CreatureAI minion in Master.Faction.Minions)
                    {
                        minion.Status.Energy.CurrentValue = minion.Status.Energy.MaxValue;
                    }
                    Master.ToolBar.SpeedButton.SetSpeed(1);
                    Time.Speed = 100;
                }
                else
                {
                    Master.ToolBar.SpeedButton.SetSpecialSpeed(3);
                    Time.Speed = 1000;
                }
            }

            //Drawer3D.DrawPlane(0, Camera.Position.X - 1500, Camera.Position.Z - 1500, Camera.Position.X + 1500, Camera.Position.Z + 1500, Color.Black);
            FillClosestLights(gameTime);
            IndicatorManager.Update(gameTime);
            AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            Camera.AspectRatio = AspectRatio;

            Camera.Update(gameTime, WorldManager.ChunkManager);

            if (KeyManager.RotationEnabled())
                Mouse.SetPosition(Game.GraphicsDevice.Viewport.Width / 2,
                    Game.GraphicsDevice.Viewport.Height / 2);

            Master.Update(Game, gameTime);
            // If not paused, we want to just update the rest of the game.
            if (!Paused)
            {
                Time.Update(gameTime);
                ComponentManager.Diplomacy.Update(gameTime, Time.CurrentDate);
                ComponentManager.Update(gameTime, ChunkManager, Camera);
                Sky.TimeOfDay = Time.GetSkyLightness();

                Sky.CosTime = (float)(Time.GetTotalHours() * 2 * Math.PI / 24.0f);
                DefaultShader.Parameters["xTimeOfDay"].SetValue(Sky.TimeOfDay);
                MonsterSpawner.Update(gameTime);
                bool allAsleep = Master.AreAllEmployeesAsleep();
                if (SleepPrompt && allAsleep && !FastForwardToDay && Time.IsNight())
                {
                    Dialog sleepingPrompt = Dialog.Popup(GUI, "Employees Asleep",
                        "All of your employees are asleep. Skip to daytime?", Dialog.ButtonType.OkAndCancel);
                    SleepPrompt = false;
                    sleepingPrompt.OnClosed += sleepingPrompt_OnClosed;
                }
                else if (!allAsleep)
                {
                    SleepPrompt = true;
                }
            }

            // These things are updated even when the game is paused
            ChunkManager.Update(gameTime, Camera, GraphicsDevice);
            InstanceManager.Update(gameTime, Camera, GraphicsDevice);

            SoundManager.Update(gameTime, Camera);
            Weather.Update();

            // Make sure that the slice slider snaps to the current viewing level (an integer)
            //if(!LevelSlider.IsMouseOver)
            {
                //   LevelSlider.SliderValue = ChunkManager.ChunkData.MaxViewingLevel;
            }
        }

        private void sleepingPrompt_OnClosed(Dialog.ReturnStatus status)
        {
            if (status == Dialog.ReturnStatus.Ok)
            {
                FastForwardToDay = true;
            }
        }

        public bool FastForwardToDay { get; set; }
        public static Embarkment InitialEmbark { get; set; }

        public void Quit()
        {
            ChunkManager.Destroy();
            ComponentManager.RootComponent.Delete();
            GC.Collect();
            PlanService.Die();
        }

        public delegate void SaveCallback(bool success, Exception e);

        public void Save(string filename, SaveCallback callback = null)
        {
            Paused = true;
            WaitState waitforsave = new WaitState(Game, "SaveWait", gameState.StateManager,
                () => SaveThreadRoutine(filename), GUI);
            if (callback != null)
                waitforsave.OnFinished += (bool b, WaitStateException e) => callback(b, e);
            gameState.StateManager.PushState(waitforsave);
        }

        private bool SaveThreadRoutine(string filename)
        {
            try
            {
                DirectoryInfo worldDirectory =
                    Directory.CreateDirectory(DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Worlds" +
                                              Path.DirectorySeparatorChar + Overworld.Name);

                OverworldFile file = new OverworldFile(Game.GraphicsDevice, Overworld.Map, Overworld.Name);
                file.WriteFile(
                    worldDirectory.FullName + Path.DirectorySeparatorChar + "world." + OverworldFile.CompressedExtension,
                    true, true);
                file.SaveScreenshot(worldDirectory.FullName + Path.DirectorySeparatorChar + "screenshot.png");

                gameFile = new GameFile(Overworld.Name, GameID);
                gameFile.WriteFile(
                    DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Saves" + Path.DirectorySeparatorChar +
                    filename, true);

                lock (ScreenshotLock)
                {
                    Screenshots.Add(new Screenshot()
                    {
                        FileName = DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Saves" +
                                   Path.DirectorySeparatorChar + filename + Path.DirectorySeparatorChar +
                                   "screenshot.png",
                        Resolution = new Point(GraphicsDevice.Viewport.Width / 4, GraphicsDevice.Viewport.Height / 4)
                    });
                }
            }
            catch (Exception exception)
            {
                throw new WaitStateException(exception.Message);
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
        /// <param name="cubeEffect">The textured shader</param>
        /// <param name="view">The view matrix of the camera</param> 
        public void Draw3DThings(DwarfTime gameTime, Effect cubeEffect, Matrix view)
        {
            Matrix viewMatrix = Camera.ViewMatrix;
            Camera.ViewMatrix = view;

            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            cubeEffect.Parameters["xView"].SetValue(view);
            cubeEffect.Parameters["xProjection"].SetValue(Camera.ProjectionMatrix);
            cubeEffect.CurrentTechnique = cubeEffect.Techniques["Textured"];
            cubeEffect.Parameters["Clipping"].SetValue(1);
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            ChunkManager.Render(Camera, gameTime, GraphicsDevice, cubeEffect, Matrix.Identity);

            if (Master.CurrentToolMode == GameMaster.ToolMode.Build)
            {
                cubeEffect.Parameters["xView"].SetValue(view);
                cubeEffect.Parameters["xProjection"].SetValue(Camera.ProjectionMatrix);
                cubeEffect.CurrentTechnique = cubeEffect.Techniques["Textured"];
                GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Master.Faction.WallBuilder.Render(gameTime, GraphicsDevice, cubeEffect);
                Master.Faction.CraftBuilder.Render(gameTime, GraphicsDevice, cubeEffect);
            }
            Camera.ViewMatrix = viewMatrix;
            cubeEffect.Parameters["Clipping"].SetValue(1);
        }


        /// <summary>
        /// Draws all of the game entities
        /// </summary>
        /// <param name="gameTime">The current time</param>
        /// <param name="effect">The shader</param>
        /// <param name="view">The view matrix</param>
        /// <param name="waterRenderType">Whether we are rendering for reflection/refraction or nothing</param>
        /// <param name="waterLevel">The estimated height of water</param>
        public void DrawComponents(DwarfTime gameTime, Effect effect, Matrix view,
            ComponentManager.WaterRenderType waterRenderType, float waterLevel)
        {
            if (!WaterRenderer.DrawComponentsReflected && waterRenderType == ComponentManager.WaterRenderType.Reflective)
                return;
            effect.Parameters["xView"].SetValue(view);
            ComponentManager.Render(gameTime, ChunkManager, Camera, DwarfGame.SpriteBatch, GraphicsDevice, effect,
                waterRenderType, waterLevel);
            bool reset = waterRenderType == ComponentManager.WaterRenderType.None;
            InstanceManager.Render(GraphicsDevice, effect, Camera, reset);
        }

        /// <summary>
        /// Draws the sky box
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="view">The camera view matrix</param>
		/// <param name="scale">The scale for the sky drawing</param>
        public void DrawSky(DwarfTime time, Matrix view, float scale)
        {
            Matrix oldView = Camera.ViewMatrix;
            Camera.ViewMatrix = view;
            Sky.Render(time, GraphicsDevice, Camera, scale);
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Camera.ViewMatrix = oldView;
        }

        public void RenderUninitialized(DwarfTime gameTime, String tip = null)
        {
            Render(gameTime);
        }

        /// <summary>
        /// If the game is not loaded yet, just draws a loading message centered
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public void RenderScreenSaverMessages(DwarfTime gameTime)
        {
            DwarfGame.SpriteBatch.Begin();
            float t = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2.0f) + 1.0f) * 0.5f + 0.5f;
            Color toDraw = new Color(t, t, t);
            SpriteFont font = Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
            Vector2 measurement = Datastructures.SafeMeasure(font, LoadingMessage);
            Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, LoadingMessage, font,
                new Vector2(GraphicsDevice.Viewport.Width / 2 - measurement.X / 2,
                    GraphicsDevice.Viewport.Height / 2), toDraw, new Color(50, 50, 50));

            if (!string.IsNullOrEmpty(LoadingMessageBottom))
            {
                Vector2 tipMeasurement = Datastructures.SafeMeasure(font, LoadingMessageBottom);
                Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, "Tip: " + LoadingMessageBottom, font,
                    new Vector2(GraphicsDevice.Viewport.Width / 2 - tipMeasurement.X / 2,
                        GraphicsDevice.Viewport.Height - tipMeasurement.Y * 2), toDraw, new Color(50, 50, 50));
            }
            DwarfGame.SpriteBatch.End();
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
            // If we are not ready to show the world then just display the loading text
            if (!ShowingWorld)
            {
                RenderScreenSaverMessages(gameTime);
                return;
            }

            // Controls the sky fog
            float x = (1.0f - Sky.TimeOfDay);
            x = x * x;
            DefaultShader.Parameters["xFogColor"].SetValue(new Vector3(0.32f * x, 0.58f * x, 0.9f * x));
            DefaultShader.Parameters["xLightPositions"].SetValue(LightPositions);

            CompositeLibrary.Render(GraphicsDevice, DwarfGame.SpriteBatch);
            CompositeLibrary.Update();
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            if (GameSettings.Default.UseDynamicShadows)
            {
                ChunkManager.RenderShadowmap(DefaultShader, GraphicsDevice, Shadows, Matrix.Identity, Tilesheet);
            }

            if (GameSettings.Default.UseLightmaps)
            {
                ChunkManager.RenderLightmaps(Camera, gameTime, GraphicsDevice, DefaultShader, Matrix.Identity);
            }

            // Computes the water height.
            float wHeight = WaterRenderer.GetVisibleWaterHeight(ChunkManager, Camera, GraphicsDevice.Viewport,
                lastWaterHeight);
            lastWaterHeight = wHeight;

            // Draw reflection/refraction images
            WaterRenderer.DrawReflectionMap(gameTime, this, wHeight - 0.1f, GetReflectedCameraMatrix(wHeight),
                DefaultShader, GraphicsDevice);

            // Start drawing the bloom effect
            if (GameSettings.Default.EnableGlow)
            {
                bloom.BeginDraw();
            }
            else if (UseFXAA)
            {
                fxaa.Begin(DwarfTime.LastTime, fxaa.RenderTarget);
            }

            // Draw the sky
            GraphicsDevice.Clear(new Color(DefaultShader.Parameters["xFogColor"].GetValueVector3()));
            DrawSky(gameTime, Camera.ViewMatrix, 1.0f);

            // Defines the current slice for the GPU
            float level = ChunkManager.ChunkData.MaxViewingLevel + 2.0f;
            if (level > ChunkManager.ChunkData.ChunkSizeY)
            {
                level = 1000;
            }

            SlicePlane = SlicePlane * 0.5f + level * 0.5f;

            Plane slicePlane = WaterRenderer.CreatePlane(SlicePlane, new Vector3(0, -1, 0), Camera.ViewMatrix, false);

            // Draw the whole world, and make sure to handle slicing
            DefaultShader.Parameters["ClipPlane0"].SetValue(new Vector4(slicePlane.Normal, slicePlane.D));
            DefaultShader.Parameters["Clipping"].SetValue(1);
            //Blue ghost effect above the current slice.
            DefaultShader.Parameters["GhostMode"].SetValue(1);
            Draw3DThings(gameTime, DefaultShader, Camera.ViewMatrix);

            // Now we want to draw the water on top of everything else
            DefaultShader.Parameters["Clipping"].SetValue(1);
            DefaultShader.Parameters["GhostMode"].SetValue(0);

            //ComponentManager.CollisionManager.DebugDraw();

            // Render simple geometry (boxes, etc.)
            Drawer3D.Render(GraphicsDevice, DefaultShader, true);

            // Now draw all of the entities in the game
            DefaultShader.Parameters["ClipPlane0"].SetValue(new Vector4(slicePlane.Normal, slicePlane.D));
            DefaultShader.Parameters["Clipping"].SetValue(1);
            DefaultShader.Parameters["GhostMode"].SetValue(1);
            DefaultShader.Parameters["xEnableShadows"].SetValue(GameSettings.Default.UseDynamicShadows ? 1 : 0);
            if (GameSettings.Default.UseDynamicShadows)
            {
                Shadows.BindShadowmapEffect(DefaultShader);
            }

            DrawComponents(gameTime, DefaultShader, Camera.ViewMatrix, ComponentManager.WaterRenderType.None,
                lastWaterHeight);

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

            DefaultShader.Parameters["Clipping"].SetValue(0);

            if (GameSettings.Default.EnableGlow)
            {
                bloom.DrawTarget = UseFXAA ? fxaa.RenderTarget : null;
                bloom.Draw(gameTime.ToGameTime());
                if (UseFXAA)
                    fxaa.End(DwarfTime.LastTime, fxaa.RenderTarget);
            }
            else if (UseFXAA)
            {
                fxaa.End(DwarfTime.LastTime, fxaa.RenderTarget);
            }

            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp,
                null, rasterizerState);

            //DwarfGame.SpriteBatch.Draw(Shadows.ShadowTexture, Vector2.Zero, Color.White);

            if (IsCameraUnderwater())
            {
                Drawer2D.FillRect(DwarfGame.SpriteBatch, GraphicsDevice.Viewport.Bounds, new Color(10, 40, 60, 200));
            }

            drawer2D.Render(DwarfGame.SpriteBatch, Camera, GraphicsDevice.Viewport);

            IndicatorManager.Render(gameTime);
            DwarfGame.SpriteBatch.End();

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