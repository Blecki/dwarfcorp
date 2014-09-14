using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This game state allows the player to create randomly generated worlds to play in.
    /// </summary>
    public class WorldGeneratorState : GameState
    {
        public float TemperatureScale { get; set; }
        public float RainfallScale { get; set; }
        public float MountainScale { get; set; }
        public float NoiseScale { get; set; }
        public float NumFaults { get; set; }
        public float NumRains { get; set; }
        public float NumRivers { get; set; }
        public float NumVolcanoes { get; set; }

        public static Texture2D worldMap;
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

        public int Seed
        {
            get { return PlayState.Seed; }
            set { PlayState.Seed = value; }
        }

        public ProgressBar Progress { get; set; }
        public string OverworldDirectory = "Worlds";
        public string WorldName = "world0";
        public LineEdit NameEdit;



        public WorldGeneratorState(DwarfGame game, GameStateManager stateManager) :
            base(game, "WorldGeneratorState", stateManager)
        {
            GenerationComplete = false;
            ImageMutex = new Mutex();
            Input = new InputManager();
            TemperatureScale = 1.0f;
            RainfallScale = 1.0f;
            MountainScale = 1.0f;
            NoiseScale = 1.0f;
            NumFaults = 10;
            NumRains = 8000;
            NumRivers = 100;
            NumVolcanoes = 3;
            GenerateWorldName();
        }

        public void GenerateWorldName()
        {
            List<string[]> templates = new List<string[]>
            {
                new[]
                {
                    "$Place",
                    " of the ",
                    "$Color",
                    " ",
                    "$Animal"
                },
                new[]
                {
                    "$Place",
                    " of the ",
                    "$Material",
                    " ",
                    "$Animal"
                },
                new[]
                {
                    "$Material",
                    " of ",
                    "$Place"
                },
                new[]
                {
                    "$Color",
                    " ",
                    "$Material",
                    " ",
                    "$Place"
                }
               
            };

            WorldName =  TextGenerator.GenerateRandom(templates[PlayState.Random.Next(templates.Count)]); 
        }


        public override void OnEnter()
        {
            PlayState.Random = new ThreadSafeRandom(Seed);


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

            Button genButton = new Button(GUI, layout, "Generate", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Generatea  new random world."
            };
            layout.SetComponentPosition(genButton, 3, 6, 1, 1);

            genButton.OnClicked += OnClick;

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

            GridLayout mapLayout = new GridLayout(GUI, MapPanel, 4, 4);

            ColorKeys = new ColorKey(GUI, mapLayout)
            {
                ColorEntries = Overworld.HeightColors
            };

            mapLayout.SetComponentPosition(ColorKeys, 3, 0, 1, 3);

            layout.SetComponentPosition(MapPanel, 0, 0, 3, 5);

            if(worldMap != null)
            {
                MapPanel.Image = new ImageFrame(worldMap);
            }


            MapPanel.OnClicked += OnMapClick;

            layout.UpdateSizes();

            GroupBox mapProperties = new GroupBox(GUI, layout, "Map Properties");

            GridLayout mapPropertiesLayout = new GridLayout(GUI, mapProperties, 6, 2)
            {
                LocalBounds = new Rectangle(mapProperties.LocalBounds.X, mapProperties.LocalBounds.Y + 32, mapProperties.LocalBounds.Width, mapProperties.LocalBounds.Height)
            };

            ComboBox selectType = new ComboBox(GUI, mapPropertiesLayout)
            {
                ToolTip = "Display type for the map."
            };

            selectType.AddValue("Height");
            selectType.AddValue("Biomes");
            selectType.AddValue("Temp.");
            selectType.AddValue("Rain");
            selectType.AddValue("Erosion");
            selectType.AddValue("Faults");
            selectType.CurrentIndex = 0;


            mapPropertiesLayout.SetComponentPosition(selectType, 1, 0, 1, 1);


            Label rainFallLabel = new Label(GUI, mapPropertiesLayout, "Rain", GUI.DefaultFont);
            Slider rainFallScaleSlider = new Slider(GUI, mapPropertiesLayout, "", RainfallScale, 0.0f, 2.0f, Slider.SliderMode.Float)
            {
                ToolTip = "Controls the amount of rainfall in the world."
            };

            rainFallScaleSlider.OnValueModified += rainFallScaleSlider_OnValueModified;
            mapPropertiesLayout.SetComponentPosition(rainFallScaleSlider, 1, 3, 1, 1);
            mapPropertiesLayout.SetComponentPosition(rainFallLabel, 0, 3, 1, 1);


            Label tempLabel = new Label(GUI, mapPropertiesLayout, "Temp.", GUI.DefaultFont);
            Slider tempScaleSlider = new Slider(GUI, mapPropertiesLayout, "", TemperatureScale, 0.0f, 2.0f, Slider.SliderMode.Float)
            {
                ToolTip = "Controls the average temperature of the world."
            };

            tempScaleSlider.OnValueModified += tempScaleSlider_OnValueModified;
            mapPropertiesLayout.SetComponentPosition(tempScaleSlider, 1, 4, 1, 1);
            mapPropertiesLayout.SetComponentPosition(tempLabel, 0, 4, 1, 1);


            Label faultLabel = new Label(GUI, mapPropertiesLayout, "Faults", GUI.DefaultFont);
            Slider numFaultsSlider = new Slider(GUI, mapPropertiesLayout, "", NumFaults, 0, 50, Slider.SliderMode.Integer)
            {
                ToolTip = "Controls, roughly, the number of seas."
            };

            numFaultsSlider.OnValueModified += numFaultsSlider_OnValueModified;
            mapPropertiesLayout.SetComponentPosition(numFaultsSlider, 1, 5, 1, 1);
            mapPropertiesLayout.SetComponentPosition(faultLabel, 0, 5, 1, 1);


            Label selectLabel = new Label(GUI, mapPropertiesLayout, "Display", GUI.DefaultFont);
            mapPropertiesLayout.SetComponentPosition(selectLabel, 0, 0, 1, 1);
            selectLabel.Alignment = Drawer2D.Alignment.Right;

            layout.SetComponentPosition(mapProperties, 3, 0, 1, 6);

            Progress = new ProgressBar(GUI, layout, 0.0f);
            layout.SetComponentPosition(Progress, 0, 5, 3, 1);


            NameEdit = new LineEdit(GUI, mapPropertiesLayout, WorldName)
            {
                ToolTip = "Name of the world."
            };
            mapPropertiesLayout.SetComponentPosition(NameEdit, 0, 1, 2, 1);

            NameEdit.OnTextModified += nameEdit_OnTextModified;


            Label seedLabel = new Label(GUI, mapPropertiesLayout, "Seed", GUI.DefaultFont);
            mapPropertiesLayout.SetComponentPosition(seedLabel, 0, 2, 1, 1);
            seedLabel.Alignment = Drawer2D.Alignment.Right;

            LineEdit seedEdit = new LineEdit(GUI, mapPropertiesLayout, Seed.ToString(CultureInfo.InvariantCulture))
            {
                ToolTip = "Global random seed of the whole game."
            };
            mapPropertiesLayout.SetComponentPosition(seedEdit, 1, 2, 1, 1);

            seedEdit.OnTextModified += seedEdit_OnTextModified;

            selectType.OnSelectionModified += DisplayModeModified;
            base.OnEnter();
        }

        private void nameEdit_OnTextModified(string arg)
        {
            WorldName = arg;
        }

        private void saveButton_OnClicked()
        {
            if(GenerationComplete)
            {
                System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + Program.DirChar + "Worlds" + Program.DirChar + WorldName);
                OverworldFile file = new OverworldFile(Overworld.Map, WorldName);
                file.WriteFile(worldDirectory.FullName + Program.DirChar + "world." + OverworldFile.CompressedExtension, true);
                file.SaveScreenshot(worldDirectory.FullName + Program.DirChar + "screenshot.png");
                Dialog.Popup(GUI, "Save", "File saved.", Dialog.ButtonType.OK);
            }
        }

        private void numFaultsSlider_OnValueModified(float arg)
        {
            NumFaults = arg;
        }

        private void tempScaleSlider_OnValueModified(float arg)
        {
            TemperatureScale = arg;
        }

        private void rainFallScaleSlider_OnValueModified(float arg)
        {
            RainfallScale = arg;
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
            LoadingMessage = "";
            base.OnExit();
        }

        public void StartButtonOnClick()
        {
            if(GenerationComplete)
            {
                Overworld.Name = WorldName;
                //System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + Program.DirChar + OverworldDirectory);
                //OverworldFile file = new OverworldFile(Overworld.Map, WorldName);
                //file.WriteFile(worldDirectory.FullName + Program.DirChar + WorldName + "." + OverworldFile.CompressedExtension, true);

                StateManager.PushState("PlayState");
                PlayState play = (PlayState) StateManager.States["PlayState"];
                MainMenuState menu = (MainMenuState) StateManager.States["MainMenuState"];
                menu.IsGameRunning = true;
            }
        }

        public void ExitButtonOnClick()
        {
            StateManager.PopState();
        }

        public void OnClick()
        {
            if(!IsGenerating)
            {
                GenerateWorldName();
                NameEdit.Text = WorldName;
                PlayState.WorldOrigin = new Vector2(PlayState.WorldWidth / 2, PlayState.WorldHeight / 2);
                genThread = new Thread(unused => GenerateWorld(Seed, (int) PlayState.WorldWidth, (int) PlayState.WorldHeight));
                genThread.Start();
                IsGenerating = true;
            }
        }

        public bool IsGenerating { get; set; }

        public void OnMapClick()
        {
            Vector2 mapCorner = new Vector2(MapPanel.GetImageBounds().Location.X, MapPanel.GetImageBounds().Location.Y);
            MouseState ms = Mouse.GetState();


            float dx = ((float) PlayState.WorldWidth / (float) MapPanel.GetImageBounds().Width);
            float dy = ((float) PlayState.WorldHeight / (float) MapPanel.GetImageBounds().Height);

            float clickX = Math.Max(Math.Min(ms.X - mapCorner.X, MapPanel.GetImageBounds().Width), 0.0f);
            float clickY = Math.Max(Math.Min(ms.Y - mapCorner.Y, MapPanel.GetImageBounds().Height), 0.0f);

            PlayState.WorldOrigin = new Vector2(clickX * dx, clickY * dy);
        }


        public void DisplayModeModified(string type)
        {
            if(!GenerationComplete)
            {
                return;
            }

            if(type == "Height")
            {
                ColorKeys.ColorEntries = Overworld.HeightColors;
                Overworld.TextureFromHeightMap(type, Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap);
            }
            else if(type == "Biomes")
            {
                ColorKeys.ColorEntries = Overworld.BiomeColors;
                Overworld.TextureFromHeightMap(type, Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap);
            }
            else if(type == "Temp.")
            {
                ColorKeys.ColorEntries = Overworld.JetColors;
                Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Temperature, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap);
            }
            else if(type == "Rain")
            {
                ColorKeys.ColorEntries = Overworld.JetColors;
                Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Rainfall, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap);
            }
            else if(type == "Erosion")
            {
                ColorKeys.ColorEntries = Overworld.JetColors;
                Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Erosion, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap);
            }
            else if(type == "Faults")
            {
                ColorKeys.ColorEntries = Overworld.JetColors;
                Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Faults, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, worldData, worldMap);
            }

            
        }


        public void GenerateVolcanoes(int width, int height)
        {
            int volcanoSamples = 4;
            float volcanoSize = 11;
            for(int i = 0; i < (int) NumVolcanoes; i++)
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
                            Overworld.Map[x, y].Biome = Overworld.Biome.Volcano;
                        }
                    }
                }
            }
        }

        public void GenerateWorld(int seed, int width, int height)
        {
            try
            {
                PlayState.Random = new ThreadSafeRandom(Seed);
                GenerationComplete = false;

                LoadingMessage = "Init..";
                Overworld.heightNoise = new Perlin(seed);
                worldMap = new Texture2D(Game.GraphicsDevice, width, height);
                worldData = new Color[width * height];
                Overworld.Map = new Overworld.MapData[width, height];

                Progress.Value = 0.01f;

                LoadingMessage = "Height Map ...";
                Overworld.GenerateHeightMap(width, height, 1.0f, false);

                Progress.Value = 0.05f;

                int numRains = (int)NumRains;
                int rainLength = 250;
                int numRainSamples = 3;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Erosion = 1.0f;
                        Overworld.Map[x, y].Weathering = 0.0f;
                        Overworld.Map[x, y].Faults = 1.0f;
                    }
                }

                LoadingMessage = "Biomes.";
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Temperature = Overworld.noise(x, y, 1.0f, 0.001f) * 0.1f + ((float)(y) / (float)(height)) + Overworld.noise(x, y, 10.0f, 0.1f) * 0.05f * TemperatureScale;
                        Overworld.Map[x, y].Rainfall = Math.Max(Math.Min(Overworld.noise(x, y, 1000.0f, 0.01f) + Overworld.noise(x, y, 100.0f, 0.1f) * 0.05f, 1.0f), 0.0f) * RainfallScale;
                    }
                }

                Overworld.Distort(width, height, 60.0f, 0.005f, Overworld.ScalarFieldType.Rainfall);
                Overworld.Distort(width, height, 30.0f, 0.005f, Overworld.ScalarFieldType.Temperature);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Temperature = Math.Max(Math.Min(Overworld.Map[x, y].Temperature / (Overworld.Map[x, y].Height * 2.0f), 1.0f), 0.0f);
                    }
                }

                Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, width, height, MapPanel.Lock, worldData, worldMap);

                MapPanel.Image = new ImageFrame(worldMap, new Rectangle(0, 0, worldMap.Width, worldMap.Height));
                MapPanel.LocalBounds = new Rectangle(300, 30, worldMap.Width, worldMap.Height);

                int numVoronoiPoints = (int)NumFaults;


                Progress.Value = 0.1f;
                LoadingMessage = "Faults ...";

                #region voronoi

                Voronoi(width, height, numVoronoiPoints);

                #endregion

                Overworld.GenerateHeightMap(width, height, 1.0f, true);

                Progress.Value = 0.2f;
                LoadingMessage = "Weathering...";

                #region weathering

                Vector2[] neighbs =
            {
                new Vector2(1, 0),
                new Vector2(-1, 0),
                new Vector2(0, 1),
                new Vector2(0, -1)
            };
                float[,] buffer = new float[width, height];
                //Weather(width, height, T, neighbs, buffer); 

                #endregion

                Overworld.GenerateHeightMap(width, height, 1.0f, true);

                Progress.Value = 0.25f;
                Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, width, height, MapPanel.Lock, worldData, worldMap);
                LoadingMessage = "Erosion...";

                #region erosion

                Erode(width, height, Overworld.Map, numRains, rainLength, numRainSamples, buffer);
                Overworld.GenerateHeightMap(width, height, 1.0f, true);

                #endregion

                Progress.Value = 0.9f;
                MapPanel.Image = new ImageFrame(worldMap, new Rectangle(0, 0, worldMap.Width, worldMap.Height));
                MapPanel.LocalBounds = new Rectangle(300, 30, worldMap.Width, worldMap.Height);

                LoadingMessage = "Blur.";
                Overworld.Blur(Overworld.Map, width, height, Overworld.ScalarFieldType.Erosion);
                Overworld.GenerateHeightMap(width, height, 1.0f, true);


                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Biome = Overworld.GetBiome(Overworld.Map[x, y].Temperature, Overworld.Map[x, y].Rainfall, Overworld.Map[x, y].Height);
                    }
                }

                LoadingMessage = "Volcanoes";

                GenerateVolcanoes(width, height);

                Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, width, height, MapPanel.Lock, worldData, worldMap);


                GenerationComplete = true;

                MapPanel.Image = new ImageFrame(worldMap, new Rectangle(0, 0, worldMap.Width, worldMap.Height));
                MapPanel.LocalBounds = new Rectangle(300, 30, worldMap.Width, worldMap.Height);


                Progress.Value = 1.0f;
                IsGenerating = false;
            }
            catch (Exception exception)
            {
                Program.WriteExceptionLog(exception);
                throw;
            }
        }

        private void Voronoi(int width, int height, int numVoronoiPoints)
        {
            List<List<Vector2>> vPoints = new List<List<Vector2>>();
            List<float> rands = new List<float>();

            List<Vector2> edge = new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(width, 0),
                new Vector2(width, height),
                new Vector2(0, height),
                new Vector2(0, 0)
            };

            vPoints.Add(edge);

            for(int i = 0; i < numVoronoiPoints; i++)
            {
                Vector2 v = GetEdgePoint(width, height);

                for(int j = 0; j < 4; j++)
                {
                    List<Vector2> line = new List<Vector2>();
                    rands.Add(1.0f);

                    line.Add(v);
                    v += new Vector2(MathFunctions.Rand() - 0.5f, MathFunctions.Rand() - 0.5f) * 50;
                    line.Add(v);
                    vPoints.Add(line);
                }
            }


            List<VoronoiNode> nodes = new List<VoronoiNode>();
            for(int i = 0; i < vPoints.Count; i++)
            {
                for(int j = 0; j < vPoints[i].Count - 1; j++)
                {
                    VoronoiNode node = new VoronoiNode();
                    node.pointA = vPoints[i][j];
                    node.pointB = vPoints[i][j + 1];
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

        private float signum(float f)
        {
            if(f > 0)
            {
                return 1;
            }
            else if(f < 0)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        private void Erode(int width, int height, Overworld.MapData[,] heightMap, int numRains, int rainLength, int numRainSamples, float[,] buffer)
        {
            float remaining = 1.0f - Progress.Value - 0.2f;
            float orig = Progress.Value;
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    buffer[x, y] = heightMap[x, y].Faults;
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

                    if(h < 0.18f || g.LengthSquared() < 1e-12)
                    {
                        break;
                    }

                    Overworld.MinBlend(Overworld.Map, currentPos, erosionRate * Overworld.GetValue(Overworld.Map, currentPos, Overworld.ScalarFieldType.Erosion), Overworld.ScalarFieldType.Erosion);

                    velocity = 0.1f * g + 0.7f * velocity + 0.2f * MathFunctions.RandVector2Circle();
                    //velocity += g * 0.01f;
                    //velocity.Length();
                    currentPos += velocity;
                }
            }

            /*
            int numReverseRains = 200;

            for(int i = 0; i < numReverseRains; i++)
            {
                LoadingMessage = "Rivers " + i + "/" + numReverseRains;
                Vector2 currentPos = new Vector2(0, 0);
                Vector2 bestPos = currentPos;
                float bestHeight = float.MinValue;
                for(int k = 0; k < 5; k++)
                {
                    int randX = PlayState.Random.Next(1, width - 1);
                    int randY = PlayState.Random.Next(1, height - 1);

                    currentPos = new Vector2(randX, randY);
                    float h = Overworld.GetHeight(buffer, currentPos);

                    if(h > bestHeight && h > 0.19f)
                    {
                        bestHeight = h;
                        bestPos = currentPos;
                    }
                }

                currentPos = bestPos;

                for(int j = 0; j < 10000; j++)
                {
                    Vector2 g = Overworld.GetMinNeighbor(buffer, currentPos);


                    if(g.Length() < 0.1f)
                    {
                        break;
                    }

                    g += new Vector2(MathFunctions.Rand() - 0.5f, MathFunctions.Rand() - 0.5f);

                    float h = Overworld.GetHeight(buffer, currentPos);


                    for(int dx = -1; dx < 1; dx++)
                    {
                        for(int dy = -1; dy < 1; dy++)
                        {
                            Overworld.SetWater(Overworld.Map, currentPos + new Vector2(dx, dy), Overworld.WaterType.River);

                            float f = 0.19f / h;
                            Overworld.MinBlend(Overworld.Map, currentPos, f, Overworld.ScalarFieldType.Erosion);
                        }
                    }

                    currentPos += g;


                    if(h < 0.19f)
                    {
                        break;
                    }
                }
            }
             */
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
                            Overworld.AddValue(Overworld.Map, p + maxDiffNeigh, Overworld.ScalarFieldType.Weathering, maxDiff * 0.4f);
                            Overworld.AddValue(Overworld.Map, p, Overworld.ScalarFieldType.Weathering, -maxDiff * 0.4f);
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
                    map[x, y].SetValue(fieldType, ((v - min) / (max - min)) + 0.1f);
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

            return (float) (100 * (maxNode.dist));
        }

        private static int CompareVoronoi(VoronoiNode a, VoronoiNode b)
        {
            if(a == b)
            {
                return 0;
            }
            else if(a.dist < b.dist)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        public override void Update(GameTime gameTime)
        {
            GUI.Update(gameTime);
            Input.Update();

            base.Update(gameTime);
        }


        private void DrawGUI(GameTime gameTime, float dx)
        {
            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                null, null);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(0, dx));


            if(worldMap != null)
            {
                Vector2 mapCorner = new Vector2(MapPanel.GlobalBounds.Location.X, MapPanel.GlobalBounds.Location.Y);
            }
            if(!GenerationComplete)
            {
                Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, LoadingMessage, DefaultFont, new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - 100, Game.GraphicsDevice.Viewport.Height / 2) + new Vector2(0, dx), Color.White, Color.Black);
            }

            if(GenerationComplete)
            {
                float scaleX = ((float) PlayState.WorldWidth / (float) MapPanel.GetImageBounds().Width);
                float scaleY = ((float) PlayState.WorldHeight / (float) MapPanel.GetImageBounds().Height);
                Vector2 mapCorner = new Vector2(MapPanel.GetImageBounds().Location.X, MapPanel.GetImageBounds().Location.Y);
                Vector2 scaledOrigin = PlayState.WorldOrigin;
                scaledOrigin.X /= scaleX;
                scaledOrigin.Y /= scaleY;
                Drawer2D.DrawRect(DwarfGame.SpriteBatch, new Rectangle((int) (PlayState.WorldOrigin.X / scaleX + mapCorner.X) - 10, (int) (PlayState.WorldOrigin.Y / scaleY + mapCorner.Y) - 10, 20, 20), Color.Yellow, 3.0f);
                Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, "Spawn", DefaultFont, mapCorner + scaledOrigin - new Vector2(50, 30), Color.White, Color.Black);
            }

            DwarfGame.SpriteBatch.End();
            GUI.PostRender(gameTime);
        }

        public override void Render(GameTime gameTime)
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
    }

}