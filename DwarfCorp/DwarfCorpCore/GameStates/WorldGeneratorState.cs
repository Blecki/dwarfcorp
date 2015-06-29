// WorldGeneratorState.cs
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
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Security.Policy;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Converters;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This game state allows the player to create randomly generated worlds to play in.
    /// </summary>
    public class WorldGeneratorState : GameState, IDisposable
    {
        public WorldSettings Settings { get; set; }

        public static Texture2D	 worldMap;
        public Color[] worldData;
        public DwarfGUI GUI { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public Drawer2D Drawer { get; set; }
        public bool GenerationComplete { get; set; }
        public string LoadingMessage = "";
        public Mutex ImageMutex;
        private Thread genThread;
        public Panel MainWindow { get; set; }
        public int EdgePadding = 32;
        private ImagePanel MapPanel { get; set; }
        public InputManager Input { get; set; }
        public ColorKey ColorKeys { get; set; }
        public ImagePanel CloseupPanel { get; set; }
        public int Seed
        {
            get { return PlayState.Seed; }
            set { PlayState.Seed = value; }
        }

        public ProgressBar Progress { get; set; }
        public string OverworldDirectory = "Worlds";
        public ComboBox WorldSizeBox { get; set; }

        public List<Faction> NativeCivilizations = new List<Faction>();
        public ComboBox ViewSelectionBox;

        public WorldGeneratorState(DwarfGame game, GameStateManager stateManager) :
            base(game, "WorldGeneratorState", stateManager)
        {
            GenerationComplete = false;
            ImageMutex = new Mutex();
            Input = new InputManager();
            Settings = new WorldSettings();
        }

        public override void OnEnter()
        {
            PlayState.WorldWidth = Settings.Width;
            PlayState.WorldHeight = Settings.Height;
            PlayState.SeaLevel = Settings.SeaLevel;
            PlayState.Random = new ThreadSafeRandom(Seed);
            PlayState.WorldSize = new Point3(8, 1, 8);

            Overworld.Volcanoes = new List<Vector2>();

            DefaultFont = Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
            GUI = new DwarfGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);
            IsInitialized = true;
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
            GenerationComplete = false;
            MainWindow = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2)
            };

            GridLayout layout = new GridLayout(GUI, MainWindow, 7, 4)
            {
                LocalBounds = new Rectangle(0, 0, MainWindow.LocalBounds.Width, MainWindow.LocalBounds.Height)
            };

            Button startButton = new Button(GUI, layout, "Start!", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Check))
            {
                ToolTip = "Start the game with the currently generated world."
            };

            layout.SetComponentPosition(startButton, 2, 6, 1, 1);
            startButton.OnClicked += StartButtonOnClick;

            Button saveButton = new Button(GUI, layout, "Save", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Save))
            {
                ToolTip = "Save the generated world to a file."
            };
            layout.SetComponentPosition(saveButton, 1, 6, 1, 1);
            saveButton.OnClicked += saveButton_OnClicked;

            Button exitButton = new Button(GUI, layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow))
            {
                ToolTip = "Back to the main menu."
            };
            layout.SetComponentPosition(exitButton, 0, 6, 1, 1);

            exitButton.OnClicked += ExitButtonOnClick;


            MapPanel = new ImagePanel(GUI, layout, worldMap)
            {
                ToolTip = "Map of the world.\nClick to select a location to embark."
            };

            GridLayout mapLayout = new GridLayout(GUI, MapPanel, 4, 5);

            ColorKeys = new ColorKey(GUI, mapLayout)
            {
                ColorEntries = Overworld.HeightColors
            };

            mapLayout.SetComponentPosition(ColorKeys, 3, 0, 1, 1);

            CloseupPanel = new ImagePanel(GUI, mapLayout, new ImageFrame(worldMap, new Rectangle(0, 0, 128, 128)))
            {
                KeepAspectRatio = true,
                ToolTip = "Closeup of the colony location"
            };

            mapLayout.SetComponentPosition(CloseupPanel, 3, 2, 2, 2);

            layout.SetComponentPosition(MapPanel, 0, 0, 3, 5);

            if(worldMap != null)
            {
                MapPanel.Image = new ImageFrame(worldMap);
            }


            MapPanel.OnClicked += OnMapClick;

            layout.UpdateSizes();

            GroupBox mapProperties = new GroupBox(GUI, layout, "Map Controls");

            GridLayout mapPropertiesLayout = new GridLayout(GUI, mapProperties, 7, 2)
            {
                LocalBounds = new Rectangle(mapProperties.LocalBounds.X, mapProperties.LocalBounds.Y + 32, mapProperties.LocalBounds.Width, mapProperties.LocalBounds.Height)
            };

            ComboBox worldSizeBox = new ComboBox(GUI, mapPropertiesLayout)
            {
                ToolTip = "Size of the colony spawn area."
            };
            

            worldSizeBox.AddValue("Tiny Colony");
            worldSizeBox.AddValue("Small Colony");
            worldSizeBox.AddValue("Medium Colony");
            worldSizeBox.AddValue("Large Colony");
            worldSizeBox.AddValue("Huge Colony");
            worldSizeBox.CurrentIndex = 1;

            worldSizeBox.OnSelectionModified += worldSizeBox_OnSelectionModified;
            mapPropertiesLayout.SetComponentPosition(worldSizeBox, 0, 0, 2, 1);

            ViewSelectionBox = new ComboBox(GUI, mapPropertiesLayout)
            {
                ToolTip = "Display type for the map."
            };

            ViewSelectionBox.AddValue("Height");
            ViewSelectionBox.AddValue("Factions");
            ViewSelectionBox.AddValue("Biomes");
            ViewSelectionBox.AddValue("Temp.");
            ViewSelectionBox.AddValue("Rain");
            ViewSelectionBox.AddValue("Erosion");
            ViewSelectionBox.AddValue("Faults");
            ViewSelectionBox.CurrentIndex = 0;

            mapPropertiesLayout.SetComponentPosition(ViewSelectionBox, 1, 1, 1, 1);

            Label selectLabel = new Label(GUI, mapPropertiesLayout, "Display", GUI.DefaultFont);
            mapPropertiesLayout.SetComponentPosition(selectLabel, 0, 1, 1, 1);
            selectLabel.Alignment = Drawer2D.Alignment.Right;

            layout.SetComponentPosition(mapProperties, 3, 0, 1, 6);

            Progress = new ProgressBar(GUI, layout, 0.0f);
            layout.SetComponentPosition(Progress, 0, 5, 3, 1);

           
            ViewSelectionBox.OnSelectionModified += DisplayModeModified;
            base.OnEnter();
        }

        void worldSizeBox_OnSelectionModified(string arg)
        {
            switch (arg)
            {
                case "Tiny Colony":
                    PlayState.WorldSize = new Point3(4, 1, 4);
                    break;
                case "Small Colony":
                    PlayState.WorldSize = new Point3(8, 1, 8);
                    break;
                case "Medium Colony":
                    PlayState.WorldSize = new Point3(10, 1, 10);
                    break;
                case "Large Colony":
                    PlayState.WorldSize = new Point3(16, 1, 16);
                    break;
                case "Huge Colony":
                    PlayState.WorldSize = new Point3(24, 1, 24);
                    break;
            }
            OnMapClick();
        }


        private void saveButton_OnClicked()
        {
            if(GenerationComplete)
            {
                System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Worlds" + ProgramData.DirChar + Settings.Name);
                OverworldFile file = new OverworldFile(Overworld.Map, Settings.Name);
                file.WriteFile(worldDirectory.FullName + ProgramData.DirChar + "world." + OverworldFile.CompressedExtension, true);
                file.SaveScreenshot(worldDirectory.FullName + ProgramData.DirChar + "screenshot.png");
                Dialog.Popup(GUI, "Save", "File saved.", Dialog.ButtonType.OK);
            }
        }



        private void seedEdit_OnTextModified(string arg)
        {
            Seed = arg.GetHashCode();
            PlayState.Random = new ThreadSafeRandom(Seed);
        }

        public override void OnExit()
        {
            if(genThread != null && genThread.IsAlive)
            {
                genThread.Join();
            }

            if (worldMap != null)
            {
                worldMap.Dispose();
                worldMap = null;
                worldData = null;
            }
            GUI.RootComponent.ClearChildren();
            GUI = null;
            

            LoadingMessage = "";
            base.OnExit();
        }

        public void StartButtonOnClick()
        {
            if(GenerationComplete)
            {
                Overworld.Name = Settings.Name;
                GUI.MouseMode = GUISkin.MousePointer.Wait;
                List<Faction> factionsInSpawn = GetFactionsInSpawn();    
                StateManager.PopState();
                StateManager.PushState("PlayState");

                MainMenuState menu = (MainMenuState) StateManager.States["MainMenuState"];
                menu.IsGameRunning = true;

                PlayState.Natives = factionsInSpawn;
            }
        }

        public List<Faction> GetFactionsInSpawn()
        {
            Rectangle spawnRect = GetSpawnRectangle();
            List<Faction> toReturn = new List<Faction>();
            for (int x = spawnRect.X; x < spawnRect.X + spawnRect.Width; x++)
            {
                for (int y = spawnRect.Y; y < spawnRect.Y + spawnRect.Height; y++)
                {
                    byte factionIdx = Overworld.Map[x, y].Faction;

                    if (factionIdx > 0)
                    {
                        Faction faction = NativeCivilizations[factionIdx - 1];

                        if (!toReturn.Contains(faction))
                        {
                            toReturn.Add(faction);
                        }
                        
                    }
                }
            }
            return toReturn;
        }

        public void ExitButtonOnClick()
        {
            StateManager.PopState();
        }

        public void Generate()
        {
            DoneGenerating = false;
            if(!IsGenerating && !DoneGenerating)
            {
                PlayState.WorldOrigin = new Vector2(PlayState.WorldWidth / 2, PlayState.WorldHeight / 2);
                genThread = new Thread(unused => GenerateWorld(Seed, (int) PlayState.WorldWidth, (int) PlayState.WorldHeight));
                genThread.Start();
                IsGenerating = true;
            }
        }

        public bool IsGenerating { get; set; }

        public void OnMapClick()
        {
            Rectangle imageBounds = MapPanel.GetImageBounds();
            Vector2 mapCorner = new Vector2(imageBounds.Location.X, imageBounds.Location.Y);
            MouseState ms = Mouse.GetState();

            float dx = ((float) PlayState.WorldWidth / (float) MapPanel.GetImageBounds().Width);
            float dy = ((float) PlayState.WorldHeight / (float) MapPanel.GetImageBounds().Height);
            float w = PlayState.WorldSize.X * PlayState.WorldScale;
            float h = PlayState.WorldSize.Z * PlayState.WorldScale;
            float clickX = ms.X - mapCorner.X;
            float clickY = ms.Y - mapCorner.Y;
            clickX *= dx;
            clickY *= dy;
            clickX = Math.Max(Math.Min(clickX, PlayState.WorldWidth - w * 2 - 12), 12);
            clickY = Math.Max(Math.Min(clickY, PlayState.WorldHeight - h * 2 - 12), 12);
           
            PlayState.WorldOrigin = new Vector2((int)(clickX + w), (int)(clickY + h));
        }

        public Dictionary<string, Color> GenerateFactionColors()
        {
            Dictionary<string, Color> toReturn = new Dictionary<string, Color>();
            toReturn["Unclaimed"] = Color.Gray;
            foreach (Faction faction in NativeCivilizations)
            {
                toReturn[faction.Name + " (" + faction.Race.Name + ")"] = faction.PrimaryColor;
            }
            return toReturn;
        }


        public void DisplayModeModified(string type)
        {
            if(!GenerationComplete)
            {
                return;
            }

            switch (type)
            {
                case "Height":
                    ColorKeys.ColorEntries = Overworld.HeightColors;
                    Overworld.TextureFromHeightMap(type, Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Biomes":
                    ColorKeys.ColorEntries = BiomeLibrary.CreateBiomeColors();
                    Overworld.TextureFromHeightMap(type, Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Temp.":
                    ColorKeys.ColorEntries = Overworld.JetColors;
                    Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Temperature, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Rain":
                    ColorKeys.ColorEntries = Overworld.JetColors;
                    Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Rainfall, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Erosion":
                    ColorKeys.ColorEntries = Overworld.JetColors;
                    Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Erosion, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Faults":
                    ColorKeys.ColorEntries = Overworld.JetColors;
                    Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Faults, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Factions":
                    ColorKeys.ColorEntries = GenerateFactionColors();
                    Overworld.NativeFactions = NativeCivilizations;
                    Overworld.TextureFromHeightMap(type, Overworld.Map, Overworld.ScalarFieldType.Factions, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);
                    break;
            }
        }

        public void GenerateVolcanoes(int width, int height)
        {
            int volcanoSamples = 4;
            float volcanoSize = 11;
            for(int i = 0; i < (int) Settings.NumVolcanoes; i++)
            {
                Vector2 randomPos = new Vector2((float) (PlayState.Random.NextDouble() * width), (float) (PlayState.Random.NextDouble() * height));
                float maxFaults = Overworld.Map[(int) randomPos.X, (int) randomPos.Y].Height;
                for(int j = 0; j < volcanoSamples; j++)
                {
                    Vector2 randomPos2 = new Vector2((float) (PlayState.Random.NextDouble() * width), (float) (PlayState.Random.NextDouble() * height));
                    float faults = Overworld.Map[(int) randomPos2.X, (int) randomPos2.Y].Height;

                    if(faults > maxFaults)
                    {
                        randomPos = randomPos2;
                        maxFaults = faults;
                    }
                }

                Overworld.Volcanoes.Add(randomPos);


                for(int dx = -(int) volcanoSize; dx <= (int) volcanoSize; dx++)
                {
                    for(int dy = -(int) volcanoSize; dy <= (int) volcanoSize; dy++)
                    {
                        int x = (int) MathFunctions.Clamp(randomPos.X + dx, 0, width - 1);
                        int y = (int) MathFunctions.Clamp(randomPos.Y + dy, 0, height - 1);

                        float dist = (float) Math.Sqrt(dx * dx + dy * dy);
                        float fDist = (float) Math.Sqrt((dx / 3.0f) * (dx / 3.0f) + (dy / 3.0f) * (dy / 3.0f));

                        //Overworld.Map[x, y].Erosion = MathFunctions.Clamp(dist, 0.0f, 0.5f);
                        float f = (float) (Math.Pow(Math.Sin(fDist), 3.0f) + 1.0f) * 0.2f;
                        Overworld.Map[x, y].Height += f;

                        if(dist <= 2)
                        {
                            Overworld.Map[x, y].Water = Overworld.WaterType.Volcano;
                        }

                        if(dist < volcanoSize)
                        {
                            Overworld.Map[x, y].Biome = Overworld.Biome.Waste;
                        }
                    }
                }
            }
        }

        public void GenerateWorld(int seed, int width, int height)
        {
#if CREATE_CRASH_LOGS
           try
#endif
            {
                GUI.MouseMode = GUISkin.MousePointer.Wait;
               
                PlayState.Random = new ThreadSafeRandom(Seed);
                GenerationComplete = false;

                LoadingMessage = "Init..";
                Overworld.heightNoise.Seed = Seed;
                worldMap = new Texture2D(Game.GraphicsDevice, width, height);
                worldData = new Color[width * height];
                Overworld.Map = new Overworld.MapData[width, height];

                Progress.Value = 0.01f;

                LoadingMessage = "Height Map ...";
                Overworld.GenerateHeightMap(width, height, 1.0f, false);

                Progress.Value = 0.05f;

                int numRains = (int)Settings.NumRains;
                int rainLength = 250;
                int numRainSamples = 3;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Erosion = 1.0f;
                        Overworld.Map[x, y].Weathering = 0;
                        Overworld.Map[x, y].Faults = 1.0f;
                    }
                }

                LoadingMessage = "Climate";
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Temperature = ((float)(y) / (float)(height)) * Settings.TemperatureScale;
                        //Overworld.Map[x, y].Rainfall = Math.Max(Math.Min(Overworld.noise(x, y, 1000.0f, 0.01f) + Overworld.noise(x, y, 100.0f, 0.1f) * 0.05f, 1.0f), 0.0f) * RainfallScale;
                    }
                }

                //Overworld.Distort(width, height, 60.0f, 0.005f, Overworld.ScalarFieldType.Rainfall);
                Overworld.Distort(width, height, 30.0f, 0.005f, Overworld.ScalarFieldType.Temperature);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Temperature = Math.Max(Math.Min(Overworld.Map[x, y].Temperature, 1.0f), 0.0f);
                    }
                }
        
                Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, width, height, MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);

                int numVoronoiPoints = (int)Settings.NumFaults;


                Progress.Value = 0.1f;
                LoadingMessage = "Faults ...";

                #region voronoi

                Voronoi(width, height, numVoronoiPoints);

                #endregion

                Overworld.GenerateHeightMap(width, height, 1.0f, true);

                Progress.Value = 0.2f;

                Overworld.GenerateHeightMap(width, height, 1.0f, true);

                Progress.Value = 0.25f;
                Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, width, height, MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);
                LoadingMessage = "Erosion...";

                #region erosion

                float[,] buffer = new float[width, height];
                Erode(width, height, Settings.SeaLevel, Overworld.Map, numRains, rainLength, numRainSamples, buffer);
                Overworld.GenerateHeightMap(width, height, 1.0f, true);

                #endregion

                Progress.Value = 0.9f;


                LoadingMessage = "Blur.";
                Overworld.Blur(Overworld.Map, width, height, Overworld.ScalarFieldType.Erosion);

                LoadingMessage = "Generate height.";
                Overworld.GenerateHeightMap(width, height, 1.0f, true);


                LoadingMessage = "Rain";
                CalculateRain(width, height);

                LoadingMessage = "Biome";
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Biome = Overworld.GetBiome(Overworld.Map[x, y].Temperature, Overworld.Map[x, y].Rainfall, Overworld.Map[x, y].Height);
                    }
                }

                LoadingMessage = "Volcanoes";

                GenerateVolcanoes(width, height);

                Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, width, height, MapPanel.Lock, worldData, worldMap, Settings.SeaLevel);

                LoadingMessage = "Factions";
                NativeCivilizations = new List<Faction>();
                FactionLibrary library = new FactionLibrary();
                library.Initialize(null, "fake", "fake", null, Color.Blue);
                for (int i = 0; i < Settings.NumCivilizations; i++)
                {
                    NativeCivilizations.Add(library.GenerateFaction(i, Settings.NumCivilizations));
                }
                SeedCivs(Overworld.Map, Settings.NumCivilizations, NativeCivilizations);
                GrowCivs(Overworld.Map, 200, NativeCivilizations);


                for (int x = 0; x < width; x++)
                {
                    Overworld.Map[x, 0] = Overworld.Map[x, 1];
                    Overworld.Map[x, height - 1] = Overworld.Map[x, height - 2];
                }

                for (int y = 0; y < height; y++)
                {
                    Overworld.Map[0, y] = Overworld.Map[1, y];
                    Overworld.Map[width - 1, y] = Overworld.Map[width - 2, y];
                }

                GenerationComplete = true;

                MapPanel.Image = new ImageFrame(worldMap, new Rectangle(0, 0, worldMap.Width, worldMap.Height));
                MapPanel.LocalBounds = new Rectangle(300, 30, worldMap.Width, worldMap.Height);


                Progress.Value = 1.0f;
                IsGenerating = false;
                GUI.MouseMode = GUISkin.MousePointer.Pointer;
                DoneGenerating = true;
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
                throw;
            }
