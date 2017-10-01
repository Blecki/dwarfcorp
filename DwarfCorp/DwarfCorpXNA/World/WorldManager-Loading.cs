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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using BloomPostprocess;
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

        public void Setup()
        {
            Screenshots = new List<Screenshot>();
            Game.Graphics.PreferMultiSampling = GameSettings.Default.AntiAliasing > 1;

            try
            {
                Game.Graphics.ApplyChanges();
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }

            Game.Graphics.PreparingDeviceSettings += GraphicsPreparingDeviceSettings;

            LoadingThread = new Thread(LoadThreaded);
            LoadingThread.Name = "Load";
            LoadingThread.Start();
        }

        private void LoadThreaded()
        {
            // Ensure we're using the invariant culture.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            LoadStatus = LoadingStatus.Loading;
            SetLoadingMessage("Initializing ...");

            while (GraphicsDevice == null)
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(1000);

#if CREATE_CRASH_LOGS
            try
#endif
            try
            {
                bool fileExists = !string.IsNullOrEmpty(ExistingFile);

                SetLoadingMessage("Creating Sky...");

                Sky = new SkyRenderer(
                    TextureManager.GetTexture(ContentPaths.Sky.moon),
                    TextureManager.GetTexture(ContentPaths.Sky.sun),
                    Content.Load<TextureCube>(ContentPaths.Sky.day_sky),
                    Content.Load<TextureCube>(ContentPaths.Sky.night_sky),
                    TextureManager.GetTexture(ContentPaths.Gradients.skygradient),
                    Content.Load<Model>(ContentPaths.Models.sphereLowPoly),
                    Content.Load<Effect>(ContentPaths.Shaders.SkySphere),
                    Content.Load<Effect>(ContentPaths.Shaders.Background));

                #region Reading game file

                if (fileExists)
                {
                    SetLoadingMessage("Loading " + ExistingFile);

                    gameFile = SaveGame.CreateFromDirectory(ExistingFile);
                    if (gameFile == null) throw new InvalidOperationException("Game File does not exist.");

                    Sky.TimeOfDay = gameFile.Metadata.TimeOfDay;
                    Time = gameFile.Metadata.Time;
                    WorldOrigin = gameFile.Metadata.WorldOrigin;
                    WorldScale = gameFile.Metadata.WorldScale;
                    WorldSize = gameFile.Metadata.NumChunks;
                    GameID = gameFile.Metadata.GameID;

                    if (gameFile.Metadata.OverworldFile != null && gameFile.Metadata.OverworldFile != "flat")
                    {
                        SetLoadingMessage("Loading world " + gameFile.Metadata.OverworldFile);
                        Overworld.Name = gameFile.Metadata.OverworldFile;
                        DirectoryInfo worldDirectory =
                            Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Worlds" +
                                                      ProgramData.DirChar + Overworld.Name);
                        OverworldFile overWorldFile =
                            new OverworldFile(
                                worldDirectory.FullName + ProgramData.DirChar + "world." +
                                (DwarfGame.COMPRESSED_BINARY_SAVES
                                    ? OverworldFile.CompressedExtension
                                    : OverworldFile.Extension),
                                DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES);
                        Overworld.Map = overWorldFile.Data.CreateMap();
                        Overworld.Name = overWorldFile.Data.Name;
                    }
                    else
                    {
                        SetLoadingMessage("Generating flat world..");
                        Overworld.CreateUniformLand(GraphicsDevice);
                    }
                }

                #endregion

                #region Initialize static data

                {
                    Vector3 origin = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y);
                    Vector3 extents = new Vector3(1500, 1500, 1500);
                    CollisionManager = new CollisionManager(new BoundingBox(origin - extents, origin + extents));


                    CompositeLibrary.Initialize();
                    CraftLibrary = new CraftLibrary();

                    new PrimitiveLibrary(GraphicsDevice, Content);
                    InstanceManager = new InstanceManager();
                    NewInstanceManager = new NewInstanceManager(new BoundingBox(origin - extents, origin + extents),
                        Content);

                    EntityFactory.InstanceManager = InstanceManager;
                    InstanceManager.CreateStatics(Content);

                    Color[] white = new Color[1];
                    white[0] = Color.White;
                    pixel = new Texture2D(GraphicsDevice, 1, 1);
                    pixel.SetData(white);

                    Tilesheet = TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles);
                    AspectRatio = GraphicsDevice.Viewport.AspectRatio;
                    DefaultShader = new Shader(Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders), true);

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

                    JobLibrary.Initialize();
                    MonsterSpawner = new MonsterSpawner(this);
                    EntityFactory.Initialize(this);
                }

                #endregion


                SetLoadingMessage("Creating Planner ...");
                PlanService = new PlanService();

                SetLoadingMessage("Creating Shadows...");
                Shadows = new ShadowRenderer(GraphicsDevice, 1024, 1024);

                SetLoadingMessage("Creating Liquids ...");

                #region liquids

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

                #endregion

                SetLoadingMessage("Generating Initial Terrain Chunks ...");

                if (!fileExists)
                    GameID = MathFunctions.Random.Next(0, 1024);

                ChunkGenerator = new ChunkGenerator(VoxelLibrary, Seed, 0.02f, this.WorldScale)
                {
                    SeaLevel = SeaLevel
                };


                #region Load Components

                if (fileExists)
                {

                    ChunkManager = new ChunkManager(Content, this, Camera,
                        GraphicsDevice,
                        ChunkGenerator, WorldSize.X, WorldSize.Y, WorldSize.Z);

                    ChunkRenderer = new ChunkRenderer(this, Camera, GraphicsDevice, ChunkManager.ChunkData);
                    
                    SetLoadingMessage("Loading Terrain...");
                    gameFile.ReadChunks(ExistingFile);
                    ChunkManager.ChunkData.LoadFromFile(gameFile, SetLoadingMessage);

                    ChunkManager.ChunkData.SetMaxViewingLevel(gameFile.Metadata.Slice > 0
                        ? gameFile.Metadata.Slice
                        : ChunkManager.ChunkData.MaxViewingLevel, ChunkManager.SliceMode.Y);
                    
                    SetLoadingMessage("Loading Entities...");
                    gameFile.LoadPlayData(ExistingFile, this);
                    Camera = gameFile.PlayData.Camera;
                    ChunkManager.camera = Camera;
                    ChunkRenderer.camera = Camera;
                    InstanceManager.Clear();

                    Vector3 origin = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y);
                    Vector3 extents = new Vector3(1500, 1500, 1500);
                    CollisionManager = new CollisionManager(new BoundingBox(origin - extents, origin + extents));

                    ComponentManager = new ComponentManager(gameFile.PlayData.Components, this);

                    foreach (var component in gameFile.PlayData.Components.SaveableComponents)
                    {
                        if (!ComponentManager.HasComponent(component.GlobalID) &&
                            ComponentManager.HasComponent(component.Parent.GlobalID))
                        {
                            // Logically impossible.
                            throw new InvalidOperationException("Component exists in save data but not in manager.");
                        }
                    }

                    foreach (var resource in gameFile.PlayData.Resources)
                    {
                        if (!ResourceLibrary.Resources.ContainsKey(resource.Key))
                        {
                            ResourceLibrary.Add(resource.Value);
                        }
                    }

                    Factions = gameFile.PlayData.Factions;
                    ComponentManager.World = this;

                    Sky.TimeOfDay = gameFile.Metadata.TimeOfDay;
                    Time = gameFile.Metadata.Time;
                    WorldOrigin = gameFile.Metadata.WorldOrigin;
                    WorldScale = gameFile.Metadata.WorldScale;

                    // Restore native factions from deserialized data.
                    Natives = new List<Faction>();
                    foreach (Faction faction in Factions.Factions.Values)
                    {
                        if (faction.Race.IsNative && faction.Race.IsIntelligent && !faction.IsRaceFaction)
                        {
                            Natives.Add(faction);
                        }
                    }

                    Diplomacy = gameFile.PlayData.Diplomacy;

                    GoalManager = new Goals.GoalManager();
                    GoalManager.Initialize(gameFile.PlayData.Goals);

                    TutorialManager = new Tutorial.TutorialManager("Content/tutorial.txt");
                    TutorialManager.SetFromSaveData(gameFile.PlayData.TutorialSaveData);
                }
                else
                {
                    Time = new WorldTime();
                    // WorldOrigin is in "map" units. Convert to voxels
                    var globalOffset = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y)*WorldScale;

                    Camera = new OrbitCamera(this,
                        new Vector3(VoxelConstants.ChunkSizeX,
                            VoxelConstants.ChunkSizeY - 1.0f,
                            VoxelConstants.ChunkSizeZ) + new Vector3(WorldOrigin.X, 0, WorldOrigin.Y)*WorldScale,
                        new Vector3(VoxelConstants.ChunkSizeY, VoxelConstants.ChunkSizeY - 1.0f,
                            VoxelConstants.ChunkSizeZ) + new Vector3(WorldOrigin.X, 0, WorldOrigin.Y)*WorldScale +
                        Vector3.Up*10.0f + Vector3.Backward*10,
                        MathHelper.PiOver4, AspectRatio, 0.1f,
                        GameSettings.Default.VertexCullDistance);

                    ChunkManager = new ChunkManager(Content, this, Camera,
                        GraphicsDevice,
                        ChunkGenerator, WorldSize.X, WorldSize.Y, WorldSize.Z);

                    ChunkRenderer = new ChunkRenderer(this, Camera, GraphicsDevice, ChunkManager.ChunkData);


                    var chunkOffset = GlobalVoxelCoordinate.FromVector3(globalOffset).GetGlobalChunkCoordinate();
                    //var chunkOffset = ChunkManager.ChunkData.RoundToChunkCoords(globalOffset);
                    //globalOffset.X = chunkOffset.X * VoxelConstants.ChunkSizeX;
                    //globalOffset.Y = chunkOffset.Y * VoxelConstants.ChunkSizeY;
                    //globalOffset.Z = chunkOffset.Z * VoxelConstants.ChunkSizeZ;

                    WorldOrigin = new Vector2(globalOffset.X, globalOffset.Z);
                    Camera.Position = new Vector3(0, 10, 0) + globalOffset;
                    Camera.Target = new Vector3(0, 10, 1) + globalOffset;

                    // If there's no file, we have to initialize the first chunk coordinate
                    if (gameFile == null) // Todo: Always true?
                    {
                        ChunkManager.GenerateInitialChunks(
                            GlobalVoxelCoordinate.FromVector3(globalOffset).GetGlobalChunkCoordinate(),
                            SetLoadingMessage);
                    }

                    ComponentManager = new ComponentManager(this, CompanyMakerState.CompanyInformation, Natives);
                    ComponentManager.SetRootComponent(new Body(ComponentManager, "root", Matrix.Identity,
                        Vector3.Zero, Vector3.Zero, false));

                    if (Natives == null) // Todo: Always true??
                    {
                        FactionLibrary library = new FactionLibrary();
                        library.Initialize(this, CompanyMakerState.CompanyInformation);
                        Natives = new List<Faction>();
                        for (int i = 0; i < 10; i++)
                        {
                            Natives.Add(library.GenerateFaction(this, i, 10));
                        }

                    }

                    #region Prepare Factions

                    foreach (Faction faction in Natives)
                    {
                        faction.World = this;

                        if (faction.WallBuilder == null)
                            faction.WallBuilder = new PutDesignator(faction, this);

                        if (faction.RoomBuilder == null)
                            faction.RoomBuilder = new RoomBuilder(faction, this);

                        if (faction.CraftBuilder == null)
                            faction.CraftBuilder = new CraftBuilder(faction, this);

                        faction.WallBuilder.World = this;

                    }

                    Factions = new FactionLibrary();
                    if (Natives != null && Natives.Count > 0)
                    {
                        Factions.AddFactions(this, Natives);
                    }

                    Factions.Initialize(this, CompanyMakerState.CompanyInformation);
                    Point playerOrigin = new Point((int) (WorldOrigin.X), (int) (WorldOrigin.Y));

                    Factions.Factions["Player"].Center = playerOrigin;
                    Factions.Factions["The Motherland"].Center = new Point(playerOrigin.X + 50, playerOrigin.Y + 50);

                    #endregion

                    Diplomacy = new Diplomacy(this);
                    Diplomacy.Initialize(Time.CurrentDate);

                    // Initialize goal manager here.
                    GoalManager = new Goals.GoalManager();
                    GoalManager.Initialize(new List<Goals.Goal>());

                    TutorialManager = new Tutorial.TutorialManager("Content/tutorial.txt");
                    Tutorial("new game start");
                }

                Camera.World = this;
                //Drawer3D.Camera = Camera;


                #endregion

                ChunkManager.camera = Camera;
                // Finally, the chunk manager's threads are started to allow it to 
                // dynamically rebuild terrain
                ChunkManager.RebuildList = new ConcurrentQueue<VoxelChunk>();

                SetLoadingMessage("Creating Particles ...");
                ParticleManager = new ParticleManager(ComponentManager);

                SetLoadingMessage("Creating GameMaster ...");
                Master = new GameMaster(Factions.Factions["Player"], Game, ComponentManager, ChunkManager,
                    Camera, GraphicsDevice);

                if (Master.Faction.Economy.Company.Information == null)
                    Master.Faction.Economy.Company.Information = new CompanyInformation();
                CreateInitialEmbarkment();
                ChunkManager.UpdateRebuildList();
                ChunkManager.CreateGraphics(SetLoadingMessage, ChunkManager.ChunkData);
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
            catch (Exception exception)
            {
                Game.CaptureException(exception);
                LoadingException = exception;
                LoadStatus = LoadingStatus.Failure;
            }
        }
        

#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif
    }
}
