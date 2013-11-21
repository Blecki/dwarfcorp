using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static OrbitCamera camera;

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

        public SillyGUI GUI = null;

        private Texture2D pixel = null;

        private Drawer2D drawer2D = null;

        private PrimitiveLibrary primitiveLibrary = null;
        private BloomPostprocess.BloomComponent bloom;

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

        public static ParticleManager ParticleManager;

        public static bool Paused { get; set; }

        public static PlanService PlanService = null;
        public static BiomeLibrary BiomeLibrary = new BiomeLibrary();

        public GameCycle GameCycle { get; set; }

        public bool ShouldReset { get; set; }

        public Label CompanyNameLabel { get; set; }
        public ImagePanel CompanyLogoPanel { get; set; }
        public Label MoneyLabel { get; set; }
        public Label OrderStatusLabel { get; set; }

        public Timer PreSimulateTimer { get; set; }

        public Label CurrentLevelLabel { get; set; }
        public Button CurrentLevelUpButton { get; set; }
        public Button CurrentLevelDownButton { get; set; }

        public Slider LevelSlider { get; set; }

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
        }

        public override void OnEnter()
        {
            isExiting = false;
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
            Vector3 g = c.WorldToGrid(camera.Position);
            float h = c.GetFilledVoxelGridHeightAt((int) g.X, ChunkHeight - 1, (int) g.Z);


            camera.UpdateBasisVectors();
            camera.UpdateProjectionMatrix();
            camera.UpdateViewMatrix();

            for(int i = 0; i < numDwarves; i++)
            {
                Vector3 dorfPos = new Vector3(camera.Position.X + (float) Random.NextDouble(), h + 10, camera.Position.Z + (float) Random.NextDouble());
                PhysicsComponent creat = (PhysicsComponent) EntityFactory.GenerateDwarf(dorfPos,
                    ComponentManager, Content, GraphicsDevice, ChunkManager, camera, Master, PlanService, "Dwarf");

                creat.Velocity = new Vector3(1, 0, 0);
            }

            camera.Target = new Vector3(camera.Position.X, h + 10, camera.Position.Z + 10);
            camera.Phi = -(float) Math.PI * 0.3f;
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
            DefaultShader = Content.Load<Effect>("Hargraves");
            shader = Content.Load<Effect>("Hargraves");

            VoxelLibrary = new VoxelLibrary();
            VoxelLibrary.InitializeDefaultLibrary(GraphicsDevice, Tilesheet);

            LocatableComponent.CollisionManager = new CollisionManager(new BoundingBox(new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000)));

            bloom = new BloomPostprocess.BloomComponent(Game)
            {
                Settings = BloomPostprocess.BloomSettings.PresetSettings[5]
            };
            bloom.Initialize();


            SoundManager.Content = Content;
            PlanService.Restart();

            ComponentManager = new ComponentManager();
            ComponentManager.RootComponent = new LocatableComponent(ComponentManager, "root", null, Matrix.Identity, Vector3.Zero, Vector3.Zero, false);

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
                PlayState.WorldOrigin = gameFile.Data.Metadata.WorldOrigin;
                PlayState.WorldScale = gameFile.Data.Metadata.WorldScale;
                ChunkWidth = gameFile.Data.Metadata.ChunkWidth;
                ChunkHeight = gameFile.Data.Metadata.ChunkHeight;

                if(gameFile.Data.Metadata.OverworldFile != null && gameFile.Data.Metadata.OverworldFile != "flat")
                {
                    LoadingMessage = "Loading world " + gameFile.Data.Metadata.OverworldFile;
                    OverworldFile overWorldFile = new OverworldFile(DwarfGame.GetGameDirectory() + System.IO.Path.DirectorySeparatorChar + "Worlds" + System.IO.Path.DirectorySeparatorChar + gameFile.Data.Metadata.OverworldFile + "." + OverworldFile.CompressedExtension, true);
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

            ChunkGenerator = new ChunkGenerator(VoxelLibrary, PlayState.Seed, 0.02f, ChunkHeight / 2.0f);

            Vector3 globalOffset = new Vector3(PlayState.WorldOrigin.X, 0, PlayState.WorldOrigin.Y) * PlayState.WorldScale;

            if(fileExists)
            {
                globalOffset /= PlayState.WorldScale;
            }

            camera = new OrbitCamera(this, 0, 0, 10f, new Vector3(ChunkWidth, ChunkHeight - 1.0f, ChunkWidth) + globalOffset, new Vector3(0, 50, 0) + globalOffset, MathHelper.PiOver4, AspectRatio, 0.1f, GameSettings.Default.VertexCullDistance);


            if(fileExists)
            {
                camera.Position = gameFile.Data.Metadata.CameraPosition;
                camera.Phi = gameFile.Data.Metadata.CameraRotation.X;
                camera.Theta = gameFile.Data.Metadata.CameraRotation.Y;
                camera.SetTargetRotation(camera.Theta, camera.Phi);
                camera.Radius = 0.01f;
                
            }

            SimpleDrawing.m_camera = camera;

            ChunkManager = new ChunkManager(Content, (uint) ChunkWidth, (uint) ChunkHeight, (uint) ChunkWidth, camera, GraphicsDevice, Tilesheet,
                Content.Load<Texture2D>("illum2"),
                Content.Load<Texture2D>("sungradient"),
                Content.Load<Texture2D>("ambientgradient"),
                Content.Load<Texture2D>("torchgradient"),
                ChunkGenerator);
            globalOffset = ChunkManager.RoundToChunkCoords(globalOffset);
            globalOffset.X *= ChunkWidth;
            globalOffset.Y *= ChunkHeight;
            globalOffset.Z *= ChunkWidth;

            if(!fileExists)
            {
                WorldOrigin = new Vector2(globalOffset.X, globalOffset.Z);
                camera.Position = new Vector3(0, 10, 0) + globalOffset;
                camera.Target = new Vector3(0, 10, 1) + globalOffset;
                camera.Radius = 0.01f;
                camera.Phi = -1.57f;
            }

            ChunkManager.Components = ComponentManager;


            if(gameFile == null)
            {
                ChunkManager.PotentialChunks.Add(new BoundingBox(new Vector3(0, 0, 0)
                                                                 + globalOffset,
                    new Vector3(ChunkWidth, ChunkHeight, ChunkWidth)
                    + globalOffset));
                ChunkManager.GenerateInitialChunks(camera, ref LoadingMessage);
            }
            else
            {
                LoadingMessage = "Loading Chunks from Game File";
                ChunkManager.LoadFromFile(gameFile, ref LoadingMessage);
            }

            if(!fileExists)
            {
                camera.Radius = 0.01f;
                camera.Phi = -1.57f / 4.0f;
                camera.Theta = 0.0f;
            }
            else
            {
                camera.Position = gameFile.Data.Metadata.CameraPosition;
                camera.Phi = gameFile.Data.Metadata.CameraRotation.X;
                camera.Theta = gameFile.Data.Metadata.CameraRotation.Y;
                camera.SetTargetRotation(camera.Theta, camera.Phi);
                camera.Radius = 0.01f;
            }

            ChunkManager.RebuildList = new System.Collections.Concurrent.ConcurrentQueue<VoxelChunk>();
            ChunkManager.StartThreads();
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
                BumpTexture = Content.Load<Texture2D>("water_normal"),
                FoamTexture = Content.Load<Texture2D>("foam"),
                BaseTexture = Content.Load<Texture2D>("cartoon_water"),
                PuddleTexture = Content.Load<Texture2D>("puddle"),
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
                BumpTexture = Content.Load<Texture2D>("water_normal"),
                FoamTexture = Content.Load<Texture2D>("lavafoam"),
                BaseTexture = Content.Load<Texture2D>("lava"),
                PuddleTexture = Content.Load<Texture2D>("puddle"),
                RippleColor = new Vector4(0.5f, 0.4f, 0.04f, 0.0f)
            };

            waterRenderer.AddLiquidAsset(lavaAsset);
        }


        public void CreateSky()
        {
            Sky = new SkyRenderer(
                Content.Load<Texture2D>("moon"),
                Content.Load<Texture2D>("sun"),
                Content.Load<TextureCube>("day_sky"),
                Content.Load<TextureCube>("night_sky"),
                Content.Load<Texture2D>("skygradient"),
                Content.Load<Model>("sphereLowPoly"),
                Content.Load<Effect>("SkySphere"));
        }

        public void CreateGUI()
        {
            LoadingMessage = "Creating GUI";
            Game.IsMouseVisible = true;
            GUI = new SillyGUI(Game, Game.Content.Load<SpriteFont>("Default"), Game.Content.Load<SpriteFont>("Title"), Game.Content.Load<SpriteFont>("Small"), Input);
            Texture2D iconsImage = TextureManager.GetTexture("IconSheet");
            Master = new GameMaster(Game, ComponentManager, ChunkManager, camera, GraphicsDevice, VoxelLibrary, GUI);
            MasterControls tools = new MasterControls(GUI, GUI.RootComponent, Master, iconsImage, GraphicsDevice, Game.Content.Load<SpriteFont>("Default"));
            Master.ToolBar = tools;
            Master.ToolBar.Master = Master;


            SillyGUIComponent companyInfoComponent = new SillyGUIComponent(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(10, 10, 400, 80)
            };

            GridLayout infoLayout = new GridLayout(GUI, companyInfoComponent, 2, 4);
            CompanyLogoPanel = new ImagePanel(GUI, infoLayout, new ImageFrame(TextureManager.GetTexture("CompanyLogo")));
            infoLayout.SetComponentPosition(CompanyLogoPanel, 0, 0, 1, 1);

            CompanyNameLabel = new Label(GUI, infoLayout, PlayerSettings.Default.CompanyName, GUI.DefaultFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100)
            };
            infoLayout.SetComponentPosition(CompanyNameLabel, 1, 0, 1, 1);

            MoneyLabel = new Label(GUI, infoLayout, Master.Economy.CurrentMoney.ToString("C"), GUI.DefaultFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100)
            };
            infoLayout.SetComponentPosition(MoneyLabel, 3, 0, 1, 1);

            CurrentLevelLabel = new Label(GUI, infoLayout, "Slice: " + ChunkManager.MaxViewingLevel, GUI.DefaultFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100)
            };
            infoLayout.SetComponentPosition(CurrentLevelLabel, 0, 1, 1, 1);

            CurrentLevelUpButton = new Button(GUI, infoLayout, "up", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            infoLayout.SetComponentPosition(CurrentLevelUpButton, 2, 1, 1, 1);
            CurrentLevelUpButton.OnClicked += CurrentLevelUpButton_OnClicked;

            CurrentLevelDownButton = new Button(GUI, infoLayout, "down", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            infoLayout.SetComponentPosition(CurrentLevelDownButton, 1, 1, 1, 1);
            CurrentLevelDownButton.OnClicked += CurrentLevelDownButton_OnClicked;

            OrderStatusLabel = new Label(GUI, GUI.RootComponent, "Ballon : " + GameCycle.GetStatusString(GameCycle.CurrentCycle), GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 100),
                LocalBounds = new Rectangle(GraphicsDevice.Viewport.Width / 2 - 300, GraphicsDevice.Viewport.Height - 60, 300, 60)
            };

            ComponentManager.HandleAddRemoves();
            ComponentManager.RootComponent.UpdateTransformsRecursive();

            LevelSlider = new Slider(GUI, GUI.RootComponent, "", ChunkManager.MaxViewingLevel, 0, ChunkManager.ChunkSizeY, Slider.SliderMode.Integer)
            {
                Orient = Slider.Orientation.Vertical,
                LocalBounds = new Rectangle(28, 130, 64, GraphicsDevice.Viewport.Height - 300)
            };
            LevelSlider.OnClicked += LevelSlider_OnClicked;
            LevelSlider.DrawLabel = true;
            LevelSlider.InvertValue = true;

            InputManager.KeyReleasedCallback += InputManager_KeyReleasedCallback;
        }


        public void CreateInitialEmbarkment()
        {
            if(string.IsNullOrEmpty(ExistingFile))
            {
                VoxelChunk c = ChunkManager.GetVoxelChunkAtWorldLocation(camera.Position);
                GenerateInitialBalloonPort(Master.RoomDesignator, ChunkManager, camera.Position.X, camera.Position.Z, 3);
                CreateInitialDwarves(5, c);
                EntityFactory.CreateBalloon(camera.Position + new Vector3(0, 1000, 0), camera.Position + new Vector3(0, 20, 0), ComponentManager, Content, GraphicsDevice, new ShipmentOrder(0, null), Master);
            }
            else
            {
                LoadingMessage = "Creating Entitites...";
                gameFile.CreateEntities(this);
                gameFile.CreatePlayerData(this);
                
            }

            Master.Debugger = new AIDebugger(GUI, Master);
        }


        public void Load()
        {
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
            CreateGUI();

            LoadingMessage = "Embarking.";
            CreateInitialEmbarkment();


            IsInitialized = true;

            LoadingMessage = "Complete.";
        }

        private void LevelSlider_OnClicked()
        {
            ChunkManager.SetMaxViewingLevel((int) LevelSlider.SliderValue, ChunkManager.SliceMode.Y);
        }


        private void CurrentLevelDownButton_OnClicked()
        {
            ChunkManager.SetMaxViewingLevel(ChunkManager.MaxViewingLevel - 1, ChunkManager.SliceMode.Y);
        }

        private void CurrentLevelUpButton_OnClicked()
        {
            ChunkManager.SetMaxViewingLevel(ChunkManager.MaxViewingLevel + 1, ChunkManager.SliceMode.Y);
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
                    VoxelChunk chunk = chunkManager.GetVoxelChunkAtWorldLocation(worldPos);

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
                    VoxelChunk chunk = chunkManager.GetVoxelChunkAtWorldLocation(worldPos);
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
            RoomBuildDesignation buildDes = new RoomBuildDesignation(toBuild, roomDes.Master);
            buildDes.Build();
            roomDes.DesignatedRooms.Add(toBuild);
        }

        public ParticleEmitter CreateGenericExplosion(string assetName)
        {
            List<Point> frm = new List<Point>
            {
                new Point(0, 0)
            };
            Texture2D tex = Content.Load<Texture2D>(assetName);
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

            ParticleManager.RegisterEffect(assetName, testData);
            return ParticleManager.Emitters[assetName];
        }

        public EmitterData CreatePuffLike(string name, BlendState state)
        {
            List<Point> frm = new List<Point>
            {
                new Point(0, 0)
            };
            EmitterData testData = new EmitterData
            {
                Animation = new Animation(GraphicsDevice, Content.Load<Texture2D>(name), name, 32, 32, frm, true, Color.White, 1.0f, 1.0f, 1.0f, false),
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
                Texture = Content.Load<Texture2D>(name),
                Blend = state
            };
            return testData;
        }

        public void CreateParticles()
        {
            ParticleManager = new ParticleManager(ComponentManager);

            EmitterData puff = CreatePuffLike("puff", BlendState.AlphaBlend);
            EmitterData bubble = CreatePuffLike("bubble2", BlendState.AlphaBlend);
            bubble.ConstantAccel = new Vector3(0, -10, 0);
            bubble.EmissionSpeed = 5;
            bubble.LinearDamping = 0.999f;
            bubble.GrowthSpeed = 1.05f;
            bubble.ParticleDecay = 1.5f;
            EmitterData flame = CreatePuffLike("flame", BlendState.Additive);
            ParticleManager.RegisterEffect("puff", puff);
            ParticleManager.RegisterEffect("bubble2", bubble);
            ParticleManager.RegisterEffect("flame", flame);
            List<Point> frm2 = new List<Point>
            {
                new Point(0, 0)
            };
            EmitterData testData2 = new EmitterData
            {
                Animation = new Animation(GraphicsDevice, Content.Load<Texture2D>("leaf"), "leaf", 32, 32, frm2, true, Color.White, 1.0f, 1.0f, 1.0f, false),
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
                Texture = Content.Load<Texture2D>("leaf")
            };

            ParticleManager.RegisterEffect("Leaves", testData2);

            CreateGenericExplosion("dirt_particle");
            CreateGenericExplosion("stone_particle");
            CreateGenericExplosion("sand_particle");
            ParticleEmitter b = CreateGenericExplosion("blood_particle");
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
        }

        private uint frameCounter = 0;
        private Timer frameTimer = new Timer(1.0f, false);
        private float lastWaterHeight = 8.0f;
        private double timeHack = 250.0;
        private bool pausePressed = false;
        private bool isExiting = false;
        private bool bPressed = false;

        public override void Update(GameTime gameTime)
        {
            if(!Game.IsActive || !IsActiveState)
            {
                return;
            }

            if(!Paused)
            {
                GameCycle.Update(gameTime);

                /*
                if (!CollideCamera())
                {

                }
                 */
            }

            if(!Paused)
            {
                LocatableComponent.CollisionManager.Update(gameTime);
            }

            if(Keyboard.GetState().IsKeyDown(ControlSettings.Default.TimeForward))
            {
                timeHack += 1.0;
            }
            else if(Keyboard.GetState().IsKeyDown(ControlSettings.Default.TimeBackward))
            {
                timeHack -= 1.0;
            }

            if(Keyboard.GetState().IsKeyDown(Keys.End))
            {
                //FileUtils.SaveJSon(ComponentManager, "Components.json", true);
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(ComponentManager, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Error,
                    TypeNameHandling = TypeNameHandling.All,
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                });

                

                ComponentManager manager = Newtonsoft.Json.JsonConvert.DeserializeObject<ComponentManager>(output, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Error,
                    TypeNameHandling = TypeNameHandling.All,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    Converters = new List<JsonConverter>
                    {
                        new BoxConverter()
                    }
                });
                manager.RootComponent.Die();

                gameFile = new GameFile(Overworld.Name);
                gameFile.WriteFile(DwarfGame.GetGameDirectory() + System.IO.Path.DirectorySeparatorChar + "Saves" + System.IO.Path.DirectorySeparatorChar + "save0", true);
                Game.Exit();
            }

            if(Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                if(!isExiting)
                {
                    StateManager.PushState("MainMenuState");
                    isExiting = true;
                }

                //GeometricPrimitive.ExitGame = true;
                //Game.Exit();
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
                    OrderScreen orderScreen = (OrderScreen) StateManager.States["OrderScreen"];
                    orderScreen.Mode = OrderScreen.OrderMode.Buying;
                    StateManager.PushState("OrderScreen");
                }
                Paused = true;
            }


            if(!Paused)
            {
                Master.Update(Game, gameTime);
                GUI.Update(gameTime);
            }


            ChunkManager.Update(gameTime, camera, GraphicsDevice);
            InstanceManager.Update(gameTime, camera, GraphicsDevice);

            if(!Paused)
            {
                timeHack += (float) gameTime.ElapsedGameTime.TotalSeconds * 0.5f;
                ComponentManager.Update(gameTime, ChunkManager, camera);
                double x = timeHack;
                Sky.TimeOfDay = (float) Math.Cos(x * 0.01f) * 0.5f + 0.5f;
                Sky.CosTime = (float) x * 0.01f;
                shader.Parameters["xTimeOfDay"].SetValue(Sky.TimeOfDay);
            }


            Input.Update();

            SoundManager.Update(gameTime, camera);

            if(!Paused && Game.IsActive)
            {
                TimeSpan t = TimeSpan.FromSeconds(GameCycle.CycleTimers[GameCycle.CurrentCycle].TargetTimeSeconds - GameCycle.CycleTimers[GameCycle.CurrentCycle].CurrentTimeSeconds);

                string answer = string.Format("{0:D2}m:{1:D2}s",
                    t.Minutes,
                    t.Seconds);
                CurrentLevelLabel.Text = "Slice: " + ChunkManager.MaxViewingLevel + "/" + ChunkHeight;
                OrderStatusLabel.Text = "Balloon: " + GameCycle.GetStatusString(GameCycle.CurrentCycle) + " ETA: " + answer;
                OrderStatusLabel.TextColor = GameCycle.GetColor(GameCycle.CurrentCycle, (float) gameTime.TotalGameTime.TotalSeconds);
                OrderStatusLabel.OnClicked += OrderStatusLabel_OnClicked;

                MoneyLabel.Text = Master.Economy.CurrentMoney.ToString("C");
            }

            if(!LevelSlider.IsMouseOver)
            {
                LevelSlider.SliderValue = ChunkManager.MaxViewingLevel;
            }

            base.Update(gameTime);
        }

        private void OrderStatusLabel_OnClicked()
        {
            if(GameCycle.CurrentCycle == GameCycle.OrderCylce.BalloonAtMotherland)
            {
                if(StateManager.NextState == "")
                {
                    OrderScreen orderScreen = (OrderScreen) StateManager.States["OrderScreen"];
                    orderScreen.Mode = OrderScreen.OrderMode.Buying;
                    StateManager.PushState("OrderScreen");
                }
                Paused = true;
            }
            else if(GameCycle.CurrentCycle == GameCycle.OrderCylce.BalloonAtColony)
            {
                if(StateManager.NextState == "")
                {
                    OrderScreen orderScreen = (OrderScreen) StateManager.States["OrderScreen"];
                    orderScreen.Mode = OrderScreen.OrderMode.Selling;
                    StateManager.PushState("OrderScreen");
                }
                Paused = true;
            }
        }

        public Matrix GetReflectedCameraMatrix(float waterHeight)
        {
            Vector3 reflCameraPosition = camera.Position;
            reflCameraPosition.Y = -camera.Position.Y + waterHeight * 2;
            Vector3 reflTargetPos = camera.Target;
            reflTargetPos.Y = -camera.Target.Y + waterHeight * 2;

            Vector3 cameraRight = Vector3.Cross(camera.Target - camera.Position, camera.UpVector);
            cameraRight.Normalize();
            Vector3 invUpVector = Vector3.Cross(cameraRight, reflTargetPos - reflCameraPosition);
            invUpVector.Normalize();
            return Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);
        }

        public void Draw3DThings(GameTime gameTime, Effect cubeEffect, Matrix view)
        {
            Matrix viewMatrix = camera.ViewMatrix;
            camera.ViewMatrix = view;

            this.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            cubeEffect.Parameters["xView"].SetValue(view);

            cubeEffect.Parameters["xProjection"].SetValue(camera.ProjectionMatrix);
            cubeEffect.CurrentTechnique = cubeEffect.Techniques["Textured"];

            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            ChunkManager.Render(camera, gameTime, GraphicsDevice, cubeEffect, Matrix.Identity);


            if(Master.CurrentTool == GameMaster.ToolMode.Build)
            {
                Master.PutDesignator.Render(gameTime, GraphicsDevice, cubeEffect);
            }

            //LocatableComponent.CollisionManager.Root.Draw();
            camera.ViewMatrix = viewMatrix;
        }

        public void DrawComponents(GameTime gameTime, Effect effect, Matrix view, ComponentManager.WaterRenderType waterRenderType, float waterLevel)
        {
            ComponentManager.Render(gameTime, ChunkManager, camera, DwarfGame.SpriteBatch, GraphicsDevice, effect, waterRenderType, waterLevel);

            bool reset = waterRenderType == ComponentManager.WaterRenderType.None;

            InstanceManager.Render(GraphicsDevice, effect, camera, reset);
        }

        public void DrawSky(GameTime time, Matrix view)
        {
            Matrix oldView = camera.ViewMatrix;
            camera.ViewMatrix = view;
            Sky.Render(time, GraphicsDevice, camera);
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            camera.ViewMatrix = oldView;
        }

        public override void RenderUnitialized(GameTime gameTime)
        {
            DwarfGame.SpriteBatch.Begin();
            float t = (float) (Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2.0f) + 1.0f) * 0.5f + 0.5f;
            Color toDraw = new Color(t, t, t);
            Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, LoadingMessage, Game.Content.Load<SpriteFont>("Default"), new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - 100, Game.GraphicsDevice.Viewport.Height / 2), toDraw, new Color(50, 50, 50));
            DwarfGame.SpriteBatch.End();

            base.RenderUnitialized(gameTime);
        }


        private List<float> lastFPS = new List<float>();
        private float FPS = 0.0f;
        private GameFile gameFile;

        public override void Render(GameTime gameTime)
        {
            if(!PreSimulateTimer.HasTriggered)
            {
                PreSimulateTimer.Update(gameTime);
                base.Render(gameTime);
                return;
            }

            if(lastFPS.Count > 100)
            {
                lastFPS.RemoveAt(0);
            }


            float x = (1.0f - Sky.TimeOfDay);
            x = x * x;
            shader.Parameters["xFogColor"].SetValue(new Vector3(0.32f * x, 0.58f * x, 0.9f * x));

            float wHeight = waterRenderer.GetVisibleWaterHeight(ChunkManager, camera, GraphicsDevice.Viewport, lastWaterHeight);

            lastWaterHeight = wHeight;
            waterRenderer.DrawRefractionMap(gameTime, this, wHeight + 1.0f, camera.ViewMatrix, shader, GraphicsDevice);
            waterRenderer.DrawReflectionMap(gameTime, this, wHeight - 0.1f, GetReflectedCameraMatrix(wHeight), shader, GraphicsDevice);

            if(GameSettings.Default.EnableGlow)
            {
                bloom.BeginDraw();
            }

            GraphicsDevice.Clear(Color.CornflowerBlue);
            DrawSky(gameTime, camera.ViewMatrix);

            Plane slicePlane = waterRenderer.CreatePlane(ChunkManager.MaxViewingLevel + 1.3f, new Vector3(0, -1, 0), camera.ViewMatrix, false);

            shader.Parameters["ClipPlane0"].SetValue(new Vector4(slicePlane.Normal, slicePlane.D));
            shader.Parameters["Clipping"].SetValue(true);

            Draw3DThings(gameTime, shader, camera.ViewMatrix);

            shader.Parameters["Clipping"].SetValue(true);
            waterRenderer.DrawWater(
                GraphicsDevice,
                (float) gameTime.TotalGameTime.TotalSeconds,
                shader,
                camera.ViewMatrix,
                GetReflectedCameraMatrix(wHeight),
                camera.ProjectionMatrix,
                new Vector3(0.1f, 0.0f, 0.1f),
                camera,
                ChunkManager);

            shader.CurrentTechnique = shader.Techniques["Textured"];
            shader.Parameters["Clipping"].SetValue(false);

            //LocatableComponent.CollisionManager.DebugDraw();

            SimpleDrawing.Render(GraphicsDevice, shader, true);

            shader.Parameters["ClipPlane0"].SetValue(new Vector4(slicePlane.Normal, slicePlane.D));
            shader.Parameters["Clipping"].SetValue(true);
            DrawComponents(gameTime, shader, camera.ViewMatrix, ComponentManager.WaterRenderType.None, lastWaterHeight);
            shader.Parameters["Clipping"].SetValue(false);

            if(GameSettings.Default.EnableGlow)
            {
                bloom.Draw(gameTime);
            }

            frameTimer.Update(gameTime);

            if(frameTimer.HasTriggered)
            {
                FPS = frameCounter;

                lastFPS.Add(FPS);
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


            //Drawer2D.SafeDraw(spriteBatch, "\u20AC", Game.Content.Load<SpriteFont>("Default"), Color.White, new Vector2(100, 100), Vector2.Zero);

            drawer2D.Render(DwarfGame.SpriteBatch, camera, GraphicsDevice.Viewport);

            GUI.Render(gameTime, DwarfGame.SpriteBatch, Vector2.Zero);


            bool drawDebugData = GameSettings.Default.DrawDebugData;
            //spriteBatch.DrawString(font, "Num Dwarves " + master.Minions.Count, new Vector2(5, 5), Color.White);
            //camera.Position = master.Minions[0].Physics.GlobalTransform.Translation + new Vector3(0, 5, 0);
            if(drawDebugData)
            {
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "Num Chunks " + ChunkManager.ChunkMap.Values.Count, new Vector2(5, 5), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "Max Viewing Level " + ChunkManager.MaxViewingLevel, new Vector2(5, 20), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "FPS " + Math.Round(FPS), new Vector2(5, 35), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "60", new Vector2(5, 150 - 65), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "30", new Vector2(5, 150 - 35), Color.White);
                DwarfGame.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>("Default"), "10", new Vector2(5, 150 - 15), Color.White);
                for(int i = 0; i < lastFPS.Count; i++)
                {
                    DwarfGame.SpriteBatch.Draw(pixel, new Rectangle(30 + i * 2, 150 - (int) lastFPS[i], 2, (int) lastFPS[i]), new Color(1.0f - lastFPS[i] / 60.0f, lastFPS[i] / 60.0f, 0.0f, 0.5f));
                }
            }

            Vector3 frustrumNormal = camera.GetFrustrum().Far.Normal;

            if(DrawMap)
            {
                int mapWidth = 256;
                int mapHeight = 256;
                float scaleX = (float) mapWidth / (float) WorldGeneratorState.worldMap.Width;
                float scaleY = (float) mapHeight / (float) WorldGeneratorState.worldMap.Height;
                DwarfGame.SpriteBatch.Draw(WorldGeneratorState.worldMap, new Rectangle(0, GraphicsDevice.Viewport.Height - mapHeight, mapWidth, mapHeight), new Color(255, 255, 255, 200));
                Vector2 spos = ((new Vector2(camera.Position.X * scaleX, camera.Position.Z * scaleY) / WorldScale)) + new Vector2(0, GraphicsDevice.Viewport.Height - mapHeight);
                Vector2 spos2 = spos + new Vector2(frustrumNormal.X * 100 * scaleX, frustrumNormal.Z * 100 * scaleY) / WorldScale;
                Drawer2D.DrawRect(DwarfGame.SpriteBatch, new Rectangle((int) spos.X, (int) spos.Y, 1, 1), Color.White, 1.0f);
                Drawer2D.DrawLine(DwarfGame.SpriteBatch, spos, spos2, Color.White, 1);
            }

            if(Paused)
            {
                Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, "Paused", Game.Content.Load<SpriteFont>("Default"), new Vector2(GraphicsDevice.Viewport.Width - 100, 10), Color.White, Color.Black);
            }

            DwarfGame.SpriteBatch.End();
            Master.Render(gameTime, GraphicsDevice);
            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;


            base.Render(gameTime);
        }

        private bool CollideCamera()
        {
            Vector3 position = camera.Target;
            BoundingBox box = new BoundingBox(position - Vector3.One * 0.25f, position + Vector3.One * 0.25f);
            List<VoxelRef> vs = new List<VoxelRef>();
            ChunkManager.GetVoxelReferencesAtWorldLocation(null, position, vs);

            VoxelChunk chunk = ChunkManager.GetVoxelChunkAtWorldLocation(position);

            bool collided = false;

            if(vs.Count > 0 && chunk != null)
            {
                Vector3 grid = chunk.WorldToGrid(camera.Position);
                List<VoxelRef> adjacencies = chunk.GetNeighborsEuclidean((int) grid.X, (int) grid.Y, (int) grid.Z);
                vs.AddRange(adjacencies);
                foreach(VoxelRef v in vs)
                {
                    if(v.TypeName != "empty" && v.TypeName != "water")
                    {
                        BoundingBox voxAABB = v.GetBoundingBox();
                        Voxel vox = v.GetVoxel(ChunkManager, false);

                        if(!vox.IsVisible)
                        {
                            continue;
                        }

                        if(box.Intersects(voxAABB))
                        {
                            PhysicsComponent.Contact contact = new PhysicsComponent.Contact();
                            if(PhysicsComponent.TestStaticAABBAABB(box, voxAABB, ref contact))
                            {
                                collided = true;
                                camera.Target += contact.nEnter * contact.penetration;
                                camera.Velocity *= 0;
                            }
                        }
                    }
                }
            }

            return !collided;
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