#endif
        }

        public void CalculateRain(int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                float currentMoisture = Settings.RainfallScale * 10;
                for (int x = 0; x < width; x++)
                {
                    float h = Overworld.Map[x, y].Height;
                    bool isWater = h < Settings.SeaLevel;

                    if (isWater)
                    {
                        currentMoisture += MathFunctions.Rand(0.1f, 0.3f);
                        currentMoisture = Math.Min(currentMoisture, Settings.RainfallScale * 20);
                        Overworld.Map[x, y].Rainfall = 0.5f;
                    }
                    else
                    {
                        float rainAmount = currentMoisture * 0.017f * h + currentMoisture * 0.0006f;
                        currentMoisture -= rainAmount;
                        float evapAmount = MathFunctions.Rand(0.01f, 0.02f);
                        currentMoisture += evapAmount;
                        Overworld.Map[x, y].Rainfall = rainAmount * Settings.RainfallScale * Settings.Width * 0.015f;
                    }
                }
            }

            Overworld.Distort(width, height, 5.0f, 0.03f, Overworld.ScalarFieldType.Rainfall);

        }

        private void Voronoi(int width, int height, int numVoronoiPoints)
        {
            List<List<Vector2>> vPoints = new List<List<Vector2>>();
            List<float> rands = new List<float>();

            /*
            List<Vector2> edge = new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(width, 0),
                new Vector2(width, height),
                new Vector2(0, height),
                new Vector2(0, 0)
            };

            List<Vector2> randEdge = new List<Vector2>();
            for (int i = 1; i < edge.Count; i++)
            {
                if (MathFunctions.RandEvent(0.5f))
                {
                    randEdge.Add(edge[i]);
                    randEdge.Add(edge[i - 1]);
                }
            }

            vPoints.Add(randEdge);
             */
            for(int i = 0; i < numVoronoiPoints; i++)
            {
                Vector2 v = GetEdgePoint(width, height);

                for(int j = 0; j < 4; j++)
                {
                    List<Vector2> line = new List<Vector2>();
                    rands.Add(1.0f);

                    line.Add(v);
                    v += new Vector2(MathFunctions.Rand() - 0.5f, MathFunctions.Rand() - 0.5f) * Settings.Width * 0.5f;
                    line.Add(v);
                    vPoints.Add(line);
                }
            }


            List<VoronoiNode> nodes = new List<VoronoiNode>();
            foreach (List<Vector2> pts in vPoints)
            {
                for(int j = 0; j < pts.Count - 1; j++)
                {
                    VoronoiNode node = new VoronoiNode
                    {
                        pointA = pts[j], 
                        pointB = pts[j + 1]
                    };
                    nodes.Add(node);
                }
            }

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    Overworld.Map[x, y].Faults = GetVoronoiValue(nodes, x, y);
                }
            }

            ScaleMap(Overworld.Map, width, height, Overworld.ScalarFieldType.Faults);
            Overworld.Distort(width, height, 20, 0.01f, Overworld.ScalarFieldType.Faults);
        }

        private void Erode(int width, int height, float seaLevel, Overworld.MapData[,] heightMap, int numRains, int rainLength, int numRainSamples, float[,] buffer)
        {
            float remaining = 1.0f - Progress.Value - 0.2f;
            float orig = Progress.Value;
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    buffer[x, y] = heightMap[x, y].Height;
                }
            }

            for(int i = 0; i < numRains; i++)
            {
                LoadingMessage = "Erosion " + i + "/" + numRains;
                Progress.Value = orig + remaining * ((float) i / (float) numRains);
                Vector2 currentPos = new Vector2(0, 0);
                Vector2 bestPos = currentPos;
                float bestHeight = 0.0f;
                for(int k = 0; k < numRainSamples; k++)
                {
                    int randX = PlayState.Random.Next(1, width - 1);
                    int randY = PlayState.Random.Next(1, height - 1);

                    currentPos = new Vector2(randX, randY);
                    float h = Overworld.GetHeight(buffer, currentPos);

                    if(h > bestHeight)
                    {
                        bestHeight = h;
                        bestPos = currentPos;
                    }
                }

                currentPos = bestPos;

                const float erosionRate = 0.99f;
                Vector2 velocity = Vector2.Zero;
                for(int j = 0; j < rainLength; j++)
                {
                    Vector2 g = Overworld.GetMinNeighbor(buffer, currentPos);

                    float h = Overworld.GetHeight(buffer, currentPos);

                    if(h < seaLevel|| g.LengthSquared() < 1e-12)
                    {
                        break;
                    }

                    Overworld.MinBlend(Overworld.Map, currentPos, erosionRate * Overworld.GetValue(Overworld.Map, currentPos, Overworld.ScalarFieldType.Erosion), Overworld.ScalarFieldType.Erosion);

                    velocity = 0.1f * g + 0.7f * velocity + 0.2f * MathFunctions.RandVector2Circle();
                    currentPos += velocity;
                }
            }
        }

        private void Weather(int width, int height, float T, Vector2[] neighbs, float[,] buffer)
        {
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    buffer[x, y] = Overworld.Map[x, y].Height * Overworld.Map[x, y].Faults;
                }
            }

            int weatheringIters = 10;

            for(int iter = 0; iter < weatheringIters; iter++)
            {
                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        Vector2 p = new Vector2(x, y);
                        Vector2 maxDiffNeigh = Vector2.Zero;
                        float maxDiff = 0;
                        float totalDiff = 0;
                        float h = Overworld.GetHeight(buffer, p);
                        float lowestNeighbor = 0.0f;
                        for(int i = 0; i < 4; i++)
                        {
                            float nh = Overworld.GetHeight(buffer, p + neighbs[i]);
                            float diff = h - nh;
                            totalDiff += diff;
                            if(diff > maxDiff)
                            {
                                maxDiffNeigh = neighbs[i];
                                maxDiff = diff;
                                lowestNeighbor = nh;
                            }
                        }

                        if(maxDiff > T)
                        {
                            Overworld.AddValue(Overworld.Map, p + maxDiffNeigh, Overworld.ScalarFieldType.Weathering, (float)(maxDiff * 0.4f));
                            Overworld.AddValue(Overworld.Map, p, Overworld.ScalarFieldType.Weathering, (float)(-maxDiff * 0.4f));
                        }
                    }
                }

                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        Vector2 p = new Vector2(x, y);
                        float w = Overworld.GetValue(Overworld.Map, p, Overworld.ScalarFieldType.Weathering);
                        Overworld.AddHeight(buffer, p, w);
                        Overworld.Map[x, y].Weathering = 0.0f;
                    }
                }
            }

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    Overworld.Map[x, y].Weathering = buffer[x, y] - Overworld.Map[x, y].Height * Overworld.Map[x, y].Faults;
                }
            }
        }

        private static Vector2 GetEdgePoint(int width, int height)
        {
            return new Vector2(PlayState.Random.Next(0, width), PlayState.Random.Next(0, height));
        }

        private static void ScaleMap(Overworld.MapData[,] map, int width, int height, Overworld.ScalarFieldType fieldType)
        {
            float min = 99999;
            float max = -99999;

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    float v = map[x, y].GetValue(fieldType);
                    if(v < min)
                    {
                        min = v;
                    }

                    if(v > max)
                    {
                        max = v;
                    }
                }
            }

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    float v = map[x, y].GetValue(fieldType);
                    map[x, y].SetValue(fieldType, ((v - min) / (max - min)) + 0.001f);
                }
            }
        }

        private class VoronoiNode
        {
            public Vector2 pointA;
            public Vector2 pointB;
            public float dist;
        }

        private float GetVoronoiValue(List<VoronoiNode> points, int x, int y)
        {
            Vector2 xVec = new Vector2(x, y);

            float minDist = float.MaxValue;
            VoronoiNode maxNode = null;
            for(int i = 0; i < points.Count; i++)
            {
                VoronoiNode vor = points[i];
                vor.dist = MathFunctions.PointLineDistance2D(vor.pointA, vor.pointB, xVec);

                if(vor.dist < minDist)
                {
                    minDist = vor.dist;
                    maxNode = vor;
                }
            }

            if(maxNode == null)
            {
                return 1.0f;
            }

            return (float) (1e-2*(maxNode.dist / Settings.Width));
        }


        public  Point? GetRandomLandPoint(Overworld.MapData[,] map)
        {
            const int maxIters = 1000;
            int i = 0;
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            while (i < maxIters)
            {
                int x = PlayState.Random.Next(0, width);
                int y = PlayState.Random.Next(0, height);

                if (map[x, y].Height > Settings.SeaLevel)
                {
                    return new Point(x, y);
                }

                i++;
            }

            return null;
        }

        public  void SeedCivs(Overworld.MapData[,] map, int numCivs, List<Faction> civs )
        {
            for (int i = 0; i < numCivs; i++)
            {
                Point? randomPoint = GetRandomLandPoint(map);

                if (randomPoint == null) continue;
                else
                {
                    map[randomPoint.Value.X, randomPoint.Value.Y].Faction = (byte)(i + 1);
                    civs[i].StartingPlace = randomPoint.Value;
                }
            }
        }

        public  void GrowCivs(Overworld.MapData[,] map, int iters, List<Faction> civs)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            byte[] neighbors = new byte[] {0, 0, 0, 0};
            float[] neighborheights = new float[] { 0, 0, 0, 0};
            Point[] deltas = new Point[] { new Point(1, 0), new Point(0, 1), new Point(-1, 0), new Point(1, -1) };
            for (int i = 0; i < iters; i++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        bool isUnclaimed = map[x, y].Faction == 0;
                        bool isWater = map[x, y].Height < Settings.SeaLevel;
                        if (!isUnclaimed && !isWater)
                        {
                            neighbors[0] = map[x + 1, y].Faction;
                            neighbors[1] = map[x, y + 1].Faction;
                            neighbors[2] = map[x - 1, y].Faction;
                            neighbors[3] = map[x, y - 1].Faction;
                            neighborheights[0] = map[x + 1, y].Height;
                            neighborheights[1] = map[x, y + 1].Height;
                            neighborheights[2] = map[x - 1, y].Height;
                            neighborheights[3] = map[x, y - 1].Height;

                            int minNeighbor = -1;
                            float minHeight = float.MaxValue;

                            for (int k = 0; k < 4; k++)
                            {
                                if (neighbors[k] == 0 && neighborheights[k] < minHeight && neighborheights[k] > Settings.SeaLevel)
                                {
                                    minHeight = neighborheights[k];
                                    minNeighbor = k;
                                }
                            }

                            if (minNeighbor >= 0 && MathFunctions.RandEvent(0.25f / (neighborheights[minNeighbor] + 1e-2f)))
                            {
                                map[x + deltas[minNeighbor].X, y + deltas[minNeighbor].Y].Faction = map[x, y].Faction;
                            }
                        }
                    }
                }
            }

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    byte f = map[x, y].Faction;
                    if (f> 0)
                    {
                        civs[f - 1].Center = new Point(x + civs[f - 1].Center.X, y + civs[f - 1].Center.Y);
                        civs[f - 1].TerritorySize++;
                    }
                }
            }

            foreach (Faction f in civs)
            {
                if(f.TerritorySize > 0)
                    f.Center = new Point(f.Center.X / f.TerritorySize, f.Center.Y / f.TerritorySize);
            }
        }

        public override void Update(DwarfTime gameTime)
        {
            if (StateManager.CurrentState == Name)
            {
                GUI.Update(gameTime);
                Input.Update();

                GUI.EnableMouseEvents = !IsGenerating;

                if (!IsGenerating && !DoneGenerating)
                {
                    Generate();
                }
            }
            TransitionValue = 1.0f;

            base.Update(gameTime);
        }

        public bool DoneGenerating { get; set; }

        public Rectangle GetSpawnRectangle()
        {
            int w = (int) (PlayState.WorldSize.X * PlayState.WorldScale);
            int h = (int) (PlayState.WorldSize.Z * PlayState.WorldScale);
            return new Rectangle((int)PlayState.WorldOrigin.X - w, (int)PlayState.WorldOrigin.Y - h, w * 2, h * 2);
        }

        public Rectangle GetSpawnRectangleOnImage()
        {
            Rectangle spawnRect = GetSpawnRectangle();
            Rectangle imageBounds = MapPanel.GetImageBounds();
            float scaleX = ((float)imageBounds.Width / (float)PlayState.WorldWidth);
            float scaleY = ((float)imageBounds.Height / (float)PlayState.WorldHeight);
            int posX = imageBounds.X;
            int posY = imageBounds.Y;

            return new Rectangle((int)(spawnRect.X * scaleX + posX), (int)(spawnRect.Y * scaleY + posY), (int)(spawnRect.Width * scaleX), (int)(spawnRect.Height * scaleY));
        }

        private void DrawGUI(DwarfTime gameTime, float dx)
        {
            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                null, null);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(0, dx));

            Progress.Message = !GenerationComplete ? LoadingMessage : "";

            if(GenerationComplete)
            {
                Rectangle imageBounds = MapPanel.GetImageBounds();
                float scaleX = ((float)imageBounds.Width / (float)PlayState.WorldWidth);
                float scaleY = ((float)imageBounds.Height / (float)PlayState.WorldHeight);
                Rectangle spawnRect = GetSpawnRectangle();
                Rectangle spawnRectOnImage = GetSpawnRectangleOnImage();
                Drawer2D.DrawRect(DwarfGame.SpriteBatch, spawnRectOnImage, Color.Yellow, 3.0f);
                Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, "Spawn", DefaultFont, new Vector2(spawnRectOnImage.X - 5, spawnRectOnImage.Y - 20), Color.White, Color.Black);

                //ImageMutex.WaitOne();
                /*
                DwarfGame.SpriteBatch.Draw(MapPanel.Image.Image,
                    new Rectangle(MapPanel.GetImageBounds().Right + 2, MapPanel.GetImageBounds().Top,  spawnRect.Width * 4, spawnRect.Height * 4),
                   spawnRect, Color.White);
                 */
                //ImageMutex.ReleaseMutex();
                CloseupPanel.Image.Image = MapPanel.Image.Image;
                CloseupPanel.Image.SourceRect = spawnRect;


                if (ViewSelectionBox.CurrentValue == "Factions")
                {
                    foreach (Faction civ in NativeCivilizations)
                    {
                        Point start = civ.Center;
                        Vector2 measure = Datastructures.SafeMeasure(GUI.SmallFont, civ.Name);
                        Rectangle snapped =
                            MathFunctions.SnapRect(
                                new Vector2(start.X*scaleX + imageBounds.X, start.Y*scaleY + imageBounds.Y) -
                                measure*0.5f, measure, imageBounds);
   
                        Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, civ.Name, GUI.SmallFont, new Vector2(snapped.X, snapped.Y), Color.White, Color.Black);
                    }
                }
            }

            GUI.PostRender(gameTime);
            DwarfGame.SpriteBatch.End();
        }

        public override void Render(DwarfTime gameTime)
        {
            if(Transitioning == TransitionMode.Running)
            {
                Game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                DrawGUI(gameTime, 0);
            }
            else if(Transitioning == TransitionMode.Entering)
            {
                float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Height, Game.GraphicsDevice.Viewport.Height, 1.0f);
                DrawGUI(gameTime, dx);
            }
            else if(Transitioning == TransitionMode.Exiting)
            {
                float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Height, 1.0f);
                DrawGUI(gameTime, dx);
            }


            base.Render(gameTime);
        }

        public void Dispose()
        {
            ImageMutex.Dispose();   
        }
    }

    public class WorldSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; }
        public int NumCivilizations { get; set; }
        public int NumRains { get; set; }
        public int NumVolcanoes { get; set; }
        public float RainfallScale { get; set; }
        public int NumFaults { get; set; }
        public float SeaLevel { get; set; }
        public float TemperatureScale { get; set; }
    }
}