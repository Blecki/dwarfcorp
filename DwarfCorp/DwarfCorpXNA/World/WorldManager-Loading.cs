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
            SetLoadingMessage("Initializing ...");

            while (GraphicsDevice == null)
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(1000);

#if CREATE_CRASH_LOGS
            try
#endif
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
                    Content.Load<Effect>(ContentPaths.Shaders.SkySphere));

                if (!string.IsNullOrEmpty(ExistingFile))
                {
                    SetLoadingMessage("Loading " + ExistingFile);
                    gameFile = new GameFile(ExistingFile, DwarfGame.COMPRESSED_BINARY_SAVES, this);
                    Sky.TimeOfDay = gameFile.Data.Metadata.TimeOfDay;
                    Time = gameFile.Data.Metadata.Time;
                    WorldOrigin = gameFile.Data.Metadata.WorldOrigin;
                    WorldScale = gameFile.Data.Metadata.WorldScale;
                    GameSettings.Default.ChunkWidth = gameFile.Data.Metadata.ChunkWidth;
                    GameSettings.Default.ChunkHeight = gameFile.Data.Metadata.ChunkHeight;
                    GameID = gameFile.Data.GameID;
                    if (gameFile.Data.Metadata.OverworldFile != null && gameFile.Data.Metadata.OverworldFile != "flat")
                    {
                        SetLoadingMessage("Loading world " + gameFile.Data.Metadata.OverworldFile);
                        Overworld.Name = gameFile.Data.Metadata.OverworldFile;
                        DirectoryInfo worldDirectory =
                            Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Worlds" +
                                                      ProgramData.DirChar + Overworld.Name);
                        OverworldFile overWorldFile =
                            new OverworldFile(
                                worldDirectory.FullName + ProgramData.DirChar + "world." + OverworldFile.CompressedExtension,
                                DwarfGame.COMPRESSED_BINARY_SAVES, DwarfGame.COMPRESSED_BINARY_SAVES);
                        Overworld.Map = overWorldFile.Data.CreateMap();
                        Overworld.Name = overWorldFile.Data.Name;
                        WorldWidth = Overworld.Map.GetLength(1);
                        WorldHeight = Overworld.Map.GetLength(0);
                    }
                    else
                    {
                        SetLoadingMessage("Generating flat world..");
                        Overworld.CreateUniformLand(GraphicsDevice);
                    }
                }


                #region Initialize static data
                {
                    Vector3 origin = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y);
                    Vector3 extents = new Vector3(1500, 1500, 1500);
                    CollisionManager = new CollisionManager(new BoundingBox(origin - extents, origin + extents));


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
            
                // If we already have a file, we need to load all the chunks from it.
                // This is preliminary stuff that just makes sure the file exists and can be loaded.
                if (!fileExists)
                    GameID = MathFunctions.Random.Next(0, 1024);


                ChunkGenerator = new ChunkGenerator(VoxelLibrary, Seed, 0.02f, ChunkHeight / 2.0f, this.WorldScale)
                {
                    SeaLevel = SeaLevel
                };

                // Creates the terrain management system.
                ChunkManager = new ChunkManager(Content, this, (uint)ChunkWidth, (uint)ChunkHeight, (uint)ChunkWidth, Camera,
                    GraphicsDevice,
                    ChunkGenerator, WorldSize.X, WorldSize.Y, WorldSize.Z);
            

                SetLoadingMessage("Loading Components...");
                #region Load Components

                if (!string.IsNullOrEmpty(ExistingFile))
                {
                    SetLoadingMessage("Loading Chunks from Game File");
                    ChunkManager.ChunkData.LoadFromFile(gameFile, SetLoadingMessage);

                    gameFile.LoadData(ExistingFile, this);

                    InstanceManager.Clear();

                    //gameFile.LoadComponents(ExistingFile, this);

                    Vector3 origin = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y);
                    Vector3 extents = new Vector3(1500, 1500, 1500);
                    CollisionManager = new CollisionManager(new BoundingBox(origin - extents, origin + extents));

                    ComponentManager = new ComponentManager(gameFile.Data.Components, this);
                    Factions = gameFile.Data.Factions;
                    ComponentManager.World = this;
                    GameComponent.ResetMaxGlobalId(ComponentManager.GetMaxComponentID() + 1);
                    Sky.TimeOfDay = gameFile.Data.Metadata.TimeOfDay;
                    Time = gameFile.Data.Metadata.Time;
                    WorldOrigin = gameFile.Data.Metadata.WorldOrigin;
                    WorldScale = gameFile.Data.Metadata.WorldScale;
                    GameSettings.Default.ChunkWidth = gameFile.Data.Metadata.ChunkWidth;
                    GameSettings.Default.ChunkHeight = gameFile.Data.Metadata.ChunkHeight;

                    // Restore native factions from deserialized data.
                    Natives = new List<Faction>();
                    foreach (Faction faction in Factions.Factions.Values)
                    {
                        if (faction.Race.IsNative && faction.Race.IsIntelligent && !faction.IsRaceFaction)
                        {
                            Natives.Add(faction);
                        }
                    }

                    //gameFile.LoadDiplomacy(ExistingFile, this);
                    Diplomacy = gameFile.Data.Diplomacy;

                    // Load saved goals from file here.
                    GoalManager = new Goals.GoalManager();
                    //gameFile.LoadGoals(ExistingFile, this);
                    GoalManager.Initialize(gameFile.Data.Goals);

                    TutorialManager = new Tutorial.TutorialManager("Content/tutorial.txt");
                    //gameFile.LoadTutorial(ExistingFile, this);
                    TutorialManager.SetFromSaveData(gameFile.Data.TutorialSaveData);

                    Camera = gameFile.Data.Camera;
                }
                else
                {
                    Time = new WorldTime();

                    var globalOffset = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y) * WorldScale;

                    Camera = new OrbitCamera(this, new Vector3(ChunkWidth, ChunkHeight - 1.0f, ChunkWidth) + new Vector3(WorldOrigin.X, 0, WorldOrigin.Y) * WorldScale,
                        new Vector3(ChunkWidth, ChunkHeight - 1.0f, ChunkWidth) + new Vector3(WorldOrigin.X, 0, WorldOrigin.Y) * WorldScale + Vector3.Up * 10.0f + Vector3.Backward * 10,
                        MathHelper.PiOver4, AspectRatio, 0.1f,
                        GameSettings.Default.VertexCullDistance);


                    globalOffset = ChunkManager.ChunkData.RoundToChunkCoords(globalOffset);
                    globalOffset.X *= ChunkWidth;
                    globalOffset.Y *= ChunkHeight;
                    globalOffset.Z *= ChunkWidth;

                    WorldOrigin = new Vector2(globalOffset.X, globalOffset.Z);
                    Camera.Position = new Vector3(0, 10, 0) + globalOffset;
                    Camera.Target = new Vector3(0, 10, 1) + globalOffset;

                    // If there's no file, we have to initialize the first chunk coordinate
                    if (gameFile == null)
                    {
                        ChunkManager.GenerateInitialChunks(
                            ChunkManager.ChunkData.GetChunkID(new Vector3(0, 0, 0) + globalOffset), SetLoadingMessage);
                    }

                    ComponentManager = new ComponentManager(this, CompanyMakerState.CompanyInformation, Natives);
                    ComponentManager.SetRootComponent(new Body(ComponentManager, "root", Matrix.Identity,
                        Vector3.Zero, Vector3.Zero, false));

                    if (Natives == null)
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
                    Point playerOrigin = new Point((int)(WorldOrigin.X), (int)(WorldOrigin.Y));

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
                Drawer3D.Camera = Camera;


                #endregion


                ChunkManager.camera = Camera;
                // Finally, the chunk manager's threads are started to allow it to 
                // dynamically rebuild terrain
                ChunkManager.RebuildList = new ConcurrentQueue<VoxelChunk>();
                ChunkManager.UpdateRebuildList();
                ChunkManager.StartThreads();


                SetLoadingMessage("Creating Particles ...");
                ParticleManager = new ParticleManager(ComponentManager);

                SetLoadingMessage("Creating GameMaster ...");
                Master = new GameMaster(Factions.Factions["Player"], Game, ComponentManager, ChunkManager,
                Camera, GraphicsDevice);

                if (Master.Faction.Economy.Company.Information == null)
                    Master.Faction.Economy.Company.Information = new CompanyInformation();

                SetLoadingMessage("Presimulating ...");
                ShowingWorld = false;
                OnLoadedEvent();

                Thread.Sleep(1000);
                ShowingWorld = true;
                SetLoadingMessage("Complete.");

                // GameFile is no longer needed.
                gameFile = null;
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif
        }
    }
}
