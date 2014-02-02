using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Text;
using BloomPostprocess;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Threading;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// This is the main game state for actually playing the game.
    /// </summary>
    public class PlayState : GameState
    {
        public static int Seed { get; set; }

        public static float WorldScale
        {
            get { return GameSettings.Default.WorldScale; }
            set { GameSettings.Default.WorldScale = value; }
        }

        public static int WorldWidth
        {
            get { return GameSettings.Default.WorldWidth; }
            set { GameSettings.Default.WorldWidth = value; }
        }

        public static Vector2 WorldOrigin { get; set; }

        public static int WorldHeight
        {
            get { return GameSettings.Default.WorldHeight; }
            set { GameSettings.Default.WorldHeight = value; }
        }

        public int ChunkWidth
        {
            get { return GameSettings.Default.ChunkWidth; }
            set { GameSettings.Default.ChunkWidth = value; }
        }

        public int ChunkHeight
        {
            get { return GameSettings.Default.ChunkHeight; }
            set { GameSettings.Default.ChunkHeight = value; }
        }

        public static Vector3 CursorLightPos = Vector3.Zero;
        public static bool HasStarted = false;
        public bool DrawMap = false;
        public Texture2D Tilesheet;
        public static Effect DefaultShader;
        public static OrbitCamera Camera;

        public int MultiSamples
        {
            get { return GameSettings.Default.AntiAliasing; }
            set { GameSettings.Default.AntiAliasing = value; }
        }

        public static float AspectRatio = 0.0f;

        public static ChunkManager ChunkManager = null;
        public static VoxelLibrary VoxelLibrary = null;
        public static ChunkGenerator ChunkGenerator = null;
        public static ComponentManager ComponentManager = null;

        public static GameMaster Master = null;

        public string ExistingFile = "";

        public static DwarfGUI GUI = null;

        private Texture2D pixel = null;

        private Drawer2D drawer2D = null;

        private PrimitiveLibrary primitiveLibrary = null;
        private BloomComponent bloom;

        private Effect shader;
        private WaterRenderer waterRenderer;

        public static SkyRenderer Sky;


        public static ThreadSafeRandom Random = new ThreadSafeRandom();

        public static InstanceManager InstanceManager;

        public InputManager Input = new InputManager();

        public ContentManager Content;
        public GraphicsDevice GraphicsDevice;

        public Thread LoadingThread { get; set; }

        public string LoadingMessage = "";

        public static bool Paused { get; set; }

        public static PlanService PlanService = null;
        public static BiomeLibrary BiomeLibrary = new BiomeLibrary();

        public GameCycle GameCycle { get; set; }

        public bool ShouldReset { get; set; }

        public Label CompanyNameLabel { get; set; }
        public ImagePanel CompanyLogoPanel { get; set; }
        public Label MoneyLabel { get; set; }
        public Label TimeLabel { get; set; }
        public Label OrderStatusLabel { get; set; }

        public Timer PreSimulateTimer { get; set; }

        public Label CurrentLevelLabel { get; set; }
        public Button CurrentLevelUpButton { get; set; }
        public Button CurrentLevelDownButton { get; set; }

        public Slider LevelSlider { get; set; }

        public static ParticleManager ParticleManager { get { return ComponentManager.ParticleManager; } set { ComponentManager.ParticleManager = value; } }



        public static WorldTime Time = new WorldTime();

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

        public override void OnEnter()
        {
            if(ShouldReset)
            {
                PreSimulateTimer.Reset(3);
                ShouldReset = false;
                GameCycle = new GameCycle();
                GameCycle.OnCycleChanged += GameCycle_OnCycleChanged;
                Preload();
                Game.IsMouseVisible = true;
                Game.Graphics.PreferMultiSampling = GameSettings.Default.AntiAliasing > 1;

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

                if(SoundManager.Content == null)
                {
                    SoundManager.Content = Content;
                    SoundManager.LoadDefaultSounds();
                }

                SoundManager.PlayMusic("dwarfcorp");
            }
            HasStarted = true;
            if(ChunkManager != null)
            {
                ChunkManager.PauseThreads = false;
            }
            base.OnEnter();
        }

        private void GameCycle_OnCycleChanged(GameCycle.OrderCylce cycle)
        {
        }

        public override void OnExit()
        {
            ChunkManager.PauseThreads = true;
            base.OnExit();
        }

        public void Preload()
        {
            drawer2D = new Drawer2D(Content, GraphicsDevice);
            Game.IsMouseVisible = false;
        }

        public void CreateInitialDwarves(int numDwarves, VoxelChunk c)
        {
            Vector3 g = c.WorldToGrid(Camera.Position);
            float h = c.GetFilledVoxelGridHeightAt((int) g.X, ChunkHeight - 1, (int) g.Z);


            Camera.UpdateBasisVectors();
            Camera.UpdateProjectionMatrix();
            Camera.UpdateViewMatrix();

            for(int i = 0; i < numDwarves; i++)
            {
                Vector3 dorfPos = new Vector3(Camera.Position.X + (float) Random.NextDouble(), h + 10, Camera.Position.Z + (float) Random.NextDouble());
                PhysicsComponent creat = (PhysicsComponent) EntityFactory.GenerateDwarf(dorfPos,
                    ComponentManager, Content, GraphicsDevice, ChunkManager, Camera, ComponentManager.Factions.Factions["Player"], PlanService, "Dwarf");

                creat.Velocity = new Vector3(1, 0, 0);
            }

            Camera.Target = new Vector3(Camera.Position.X, h + 10, Camera.Position.Z + 10);
            Camera.Phi = -(float) Math.PI * 0.3f;
        }

        public void InitializeStaticData()
        {
            primitiveLibrary = new PrimitiveLibrary(GraphicsDevice, Content);
            InstanceManager = new InstanceManager();

            EntityFactory.instanceManager = InstanceManager;
            InstanceManager.CreateStatics(Content);

            Color[] white = new Color[1];
            white[0] = Color.White;
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(white);

            Tilesheet = TextureManager.GetTexture("TileSet");
            AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            DefaultShader = Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders);
            shader = Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders);

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
            ComponentManager.RootComponent = new LocatableComponent(ComponentManager, "root", null, Matrix.Identity, Vector3.Zero, Vector3.Zero, false);
            Vector3 origin = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y);
            Vector3 extents = new Vector3(1500, 1500, 1500);
            ComponentManager.CollisionManager = new CollisionManager(new BoundingBox(origin - extents, origin + extents));

            Alliance.Relationships = Alliance.InitializeRelationships();
        }

        public void GenerateInitialChunks()
        {
            gameFile = null;

            bool fileExists = !string.IsNullOrEmpty(ExistingFile);

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



            Camera = fileExists ? gameFile.Data.Camera : 
                new OrbitCamera(0, 0, 10f, new Vector3(ChunkWidth, ChunkHeight - 1.0f, ChunkWidth) + globalOffset, new Vector3(0, 50, 0) + globalOffset, MathHelper.PiOver4, AspectRatio, 0.1f, GameSettings.Default.VertexCullDistance);

            Drawer3D.Camera = Camera;

            ChunkManager = new ChunkManager(Content, (uint) ChunkWidth, (uint) ChunkHeight, (uint) ChunkWidth, Camera, GraphicsDevice, Tilesheet,
                TextureManager.GetTexture(ContentPaths.Terrain.terrain_illumination),
                TextureManager.GetTexture(ContentPaths.Gradients.sungradient),
                TextureManager.GetTexture(ContentPaths.Gradients.ambientgradient),
                TextureManager.GetTexture(ContentPaths.Gradients.torchgradient),
                ChunkGenerator);
            globalOffset = ChunkManager.ChunkData.RoundToChunkCoords(globalOffset);
            globalOffset.X *= ChunkWidth;
            globalOffset.Y *= ChunkHeight;
            globalOffset.Z *= ChunkWidth;

            if(!fileExists)
            {
                WorldOrigin = new Vector2(globalOffset.X, globalOffset.Z);
                Camera.Position = new Vector3(0, 10, 0) + globalOffset;
                Camera.Target = new Vector3(0, 10, 1) + globalOffset;
                Camera.Radius = 0.01f;
                Camera.Phi = -1.57f;
            }

            ChunkManager.Components = ComponentManager;


            if(gameFile == null)
            {
                ChunkManager.PotentialChunks.Add(new BoundingBox(new Vector3(0, 0, 0)
                                                                 + globalOffset,
                    new Vector3(ChunkWidth, ChunkHeight, ChunkWidth)
                    + globalOffset));
                ChunkManager.GenerateInitialChunks(Camera, ref LoadingMessage);
            }
            else
            {
                LoadingMessage = "Loading Chunks from Game File";
                ChunkManager.ChunkData.LoadFromFile(gameFile, ref LoadingMessage);
            }

            if(!fileExists)
            {
                Camera.Radius = 0.01f;
                Camera.Phi = -1.57f / 4.0f;
                Camera.Theta = 0.0f;
            }


            ChunkManager.RebuildList = new ConcurrentQueue<VoxelChunk>();
            ChunkManager.StartThreads();
        }

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

        public void CreateLiquids()
        {
            waterRenderer = new WaterRenderer(GraphicsDevice);

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
                RippleColor = new Vector4(0.1f, 0.1f, 0.1f, 0.0f)
            };
            waterRenderer.AddLiquidAsset(waterAsset);


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
                RippleColor = new Vector4(0.5f, 0.4f, 0.04f, 0.0f)
            };

            waterRenderer.AddLiquidAsset(lavaAsset);
        }


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

        public void CreateGUI(bool createMaster)
        {
            LoadingMessage = "Creating GUI";
            IndicatorManager.SetupStandards();
            Game.IsMouseVisible = true;
            GUI = new DwarfGUI(Game, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);

            if(createMaster)
            {
                Master = new GameMaster(ComponentManager.Factions.Factions["Player"], Game, ComponentManager, ChunkManager, Camera, GraphicsDevice, GUI);
                CreateGUIComponents();
            }
        }

        public void CreateGUIComponents()
        {
            GUI.RootComponent.ClearChildren();
            GUI.RootComponent.AddChild(Master.Debugger.MainPanel);
            GUI.RootComponent.AddChild(Master.ToolBar);

            GUIComponent companyInfoComponent = new GUIComponent(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(10, 10, 400, 80)
            };

            GridLayout infoLayout = new GridLayout(GUI, companyInfoComponent, 2, 4);
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

            TimeLabel = new Label(GUI, infoLayout, Time.CurrentDate.ToShortDateString() + " " + Time.CurrentDate.ToShortTimeString(), GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100),
                ToolTip = "Current time and date."
            };
            infoLayout.SetComponentPosition(TimeLabel, 4, 0, 1, 1);



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

            OrderStatusLabel = new Label(GUI, GUI.RootComponent, "Ballon : " + GameCycle.GetStatusString(GameCycle.CurrentCycle), GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100),
                LocalBounds = new Rectangle(GraphicsDevice.Viewport.Width / 2 - 300, GraphicsDevice.Viewport.Height - 60, 300, 60),
                ToolTip = "Current status of the balloon"
            };

            LevelSlider = new Slider(GUI, GUI.RootComponent, "", ChunkManager.ChunkData.MaxViewingLevel, 0, ChunkManager.ChunkData.ChunkSizeY, Slider.SliderMode.Integer)
            {
                Orient = Slider.Orientation.Vertical,
                LocalBounds = new Rectangle(28, 130, 64, GraphicsDevice.Viewport.Height - 300),
                ToolTip = "Controls the maximum height of visible terrain"
            };
            LevelSlider.OnClicked += LevelSlider_OnClicked;
            LevelSlider.DrawLabel = true;
            LevelSlider.InvertValue = true;

            InputManager.KeyReleasedCallback -= InputManager_KeyReleasedCallback;
            InputManager.KeyReleasedCallback += InputManager_KeyReleasedCallback;
        }

        public void CreateInitialEmbarkment()
        {
            if(string.IsNullOrEmpty(ExistingFile))
            {
                VoxelChunk c = ChunkManager.ChunkData.GetVoxelChunkAtWorldLocation(Camera.Position);
                GenerateInitialBalloonPort(Master.Faction.RoomDesignator, ChunkManager, Camera.Position.X, Camera.Position.Z, 3);
                CreateInitialDwarves(5, c);
                EntityFactory.CreateBalloon(Camera.Position + new Vector3(0, 1000, 0),  new Vector3(Camera.Position.X, ChunkHeight, Camera.Position.Z), ComponentManager, Content, GraphicsDevice, new ShipmentOrder(0, null), Master.Faction);
            }
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


        public void Load()
        {
            EnableScreensaver = true;
            LoadingMessage = "Initializing Static Data...";
            InitializeStaticData();


            LoadingMessage = "Creating Particles...";
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

        private void LevelSlider_OnClicked()
        {
            ChunkManager.ChunkData.SetMaxViewingLevel((int) LevelSlider.SliderValue, ChunkManager.SliceMode.Y);
        }


        private void CurrentLevelDownButton_OnClicked()
        {
            ChunkManager.ChunkData.SetMaxViewingLevel(ChunkManager.ChunkData.MaxViewingLevel - 1, ChunkManager.SliceMode.Y);
        }

        private void CurrentLevelUpButton_OnClicked()
        {
            ChunkManager.ChunkData.SetMaxViewingLevel(ChunkManager.ChunkData.MaxViewingLevel + 1, ChunkManager.SliceMode.Y);
        }

        public void GenerateInitialBalloonPort(RoomDesignator roomDes, ChunkManager chunkManager, float x, float z, int size)
        {
            Vector3 pos = new Vector3(x, ChunkHeight - 1, z);

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


            Room toBuild = new Room(designations, RoomLibrary.GetType("BalloonPort"), chunkManager);
            RoomBuildDesignation buildDes = new RoomBuildDesignation(toBuild, roomDes.Faction);
            buildDes.Build();
            roomDes.DesignatedRooms.Add(toBuild);
        }

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

        public void CreateParticles()
        {
            ParticleManager = new ParticleManager(ComponentManager);

            EmitterData puff = CreatePuffLike("puff", ContentPaths.Particles.puff, BlendState.AlphaBlend);
            EmitterData bubble = CreatePuffLike("splash2", ContentPaths.Particles.splash2, BlendState.AlphaBlend);
            bubble.ConstantAccel = new Vector3(0, -10, 0);
            bubble.EmissionSpeed = 5;
            bubble.LinearDamping = 0.999f;
            bubble.GrowthSpeed = 1.05f;
            bubble.ParticleDecay = 1.5f;
            EmitterData flame = CreatePuffLike("flame", ContentPaths.Particles.flame, BlendState.Additive);
            ParticleManager.RegisterEffect("puff", puff);
            ParticleManager.RegisterEffect("splash2", bubble);
            ParticleManager.RegisterEffect("flame", flame);

            List<Point> frm2 = new List<Point>
            {
                new Point(0, 0)
            };

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

            CreateGenericExplosion(ContentPaths.Particles.dirt_particle, "dirt_particle");
            CreateGenericExplosion(ContentPaths.Particles.stone_particle, "stone_particle");
            CreateGenericExplosion(ContentPaths.Particles.sand_particle, "sand_particle");
            ParticleEmitter b = CreateGenericExplosion(ContentPaths.Particles.blood_particle, "blood_particle");
            b.Data.MinScale = 0.1f;
            b.Data.MaxScale = 0.15f;
            b.Data.GrowthSpeed = -0.1f;
            b.Data.EmissionSpeed = 5f;
        }

        private void InputManager_KeyReleasedCallback(Keys key)
        {
            if(key == ControlSettings.Default.Map)
            {
                DrawMap = !DrawMap;
            }

            if(key == Keys.Escape)
            {
                OpenPauseMenu();
            }

        }

        private uint frameCounter = 0;
        private readonly Timer frameTimer = new Timer(1.0f, false);
        private float lastWaterHeight = 8.0f;
        private bool pausePressed = false;
        private bool bPressed = false;

        public override void Update(GameTime gameTime)
        {
            if(!Game.IsActive || !IsActiveState)
            {
                return;
            }

            if(!Paused)
            {
                IndicatorManager.Update(gameTime);
                Time.Update(gameTime);
                GameCycle.Update(gameTime);

                /*
                if (!CollideCamera())
                {

                }
                 */
            }

            if(!Paused)
            {
                ComponentManager.CollisionManager.Update(gameTime);
            }

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

            if(Keyboard.GetState().IsKeyDown(Keys.End))
            {
                DwarfGame.ExitGame = true;
                Game.Exit();
            }



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


            if(!Paused)
            {
                Master.Update(Game, gameTime);
                GUI.Update(gameTime);
    
            }


            ChunkManager.Update(gameTime, Camera, GraphicsDevice);
            InstanceManager.Update(gameTime, Camera, GraphicsDevice);

            if(!Paused)
            {
                ComponentManager.Update(gameTime, ChunkManager, Camera);

                Sky.TimeOfDay = Time.GetSkyLightness();
                Sky.CosTime = (float)(Time.GetTotalHours() * 2 * Math.PI / 24.0f);
                //Sky.TimeOfDay = (float)Math.Cos(timeHack * 0.01f) * 0.5f + 0.5f;
                //Sky.CosTime = (float)timeHack * 0.01f;
                shader.Parameters["xTimeOfDay"].SetValue(Sky.TimeOfDay);
            }


            Input.Update();

            SoundManager.Update(gameTime, Camera);

            if(!Paused && Game.IsActive)
            {
                TimeSpan t = TimeSpan.FromSeconds(GameCycle.CycleTimers[GameCycle.CurrentCycle].TargetTimeSeconds - GameCycle.CycleTimers[GameCycle.CurrentCycle].CurrentTimeSeconds);

                string answer = string.Format("{0:D2}m:{1:D2}s",
                    t.Minutes,
                    t.Seconds);
                CurrentLevelLabel.Text = "Slice: " + ChunkManager.ChunkData.MaxViewingLevel + "/" + ChunkHeight;
                OrderStatusLabel.Text = "Balloon: " + GameCycle.GetStatusString(GameCycle.CurrentCycle) + " ETA: " + answer;
                OrderStatusLabel.TextColor = GameCycle.GetColor(GameCycle.CurrentCycle, (float) gameTime.TotalGameTime.TotalSeconds);
                OrderStatusLabel.OnClicked += OrderStatusLabel_OnClicked;
                TimeLabel.Text = Time.CurrentDate.ToShortDateString() + " " + Time.CurrentDate.ToShortTimeString();
                MoneyLabel.Text = Master.Faction.Economy.CurrentMoney.ToString("C");
            }

            if(!LevelSlider.IsMouseOver)
            {
                LevelSlider.SliderValue = ChunkManager.ChunkData.MaxViewingLevel;
            }

            base.Update(gameTime);
        }

        private void OrderStatusLabel_OnClicked()
        {
            switch(GameCycle.CurrentCycle)
            {
                case GameCycle.OrderCylce.BalloonAtMotherland:
                    if(StateManager.NextState == "")
                    {
                        OrderState orderState = (OrderState) StateManager.States["OrderState"];
                        orderState.Mode = OrderState.OrderMode.Buying;
                        StateManager.PushState("OrderState");
                    }
                    Paused = true;
                    GUI.RootComponent.IsVisible = false;
                    break;
                case GameCycle.OrderCylce.BalloonAtColony:
                    if(StateManager.NextState == "")
                    {
                        OrderState orderState = (OrderState) StateManager.States["OrderState"];
                        orderState.Mode = OrderState.OrderMode.Selling;
                        StateManager.PushState("OrderState");
                        GUI.RootComponent.IsVisible = false;
                    }
                    Paused = true;
                    break;
            }
        }

        public void OpenPauseMenu()
        {
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
                    StateManager.PushState("MainMenuState");
                    break;

            }
        }

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

        public void DrawComponents(GameTime gameTime, Effect effect, Matrix view, ComponentManager.WaterRenderType waterRenderType, float waterLevel)
        {
            ComponentManager.Render(gameTime, ChunkManager, Camera, DwarfGame.SpriteBatch, GraphicsDevice, effect, waterRenderType, waterLevel);

            bool reset = waterRenderType == ComponentManager.WaterRenderType.None;

            InstanceManager.Render(GraphicsDevice, effect, Camera, reset);
        }

        public void DrawSky(GameTime time, Matrix view)
        {
            Matrix oldView = Camera.ViewMatrix;
            Camera.ViewMatrix = view;
            Sky.Render(time, GraphicsDevice, Camera);
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Camera.ViewMatrix = oldView;
        }

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


        private readonly List<float> lastFps = new List<float>();
        private float fps = 0.0f;
        private GameFile gameFile;
        public Panel PausePanel;

        public override void Render(GameTime gameTime)
        {
            if(!PreSimulateTimer.HasTriggered)
            {
                PreSimulateTimer.Update(gameTime);
                base.Render(gameTime);
                return;
            }

            if(lastFps.Count > 100)
            {
                lastFps.RemoveAt(0);
            }


            float x = (1.0f - Sky.TimeOfDay);
            x = x * x;
            shader.Parameters["xFogColor"].SetValue(new Vector3(0.32f * x, 0.58f * x, 0.9f * x));

            float wHeight = waterRenderer.GetVisibleWaterHeight(ChunkManager, Camera, GraphicsDevice.Viewport, lastWaterHeight);

            lastWaterHeight = wHeight;
            waterRenderer.DrawRefractionMap(gameTime, this, wHeight + 1.0f, Camera.ViewMatrix, shader, GraphicsDevice);
            waterRenderer.DrawReflectionMap(gameTime, this, wHeight - 0.1f, GetReflectedCameraMatrix(wHeight), shader, GraphicsDevice);

            if(GameSettings.Default.EnableGlow)
            {
                bloom.BeginDraw();
            }

            GraphicsDevice.Clear(Color.CornflowerBlue);
            DrawSky(gameTime, Camera.ViewMatrix);

            Plane slicePlane = waterRenderer.CreatePlane(ChunkManager.ChunkData.MaxViewingLevel + 1.3f, new Vector3(0, -1, 0), Camera.ViewMatrix, false);

            shader.Parameters["ClipPlane0"].SetValue(new Vector4(slicePlane.Normal, slicePlane.D));
            shader.Parameters["Clipping"].SetValue(true);
            shader.Parameters["GhostMode"].SetValue(true);
            Draw3DThings(gameTime, shader, Camera.ViewMatrix);

            shader.Parameters["Clipping"].SetValue(true);
            shader.Parameters["GhostMode"].SetValue(false);
            waterRenderer.DrawWater(
                GraphicsDevice,
                (float) gameTime.TotalGameTime.TotalSeconds,
                shader,
                Camera.ViewMatrix,
                GetReflectedCameraMatrix(wHeight),
                Camera.ProjectionMatrix,
                new Vector3(0.1f, 0.0f, 0.1f),
                Camera,
                ChunkManager);

            shader.CurrentTechnique = shader.Techniques["Textured"];
            shader.Parameters["Clipping"].SetValue(false);

            //LocatableComponent.CollisionManager.DebugDraw();

            Drawer3D.Render(GraphicsDevice, shader, true);

            shader.Parameters["ClipPlane0"].SetValue(new Vector4(slicePlane.Normal, slicePlane.D));
            shader.Parameters["Clipping"].SetValue(true);
            shader.Parameters["GhostMode"].SetValue(true);
            DrawComponents(gameTime, shader, Camera.ViewMatrix, ComponentManager.WaterRenderType.None, lastWaterHeight);
            shader.Parameters["Clipping"].SetValue(false);

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
            //spriteBatch.DrawString(font, "Num Dwarves " + master.Minions.Count, new Vector2(5, 5), Color.White);
            //camera.Position = master.Minions[0].Physics.GlobalTransform.Translation + new Vector3(0, 5, 0);
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

            if(DrawMap)
            {
                const int mapWidth = 256;
                const int mapHeight = 256;
                float scaleX = (float) mapWidth / (float) WorldGeneratorState.worldMap.Width;
                float scaleY = (float) mapHeight / (float) WorldGeneratorState.worldMap.Height;
                DwarfGame.SpriteBatch.Draw(WorldGeneratorState.worldMap, new Rectangle(0, GraphicsDevice.Viewport.Height - mapHeight, mapWidth, mapHeight), new Color(255, 255, 255, 200));
                Vector2 spos = ((new Vector2(Camera.Position.X * scaleX, Camera.Position.Z * scaleY) / WorldScale)) + new Vector2(0, GraphicsDevice.Viewport.Height - mapHeight);
                Vector2 spos2 = spos + new Vector2(frustrumNormal.X * 100 * scaleX, frustrumNormal.Z * 100 * scaleY) / WorldScale;
                Drawer2D.DrawRect(DwarfGame.SpriteBatch, new Rectangle((int) spos.X, (int) spos.Y, 1, 1), Color.White, 1.0f);
                Drawer2D.DrawLine(DwarfGame.SpriteBatch, spos, spos2, Color.White, 1);
            }

            if(Paused)
            {
                Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, "Paused", GUI.DefaultFont, new Vector2(GraphicsDevice.Viewport.Width - 100, 10), Color.White, Color.Black);
            }

            DwarfGame.SpriteBatch.End();
            Master.Render(Game, gameTime, GraphicsDevice);
            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;


            base.Render(gameTime);
        }

 

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