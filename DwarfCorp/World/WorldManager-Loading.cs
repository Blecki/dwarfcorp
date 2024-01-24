using BloomPostprocess;
using DwarfCorp.GameStates;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Point = Microsoft.Xna.Framework.Point;

namespace DwarfCorp
{
    public partial class WorldManager
    {
        public enum LoadingStatus
        {
            Loading,
            Success,
            Failure
        }

        public LoadingStatus LoadStatus = LoadingStatus.Loading;

        public Exception LoadingException = null;

        public Thread LoadingThread { get; set; }
        public Action<String> OnSetLoadingMessage = null; // ? Pass through as argument? Should it really be stored here like this?

        public void SetLoadingMessage(String Message)
        {
            if (OnSetLoadingMessage != null)
                OnSetLoadingMessage(Message);
        }

        public void StartLoad()
        {
            Renderer.Screenshots = new List<WorldRenderer.Screenshot>(); // Todo: ?? Why is this updated every single frame?
            Game.Graphics.PreferMultiSampling = GameSettings.Current.AntiAliasing > 1;
          
            try
            {
                Game.Graphics.ApplyChanges();
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
            // This can occur when the user is plugging in a secondary monitor just as we enter this state.
            // ugh.
            catch (ArgumentException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }

            LoadingThread = new Thread(LoadThreaded) { IsBackground = true };
            LoadingThread.Name = "Load";
            LoadingThread.Start();
        }

        private void LoadThreaded()
        {
            DwarfGame.ExitGame = false;
            // Ensure we're using the invariant culture.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            LoadStatus = LoadingStatus.Loading;
            SetLoadingMessage("Initializing ...");

            while (GraphicsDevice == null)
                Thread.Sleep(100);
            Thread.Sleep(1000);

#if !DEBUG
            try
            {
#endif
            if (Overworld.InstanceSettings.LoadType == LoadType.CreateNew)
                CreateNewWorld();
            else
                LoadFromFile();
#if !DEBUG
            }
            catch (Exception exception)
            {
                Program.CaptureException(exception);
                LoadingException = exception;
                LoadStatus = LoadingStatus.Failure;
                ProgramData.WriteExceptionLog(exception);
            }
#endif
        }

        private void LoadFromFile()
        {
            SetLoadingMessage("Creating Sky...");
            Renderer.Sky = new SkyRenderer();

            #region Reading game file

            SetLoadingMessage("Loading " + Overworld.GetInstancePath());

            var gameFile = SaveGame.LoadMetaFromDirectory(Overworld.GetInstancePath());

            if (gameFile == null)
                throw new InvalidOperationException("Game File does not exist.");

            if (gameFile.Metadata.Version != Program.Version && !Program.CompatibleVersions.Contains(gameFile.Metadata.Version))
                throw new InvalidOperationException(String.Format("Game file is from version {0}. Compatible versions are {1}.", gameFile.Metadata.Version,
                    TextGenerator.GetListString(Program.CompatibleVersions)));

            Renderer.Sky.TimeOfDay = gameFile.Metadata.TimeOfDay;
            Renderer.PersistentSettings = gameFile.Metadata.RendererSettings;
            Time = gameFile.Metadata.Time;
            WorldSizeInChunks = Overworld.WorldSizeInChunks;

            #endregion

            #region Initialize static data

            bool actionComplete = false;

            Game.DoLazyAction(new Action(() =>
            {
                Renderer.InstanceRenderer = new InstanceRenderer();
                Renderer.DwarfInstanceRenderer = new DwarfSprites.DwarfInstanceGroup();

                Renderer.bloom = new BloomComponent(Game)
                {
                    Settings = BloomSettings.PresetSettings[5]
                };
                Renderer.bloom.Initialize();

                SoundManager.Content = Content;
                if (PlanService != null)
                    PlanService.Restart();

                EntityFactory.Initialize(this);
            }), () => { actionComplete = true; return true; });

            while (!actionComplete)
            {
                Thread.Sleep(10);
            }

            #endregion

            PlanService = new PlanService();

            SetLoadingMessage("Creating Liquids ...");

            #region liquids

            Renderer.WaterRenderer = new WaterRenderer(GraphicsDevice);

            #endregion

            #region Load Components

            ModuleManager = new ModuleManager(this);
            ChunkManager = new ChunkManager(Content, this);
            Splasher = new Splasher(ChunkManager);

            Renderer.ChunkRenderer = new ChunkRenderer(ChunkManager);

            SetLoadingMessage("Loading Terrain...");
            ChunkManager.LoadChunks(gameFile.LoadChunks(), ChunkManager);
            ChunkManager.Water.FirstBuild();

            SetLoadingMessage("Loading Entities...");
            gameFile.LoadPlayData(Overworld.GetInstancePath(), this);

            PersistentData = gameFile.PlayData.PersistentData;

            Renderer.Camera = gameFile.PlayData.Camera;

            if (gameFile.PlayData.Stats != null)
                Stats = gameFile.PlayData.Stats;

            ComponentManager = new ComponentManager(gameFile.PlayData.Components, this);

            foreach (var component in gameFile.PlayData.Components.SaveableComponents)
            {
                if (!ComponentManager.HasComponent(component.GlobalID) && component.Parent.HasValue(out var p) && ComponentManager.HasComponent(p.GlobalID))
                {
                    // Logically impossible.
                    throw new InvalidOperationException("Component exists in save data but not in manager.");
                }
            }

            ConversationMemory = gameFile.PlayData.ConversationMemory;

            Factions = gameFile.PlayData.Factions;
            ComponentManager.World = this;

            Renderer.Sky.TimeOfDay = gameFile.Metadata.TimeOfDay;
            Time = gameFile.Metadata.Time;

            PlayerFaction = Factions.Factions["Player"];

            EventScheduler = new Events.Scheduler();

            TutorialManager = new Tutorial.TutorialManager();
            TutorialManager.SetFromSaveData(gameFile.PlayData.TutorialSaveData);

            Renderer.Camera.World = this;

            #endregion

            SetLoadingMessage("Creating Particles ...");
            Game.DoLazyAction(new Action(() => ParticleManager = new ParticleManager(ComponentManager)));

            SetLoadingMessage("Creating GameMaster ...");

            TaskManager = new TaskManager();
            TaskManager.World = this;
            Time.NewDay += (time) => PayEmployees();

            DwarfGame.LogSentryBreadcrumb("Loading", "Started new game with an existing file.");
            if (gameFile.PlayData.Tasks != null)
            {
                TaskManager = gameFile.PlayData.Tasks;
                TaskManager.World = this;
            }

            if (PlayerFaction.Economy.Information == null)
                throw new InvalidProgramException();

            if (MathFunctions.RandEvent(0.01f))
                SetLoadingMessage("Reticulating Splines...");

            ChunkManager.NeedsMinimapUpdate = true;
            ChunkManager.StartThreads();
            SetLoadingMessage("Presimulating ...");
            ShowingWorld = false;
            OnLoadedEvent();
            Thread.Sleep(1000);

            ShowingWorld = true;

            SetLoadingMessage("Complete.");

            // GameFile is no longer needed.
            gameFile = null;
            LoadStatus = LoadingStatus.Success;
        }

        private void CreateNewWorld()
        {
            SetLoadingMessage("Creating Sky...");

            Renderer.Sky = new SkyRenderer();


            #region Initialize static data

            bool actionComplete = false;

            SetLoadingMessage("Creating render artifacts...");

            // Create rendering related items on main thread.
            Game.DoLazyAction(new Action(() =>
            {
                Renderer.InstanceRenderer = new InstanceRenderer();
                Renderer.DwarfInstanceRenderer = new DwarfSprites.DwarfInstanceGroup();

                Renderer.bloom = new BloomComponent(Game)
                {
                    Settings = BloomSettings.PresetSettings[5]
                };
                Renderer.bloom.Initialize();

                SoundManager.Content = Content;
                if (PlanService != null)
                    PlanService.Restart();

                EntityFactory.Initialize(this);
            }), () => { actionComplete = true; return true; });

            while (!actionComplete)
            {
                Thread.Sleep(10);
            }

            #endregion


            SetLoadingMessage("Creating Planner ...");
            PlanService = new PlanService();


            SetLoadingMessage("Creating Liquids ...");

            #region liquids

            Renderer.WaterRenderer = new WaterRenderer(GraphicsDevice);

            #endregion

            #region Load Components

            SetLoadingMessage("Initializing engine modules...");

            ModuleManager = new ModuleManager(this);

            Time = new WorldTime();

            Renderer.Camera = new OrbitCamera(this, // Todo: Is setting the camera position and target redundant here?
                new Vector3(VoxelConstants.ChunkSizeX,
                    WorldSizeInVoxels.Y - 1.0f,
                    VoxelConstants.ChunkSizeZ),
                new Vector3(VoxelConstants.ChunkSizeX, WorldSizeInVoxels.Y - 1.0f,
                    VoxelConstants.ChunkSizeZ) +
                Vector3.Up * 10.0f + Vector3.Backward * 10,
                MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f,
                GameSettings.Current.VertexCullDistance);

            PersistentData = new PersistentWorldData();
            ChunkManager = new ChunkManager(Content, this);
            Splasher = new Splasher(ChunkManager);

            Renderer.ChunkRenderer = new ChunkRenderer(ChunkManager);

            Renderer.Camera.Position = new Vector3(0, 10, 0) + new Vector3(WorldSizeInChunks.X * VoxelConstants.ChunkSizeX, 0, WorldSizeInChunks.Z * VoxelConstants.ChunkSizeZ) * 0.5f;
            Renderer.Camera.Target = new Vector3(0, 10, 1) + new Vector3(WorldSizeInChunks.X * VoxelConstants.ChunkSizeX, 0, WorldSizeInChunks.Z * VoxelConstants.ChunkSizeZ) * 0.5f;

            ComponentManager = new ComponentManager(this);
            ComponentManager.SetRootComponent(new GameComponent(ComponentManager, "root", Matrix.Identity, Vector3.Zero, Vector3.Zero));

            #region Prepare Factions

            Factions = new FactionSet();
            //Factions.Initialize(this, Settings.Company);
            foreach (var faction in Overworld.Natives)
                Factions.AddFaction(new Faction(this, faction));

            PlayerFaction = Factions.Factions["Player"];
            PlayerFaction.Economy = new Company(PlayerFaction, 300.0m, Overworld.Company);

            #endregion

            EventScheduler = new Events.Scheduler();

            TutorialManager = new Tutorial.TutorialManager();
            TutorialManager.TutorialEnabled = !GameSettings.Current.TutorialDisabledGlobally;
            Tutorial("new game start");

            foreach (var item in Library.EnumerateResourceTypes().Where(r => r.Craft_Craftable))
                if (!String.IsNullOrEmpty(item.Tutorial))
                    TutorialManager.AddTutorial(item.TypeName, item.Tutorial, item.Gui_Graphic);

            Renderer.Camera.World = this;
            //Drawer3D.Camera = Camera;


            #endregion

            SetLoadingMessage("Creating Particles...");
            ParticleManager = new ParticleManager(ComponentManager);

            SetLoadingMessage("Initializing Task Manager...");

            TaskManager = new TaskManager();
            TaskManager.World = this;
            Time.NewDay += (time) => PayEmployees();


            var generatorSettings = new Generation.ChunkGeneratorSettings(MathFunctions.Random.Next(), 0.02f, Overworld)
            {
                WorldSizeInChunks = WorldSizeInChunks,
                SetLoadingMessage = SetLoadingMessage,
                World = this
            };

            SetLoadingMessage("Generating Chunks...");
            if (Overworld.DebugWorld)
            {
                Generation.Generator.GenerateDebug(ChunkManager, this, generatorSettings, SetLoadingMessage);
                PlayerFaction.Economy.Funds = Overworld.PlayerCorporationFunds;
            }
            else
            {
                Generation.Generator.Generate(ChunkManager, this, generatorSettings, SetLoadingMessage);
                CreateInitialEmbarkment(generatorSettings);
            }
            ChunkManager.NeedsMinimapUpdate = true;
            ChunkManager.RecalculateBounds();

            if (PlayerFaction.Economy.Information == null)
                throw new InvalidProgramException();

            if (MathFunctions.RandEvent(0.01f))
                SetLoadingMessage("Reticulating Splines...");

            ChunkManager.StartThreads();
            ChunkManager.Water.FirstBuild();
            SetLoadingMessage("Presimulating ...");
            ShowingWorld = false;
            OnLoadedEvent();
            Thread.Sleep(1000);

            ShowingWorld = true;

            SetLoadingMessage("Complete.");

            LoadStatus = LoadingStatus.Success;
        }
    }
}
