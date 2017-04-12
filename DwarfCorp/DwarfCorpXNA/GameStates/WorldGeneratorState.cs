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
        public WorldGenerator Generator;
        public WorldGenerationSettings Settings { get; set; }
        public static Texture2D	 worldMap;
        public static BasicEffect simpleEffect;
        public DwarfGUI GUI { get; set; }
        public Matrix ViewMatrix { get; set; }
        public Matrix ProjMatrix { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public Mutex ImageMutex;
        public Panel MainWindow { get; set; }
        public int EdgePadding = 16;
        private RenderPanel MapPanel { get; set; }
        public InputManager Input { get; set; }
        public ColorKey ColorKeys { get; set; }
        public ImagePanel CloseupPanel { get; set; }

        private float phi = 1.2f;
        private float theta = -0.25f;
        private float zoom = 0.9f;
        private Vector3 cameraTarget = new Vector3(0.5f, 0.0f, 0.5f);
        private Vector3 newTarget = new Vector3(0.5f, 0, 0.5f);
        public int Seed
        {
            get { return MathFunctions.Seed; }
            set { MathFunctions.Seed = value; }
        }

        public ProgressBar Progress { get; set; }
        public string OverworldDirectory = "Worlds";
        public ComboBox WorldSizeBox { get; set; }

        public ComboBox ViewSelectionBox;

        public WorldGeneratorState(DwarfGame game, GameStateManager stateManager) :
            base(game, "WorldGeneratorState", stateManager)
        {
            ImageMutex = new Mutex();
            Input = new InputManager();
            Seed = DwarfTime.LastTime.TotalRealTime.Milliseconds;
            Settings = new WorldGenerationSettings()
            {
                Width = 512,
                Height = 512,
                Name = WorldSetupState.GetRandomWorldName(),
                NumCivilizations = 5,
                NumFaults = 3,
                NumRains = 1000,
                NumVolcanoes = 3,
                RainfallScale = 1.0f,
                SeaLevel = 0.17f,
                TemperatureScale = 1.0f
            };
            Generator = new WorldGenerator(Settings);
            Generator.Generate(game.GraphicsDevice);

            simpleEffect = new BasicEffect(game.GraphicsDevice);
            simpleEffect.EnableDefaultLighting();
            simpleEffect.LightingEnabled = false;
            simpleEffect.AmbientLightColor = new Vector3(1, 1, 1);
            simpleEffect.FogEnabled = false;
        }



        public Point ScreenToWorld(Vector2 screenCoord)
        {
            Rectangle imageBounds = MapPanel.GetImageBounds();
            Viewport port = new Viewport(imageBounds);
            port.MinDepth = 0.0f;
            port.MaxDepth = 1.0f;
            Vector3 rayStart = port.Unproject(new Vector3(screenCoord.X, screenCoord.Y, 0.0f), ProjMatrix, ViewMatrix, Matrix.Identity);
            Vector3 rayEnd = port.Unproject(new Vector3(screenCoord.X, screenCoord.Y, 1.0f), ProjMatrix,
                ViewMatrix, Matrix.Identity);
            Vector3 bearing = (rayEnd - rayStart);
            bearing.Normalize();
            Ray ray = new Ray(rayStart, bearing);
            Plane worldPlane = new Plane(Vector3.Zero, Vector3.Forward, Vector3.Right);
            float? dist = ray.Intersects(worldPlane);

            if (dist.HasValue)
            {
                Vector3 pos = rayStart + bearing * dist.Value;
                return new Point((int)(pos.X * Overworld.Map.GetLength(0)), (int)(pos.Z * Overworld.Map.GetLength(1)));
            }
            else
            {
                return new Point(0, 0);
            }
        }

        public Point WorldToScreen(Point worldCoord, ref bool valid)
        {
            Rectangle imageBounds = MapPanel.GetImageBounds();
            Viewport port = new Viewport(imageBounds);
            Vector3 worldSpace = new Vector3((float)worldCoord.X / Overworld.Map.GetLength(0), 0, (float)worldCoord.Y / Overworld.Map.GetLength(1));
            Vector3 screenSpace = port.Project(worldSpace, ProjMatrix, ViewMatrix, Matrix.Identity);
            valid = screenSpace.Z < 0.999f;
            return new Point((int)screenSpace.X, (int)screenSpace.Y);
        }

        public void DrawMesh(GameTime time)
        {
            if (simpleEffect != null && Generator.CurrentState == WorldGenerator.GenerationState.Finished)
            {
                GUI.Graphics.SetRenderTarget(MapPanel.Image);
                simpleEffect.World = Matrix.Identity;
                Matrix cameraRotation = Matrix.CreateRotationX(phi) * Matrix.CreateRotationY(theta);
                ViewMatrix = Matrix.CreateLookAt(zoom * Vector3.Transform(Vector3.Forward, cameraRotation) + cameraTarget, cameraTarget, Vector3.Up);
                cameraTarget = newTarget*0.1f + cameraTarget*0.9f;
                ProjMatrix = Matrix.CreatePerspectiveFieldOfView(1.5f, (float)MapPanel.GetImageBounds().Width / (float)MapPanel.GetImageBounds().Height, 0.01f, 3.0f);
                simpleEffect.View = ViewMatrix;
                simpleEffect.Projection = ProjMatrix;
                simpleEffect.TextureEnabled = true;
                simpleEffect.Texture = worldMap;
                GUI.Graphics.Clear(ClearOptions.Target, Color.Transparent, 3.0f, 0);

                foreach (EffectPass pass in simpleEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GUI.Graphics.SetVertexBuffer(Generator.LandMesh);
                    GUI.Graphics.Indices = Generator.LandIndex;
                    GUI.Graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Generator.LandMesh.VertexCount, 0,
                        Generator.LandIndex.IndexCount/3);
                }
                GUI.Graphics.SetRenderTarget(null);
                GUI.Graphics.Textures[0] = null;
                GUI.Graphics.Indices = null;
                GUI.Graphics.SetVertexBuffer(null);
            }
        }

        public override void OnEnter()
        {
            MathFunctions.Random = new ThreadSafeRandom(Seed);

            Overworld.Volcanoes = new List<Vector2>();

            DefaultFont = Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
            GUI = new DwarfGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);
            IsInitialized = true;
            MainWindow = new Panel(GUI, GUI.RootComponent)
            {
                Mode = Panel.PanelMode.Fancy,
                LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2)
            };

            int layoutWidth = 8;
            int layoutHeight = 12;
            GridLayout layout = new GridLayout(GUI, MainWindow, layoutHeight, layoutWidth)
            {
                LocalBounds = new Rectangle(0, 0, MainWindow.LocalBounds.Width, MainWindow.LocalBounds.Height)
            };

            Button startButton = new Button(GUI, layout, "Start!", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Check))
            {
                ToolTip = "Start the game with the currently generated world."
            };

            startButton.OnClicked += StartButtonOnClick;

            Button saveButton = new Button(GUI, layout, "Save", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Save))
            {
                ToolTip = "Save the generated world to a file."
            };
            saveButton.OnClicked += saveButton_OnClicked;

            Button exitButton = new Button(GUI, layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow))
            {
                ToolTip = "Back to the main menu."
            };

            exitButton.OnClicked += ExitButtonOnClick;


            MapPanel = new RenderPanel(GUI, layout, new RenderTarget2D(GUI.Graphics, 1280, 720, false, SurfaceFormat.Color, DepthFormat.Depth16))
            {
                ToolTip = "Map of the world.\nClick to select a location to embark.",
                KeepAspectRatio = true
            };
            MapPanel.OnScrolled += MapPanel_OnScrolled;
            MapPanel.OnDragged += MapPanel_OnDragged;

            AlignLayout mapLayout = new AlignLayout(GUI, MapPanel)
            {
                HeightSizeMode = GUIComponent.SizeMode.Fit,
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                Mode = AlignLayout.PositionMode.Percent
            };
            Label nameLabel = new Label(GUI, mapLayout, Settings.Name, GUI.DefaultFont)
            {
                TextColor = Color.Black,
                StrokeColor = Color.Transparent
            };
            mapLayout.Add(nameLabel, AlignLayout.Alignment.Left, AlignLayout.Alignment.Top, Vector2.Zero);
            ColorKeys = new ColorKey(GUI, mapLayout)
            {
                ColorEntries = Overworld.HeightColors
            };
            mapLayout.Add(ColorKeys, AlignLayout.Alignment.Right, AlignLayout.Alignment.Top, Vector2.Zero);


            CloseupPanel = new ImagePanel(GUI, mapLayout, new ImageFrame(worldMap, new Rectangle(0, 0, 128, 128)))
            {
                KeepAspectRatio = true,
                LocalBounds = new Rectangle(0, 0, 128, 128),
                ToolTip = "Closeup of the colony location"
            };
            mapLayout.Add(CloseupPanel, AlignLayout.Alignment.Right, AlignLayout.Alignment.Bottom, Vector2.Zero);


            MapPanel.OnLeftClicked += OnMapClick;

            layout.UpdateSizes();

            GroupBox mapProperties = new GroupBox(GUI, layout, "");

            FormLayout mapPropertiesLayout = new FormLayout(GUI, mapProperties);

            ComboBox worldSizeBox = new ComboBox(GUI, mapPropertiesLayout)
            {
                ToolTip = "Size of the colony spawn area."
            };
            

            worldSizeBox.AddValue("Tiny");
            worldSizeBox.AddValue("Small");
            worldSizeBox.AddValue("Medium");
            worldSizeBox.AddValue("Large");
            worldSizeBox.AddValue("Huge");
            worldSizeBox.CurrentIndex = 1;

            worldSizeBox.OnSelectionModified += worldSizeBox_OnSelectionModified;
            mapPropertiesLayout.AddItem("Colony Size ", worldSizeBox);

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
            mapPropertiesLayout.AddItem("Display", ViewSelectionBox);

            Progress = new ProgressBar(GUI, layout, 0.0f);

            ComboBox embarkCombo = new ComboBox(GUI, mapPropertiesLayout);

            foreach (var embark in Embarkment.EmbarkmentLibrary)
            {
                embarkCombo.AddValue(embark.Key);
            }

            embarkCombo.OnSelectionModified += embarkCombo_OnSelectionModified;


            mapPropertiesLayout.AddItem("Difficulty", embarkCombo);

            embarkCombo.CurrentValue = "Normal";
            embarkCombo.InvokeSelectionModified();

            ViewSelectionBox.OnSelectionModified += DisplayModeModified;


            Button advancedButton = new Button(GUI, mapPropertiesLayout, "Advanced...", GUI.DefaultFont,
                Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Advanced map generation settings"
            };
            advancedButton.OnClicked += advancedButton_OnClicked;
            mapPropertiesLayout.AddItem("", advancedButton);

            Button regenButton = new Button(GUI, mapPropertiesLayout, "Roll the dice!", GUI.DefaultFont,
                Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Regenerate the map"
            };
         
            regenButton.OnClicked += regenButton_OnClicked;
            mapPropertiesLayout.AddItem("", regenButton);


            layout.SetComponentPosition(MapPanel, 0, 0, layoutWidth - 2, layoutHeight - 2);
            layout.SetComponentPosition(exitButton, 0, layoutHeight - 1, 1, 1);
            layout.SetComponentPosition(saveButton, 1, layoutHeight - 1, 1, 1);
            layout.SetComponentPosition(startButton, 2, layoutHeight - 1, 1, 1);
            layout.SetComponentPosition(mapProperties, layoutWidth - 2, 0, 2, layoutHeight);
            layout.SetComponentPosition(Progress, 0, layoutHeight - 2, layoutWidth - 2, 1);
            base.OnEnter();
        }

        internal void LoadDummyGenerator(Color[] color, GraphicsDevice Device)
        {
            Generator.Abort();
            Generator = new WorldGenerator(Settings);
            Generator.LoadDummy(color, Device);
           
        }

        void regenButton_OnClicked()
        {
            Seed = MathFunctions.Random.Next();
            Generator.Abort();
            Generator = new WorldGenerator(Settings);
            Generator.Generate(Game.GraphicsDevice);
        }

        void advancedButton_OnClicked()
        {
            WorldSetupState setup = StateManager.GetState<WorldSetupState>();

            if (setup != null)
            {
                setup.Settings = Settings;
                StateManager.ReinsertState(setup);
            }
            else
            {
                StateManager.PushState(new WorldSetupState(Game, Game.StateManager, Settings));   
            }
        }

        void embarkCombo_OnSelectionModified(string arg)
        {
            Settings.InitalEmbarkment = Embarkment.EmbarkmentLibrary[arg];
        }

        void MapPanel_OnDragged(InputManager.MouseButton button, Vector2 delta)
        {
            if (button == InputManager.MouseButton.Right)
            {
                phi += delta.Y * 0.01f;
                theta -= delta.X * 0.01f;
                phi = Math.Max(phi, 0.5f);
                phi = Math.Min(phi, 1.5f);
            }
        }

        void MapPanel_OnScrolled(int amount)
        {
            zoom = Math.Min((float)Math.Max(zoom + amount*0.001f, 0.1f), 1.5f);
        }

        void worldSizeBox_OnSelectionModified(string arg)
        {
            switch (arg)
            {
                case "Tiny":
                    Settings.ColonySize = new Point3(4, 1, 4);
                    break;
                case "Small":
                    Settings.ColonySize = new Point3(8, 1, 8);
                    break;
                case "Medium":
                    Settings.ColonySize = new Point3(10, 1, 10);
                    break;
                case "Large":
                    Settings.ColonySize = new Point3(16, 1, 16);
                    break;
                case "Huge":
                    Settings.ColonySize = new Point3(24, 1, 24);
                    break;
            }
            float w = Settings.ColonySize.X * Settings.WorldScale;
            float h = Settings.ColonySize.Z * Settings.WorldScale;
            float clickX = Math.Max(Math.Min(Settings.WorldGenerationOrigin.X, Settings.Width - w - 1), w + 1);
            float clickY = Math.Max(Math.Min(Settings.WorldGenerationOrigin.Y, Settings.Height - h - 1), h + 1);

            Settings.WorldGenerationOrigin = new Vector2((int)(clickX), (int)(clickY));
        }


        private void saveButton_OnClicked()
        {
            if(Generator.CurrentState == WorldGenerator.GenerationState.Finished)
            {
                System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + "Worlds" + ProgramData.DirChar + Settings.Name);
                OverworldFile file = new OverworldFile(Game.GraphicsDevice, Overworld.Map, Settings.Name, Settings.SeaLevel);
                file.WriteFile(worldDirectory.FullName + ProgramData.DirChar + "world." + OverworldFile.CompressedExtension, true, true);
                file.SaveScreenshot(worldDirectory.FullName + ProgramData.DirChar + "screenshot.png");
                Dialog.Popup(GUI, "Save", "File saved.", Dialog.ButtonType.OK);
            }
        }


        public override void OnExit()
        {
            Generator.Abort();

            if (worldMap != null && !worldMap.IsDisposed)
            {
                worldMap.Dispose();
                worldMap = null;
            }
            if (GUI != null)
            {
                GUI.RootComponent.ClearChildren();
                GUI = null;
            }

            base.OnExit();
        }

        public void StartButtonOnClick()
        {
            if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                return;

                Overworld.Name = Settings.Name;
                GUI.MouseMode = GUISkin.MousePointer.Wait;
                StateManager.ClearState();
                Settings.ExistingFile = null;
                Settings.WorldOrigin = Settings.WorldGenerationOrigin;
                StateManager.PushState(new LoadState(Game, StateManager, Settings));

                Settings.Natives = Generator.NativeCivilizations;
        }

        public void ExitButtonOnClick()
        {
            StateManager.PopState();
        }


        public void OnMapClick()
        {
            Rectangle imageBounds = MapPanel.GetImageBounds();
            MouseState ms = Mouse.GetState();
            if (!imageBounds.Contains(ms.X, ms.Y))
            {
                return;
            }

            Point worldPos = ScreenToWorld(new Vector2(ms.X, ms.Y));

            float w = Settings.ColonySize.X * Settings.WorldScale;
            float h = Settings.ColonySize.Z * Settings.WorldScale;
            float clickX = worldPos.X;
            float clickY = worldPos.Y;
            clickX = Math.Max(Math.Min(clickX, Settings.Width - w - 1), w + 1);
            clickY = Math.Max(Math.Min(clickY, Settings.Height - h - 1), h + 1 );
           
            Settings.WorldGenerationOrigin = new Vector2((int)(clickX), (int)(clickY));
        }

        public void DisplayModeModified(string type)
        {
            if(Generator.CurrentState == WorldGenerator.GenerationState.Generating)
                return;
            
            switch (type)
            {
                case "Height":
                    ColorKeys.ColorEntries = Overworld.HeightColors;
                    Overworld.TextureFromHeightMap(type, Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, Generator.worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Biomes":
                    ColorKeys.ColorEntries = BiomeLibrary.CreateBiomeColors();
                    Overworld.TextureFromHeightMap(type, Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, Generator.worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Temp.":
                    ColorKeys.ColorEntries = Overworld.JetColors;
                    Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Temperature, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, Generator.worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Rain":
                    ColorKeys.ColorEntries = Overworld.JetColors;
                    Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Rainfall, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, Generator.worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Erosion":
                    ColorKeys.ColorEntries = Overworld.JetColors;
                    Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Erosion, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, Generator.worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Faults":
                    ColorKeys.ColorEntries = Overworld.JetColors;
                    Overworld.TextureFromHeightMap("Gray", Overworld.Map, Overworld.ScalarFieldType.Faults, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, Generator.worldData, worldMap, Settings.SeaLevel);
                    break;
                case "Factions":
                    ColorKeys.ColorEntries = Generator.GenerateFactionColors();
                    Overworld.NativeFactions = Generator.NativeCivilizations;
                    Overworld.TextureFromHeightMap(type, Overworld.Map, Overworld.ScalarFieldType.Factions, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), MapPanel.Lock, Generator.worldData, worldMap, Settings.SeaLevel);
                    break;
            }
        }

        public override void Update(DwarfTime gameTime)
        {
            if (Object.ReferenceEquals(StateManager.CurrentState, this))
            {
                GUI.Update(gameTime);
                Input.Update();

                GUI.EnableMouseEvents = Generator.CurrentState == WorldGenerator.GenerationState.Finished;
                Progress.Value = Generator.Progress;
            }

            if (!GUI.Graphics.IsDisposed)
            {
                GUI.Graphics.DepthStencilState = DepthStencilState.Default;
                GUI.Graphics.BlendState = BlendState.Opaque;
                DrawMesh(gameTime.ToGameTime());
            }
            base.Update(gameTime);
        }

        public Rectangle GetSpawnRectangle()
        {
            int w = (int) (Settings.ColonySize.X * Settings.WorldScale);
            int h = (int) (Settings.ColonySize.Z * Settings.WorldScale);
            return new Rectangle((int)Settings.WorldGenerationOrigin.X - w, (int)Settings.WorldGenerationOrigin.Y - h, w * 2, h * 2);
        }

        public void GetSpawnRectangleOnImage(ref Point a, ref Point b, ref Point c, ref Point d, ref bool valid)
        {
            Rectangle spawnRect = GetSpawnRectangle();
            Point worldA = new Point(spawnRect.X, spawnRect.Y);
            Point worldB = new Point(spawnRect.X + spawnRect.Width, spawnRect.Y);
            Point worldC = new Point(spawnRect.X + spawnRect.Width, spawnRect.Height + spawnRect.Y);
            Point worldD = new Point(spawnRect.X, spawnRect.Height + spawnRect.Y);
            newTarget = new Vector3((worldA.X + worldC.X) / (float)Overworld.Map.GetLength(0), 0, (worldA.Y + worldC.Y) / (float)(Overworld.Map.GetLength(1))) * 0.5f;
            bool validA = true;
            bool validB = true;
            bool validC = true;
            bool validD = true;
            a = WorldToScreen(worldA, ref validA);
            b = WorldToScreen(worldB, ref validB);
            c = WorldToScreen(worldC, ref validC);
            d = WorldToScreen(worldD, ref validD);
            valid = validA && validB && validC && validD;
        }

        private void DrawGUI(DwarfTime gameTime, float dx)
        {
            GUI.Graphics.SetRenderTarget(null);
            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp,
                null, null);
            Drawer2D.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(0, dx));

            Progress.Message = Generator.CurrentState == WorldGenerator.GenerationState.Finished ? "" : Generator.LoadingMessage;

            if(Generator.CurrentState == WorldGenerator.GenerationState.Finished)
            {
                Rectangle imageBounds = MapPanel.GetImageBounds();
                float scaleX = ((float)imageBounds.Width / (float)Settings.Width);
                float scaleY = ((float)imageBounds.Height / (float)Settings.Height);
                Rectangle spawnRect = GetSpawnRectangle();
                Point a = new Point(), b = new Point(), c= new Point(), d = new Point();
                bool valid = true;
                GetSpawnRectangleOnImage(ref a, ref b, ref c, ref d, ref valid);
                if (valid)
                {
                    Drawer2D.DrawPolygon(DwarfGame.SpriteBatch, Color.Yellow, 1,
                        new List<Vector2>()
                        {
                            new Vector2(a.X, a.Y),
                            new Vector2(b.X, b.Y),
                            new Vector2(c.X, c.Y),
                            new Vector2(d.X, d.Y),
                            new Vector2(a.X, a.Y)
                        });
                    Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, "Spawn", DefaultFont, new Vector2(a.X - 5, a.Y - 20),
                        Color.White, Color.Black);
                }
                //ImageMutex.WaitOne();
                /*
                DwarfGame.SpriteBatch.Draw(MapPanel.Image.Image,
                    new Rectangle(MapPanel.GetImageBounds().Right + 2, MapPanel.GetImageBounds().Top,  spawnRect.Width * 4, spawnRect.Height * 4),
                   spawnRect, Color.White);
                 */
                //ImageMutex.ReleaseMutex();
                CloseupPanel.Image.Image = worldMap;
                CloseupPanel.Image.SourceRect = spawnRect;


                if (ViewSelectionBox.CurrentValue == "Factions")
                {
                    foreach (Faction civ in Generator.NativeCivilizations)
                    {
                        bool validProj = false;
                        Point start = WorldToScreen(civ.Center, ref validProj);

                        if (validProj)
                        {
                            Vector2 measure = Datastructures.SafeMeasure(GUI.SmallFont, civ.Name);
                            Rectangle snapped =
                                MathFunctions.SnapRect(
                                    new Vector2(start.X, start.Y) -
                                    measure*0.5f, measure, imageBounds);

                            Drawer2D.DrawStrokedText(DwarfGame.SpriteBatch, civ.Name, GUI.SmallFont,
                                new Vector2(snapped.X, snapped.Y), Color.White, Color.Black);
                        }
                    }
                }
            }

            GUI.PostRender(gameTime);
            DwarfGame.SpriteBatch.End();
        }
      
        public override void Render(DwarfTime gameTime)
        {
            Game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            DrawGUI(gameTime, 0);
            base.Render(gameTime);
        }

        public void Dispose()
        {
            if (ImageMutex != null)
                ImageMutex.Dispose();   
            if (!worldMap.IsDisposed)
                worldMap.Dispose();
        }
    }
}
