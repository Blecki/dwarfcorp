// WorldLoaderState.cs
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
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This game state allows the player to load generated worlds from files.
    /// </summary>
    public class WorldLoaderState : GameState
    {
        public class WorldLoadDescriptor
        {
            public string DirectoryName { get; set; }
            public string FileName { get; set; }
            public string ScreenshotName { get; set; }

            public string WorldName { get; set; }

            public OverworldFile File { get; set; }
            public Button Button { get; set; }
            public bool IsLoaded { get; set; }
            public Mutex Lock { get; set; }

            public WorldLoadDescriptor()
            {
                IsLoaded = false;
                Lock = new Mutex();
            }
        }

        public DwarfGUI GUI { get; set; }
        public InputManager Input { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public string OverworldDirectory = "Worlds";
        public List<WorldLoadDescriptor> Worlds { get; set; }
        public bool ExitThreads { get; set; }
        public List<Thread> Threads { get; set; }

        public GroupBox PropertiesPanel { get; set; }

        public WorldLoadDescriptor SelectedDescriptor { get; set; }

        public WorldLoaderState(DwarfGame game, GameStateManager stateManager) :
            base(game, "WorldLoaderState", stateManager)
        {
            IsInitialized = false;
            Worlds = new List<WorldLoadDescriptor>();
            ExitThreads = false;
            Threads = new List<Thread>();
        }

        public void JoinThreads()
        {
            ExitThreads = true;
            foreach(Thread t in Threads)
            {
                t.Join();
            }

            Threads.Clear();
        }

        public void WorldLoaderThread(int min, int max)
        {
#if CREATE_CRASH_LOGS
            try
#endif
            {
                for (int i = min; i < max; i++)
                {
                    if (ExitThreads)
                    {
                        break;
                    }

                    Worlds[i].Lock.WaitOne();
                    if (!Worlds[i].IsLoaded)
                    {
                        Worlds[i].Lock.ReleaseMutex();

                        //Worlds[i].File = new OverworldFile(Worlds[i].FileName, true);
                        Worlds[i].File = new OverworldFile();
                        Worlds[i].File.Data = new OverworldFile.OverworldData();

                        Worlds[i].Lock.WaitOne();
                        try
                        {
                            Worlds[i].File.Data.Screenshot = TextureManager.LoadInstanceTexture(Worlds[i].ScreenshotName);
                            if (Worlds[i].File.Data.Screenshot != null)
                            {
                                Worlds[i].Button.Image = new ImageFrame(Worlds[i].File.Data.Screenshot);
                                Worlds[i].Button.Mode = Button.ButtonMode.ImageButton;
                                Worlds[i].Button.KeepAspectRatio = true;
                                Worlds[i].Button.Text = Worlds[i].WorldName;
                                Worlds[i].Button.TextColor = Color.Black;
                            }
                            else
                            {
                                Worlds[i].Button.Text = Worlds[i].WorldName;
                            }

                        }
                        catch (Exception e)
                        {
                            Worlds[i].Button.Text = "ERROR " + Worlds[i].WorldName;
                            Console.Error.WriteLine(e.Message);
                        }
                        Worlds[i].Lock.ReleaseMutex();
                    }
                    else
                    {
                        Worlds[i].Lock.ReleaseMutex();
                    }

                    Worlds[i].IsLoaded = true;
                }
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif
        }


        public void LoadWorlds()
        {
            ExitThreads = false;
            try
            {
                System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + OverworldDirectory);
                foreach(System.IO.DirectoryInfo file in worldDirectory.EnumerateDirectories())
                {
                    WorldLoadDescriptor descriptor = new WorldLoadDescriptor
                    {
                        DirectoryName = file.FullName,
                        WorldName = file.FullName.Split(ProgramData.DirChar).Last(),
                        ScreenshotName = file.FullName + ProgramData.DirChar + "screenshot.png",
                        FileName = file.FullName + ProgramData.DirChar + "world." + OverworldFile.CompressedExtension,
                    };
                    Worlds.Add(descriptor);
                }
            }
            catch(System.IO.IOException exception)
            {
                Console.Error.WriteLine(exception.Message);
                Dialog.Popup(GUI, "Error.", "Error loading worlds:\n" + exception.Message, Dialog.ButtonType.OK);
            }
        }

        public void CreateWorldPictures(GUIComponent parent, int cols)
        {
            scrollGrid.ClearChildren();

            foreach(WorldLoadDescriptor overworld in Worlds)
            {
                if(overworld.Button == null)
                {
                    Button image = new Button(GUI, parent, "Loading...", GUI.DefaultFont, Button.ButtonMode.ImageButton, null)
                    {
                        TextColor = Color.Black,
                        ToggleTint = new Color(255, 255, 150)
                    };

                    overworld.Button = image;
                }
                else
                {
                    overworld.Button = new Button(GUI, parent, overworld.Button.Text, overworld.Button.TextFont, overworld.Button.Mode, overworld.Button.Image)
                    {
                        TextColor = Color.Black,
                        KeepAspectRatio = true,
                        DontMakeBigger = true
                    };
                }
            }

            for (int i = 0; i < Worlds.Count; i++)
            {
                Button worldPicture = Worlds[i].Button;
                int y = (int)(i / cols);
                int x = i - (y * cols);
                scrollGrid.SetComponentPosition(worldPicture, x, y, 1, 1);
                int j = i;
                worldPicture.OnClicked += () => worldPicture_OnClicked(j);
            }

            if(Worlds.Count == 0)
            {
                Label apology = new Label(GUI, scrollGrid, "No files found...", GUI.DefaultFont);
                scrollGrid.SetComponentPosition(apology, 0, 0, 1, 1);
            }
        }

        public void CreateLoadThreads(int num)
        {
            int numPerThread = (int) Math.Ceiling(Math.Max(((float) Worlds.Count / (float) num), 1.0f));
            for(int i = 0; i < num; i++)
            {
                int min = Math.Min((numPerThread) * i, Worlds.Count);
                int max = Math.Min(min + numPerThread, Worlds.Count);

                if(max - min > 0)
                {
                    Thread loadThread = new Thread(() => WorldLoaderThread(min, max));
                    loadThread.Start();
                    Threads.Add(loadThread);
                }
            }
        }

        public void CreateGUI()
        {
            GUI.RootComponent.ClearChildren();
            Worlds.Clear();
            const int edgePadding = 32;
            Panel mainWindow = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(edgePadding, edgePadding, Game.GraphicsDevice.Viewport.Width - edgePadding * 2, Game.GraphicsDevice.Viewport.Height - edgePadding * 2)
            };
            GridLayout layout = new GridLayout(GUI, mainWindow, 10, 4);

            Label title = new Label(GUI, layout, "Load World", GUI.TitleFont);
            layout.SetComponentPosition(title, 0, 0, 1, 1);

            scroller = new ScrollView(GUI, layout);
            layout.SetComponentPosition(scroller, 0, 1, 3, 8);

            LoadWorlds();

            layout.UpdateSizes();

            int cols = Math.Max(scroller.LocalBounds.Width / 256, 1);
            int rows = Math.Max(Math.Max(scroller.LocalBounds.Height / 256, 1), (int) Math.Ceiling((float) Worlds.Count / (float) cols));

            scrollGrid = new GridLayout(GUI, scroller, rows, cols)
            {
                LocalBounds = new Rectangle(edgePadding, edgePadding, scroller.LocalBounds.Width - edgePadding, rows * 256),
                WidthSizeMode = GUIComponent.SizeMode.Fixed,
                HeightSizeMode = GUIComponent.SizeMode.Fixed
            };

            CreateWorldPictures(scrollGrid, cols);


            CreateLoadThreads(4);

            PropertiesPanel = new GroupBox(GUI, layout, "Selected");
            
            layout.SetComponentPosition(PropertiesPanel, 3, 1, 1, 8);

            Button back = new Button(GUI, layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow));
            layout.SetComponentPosition(back, 3, 9, 1, 1);
            back.OnClicked += back_OnClicked;
        }

        public void LoadDescriptor(WorldLoadDescriptor descriptor)
        {
            try
            {
                lock (descriptor.Lock)
                {
                    if (!descriptor.IsLoaded)
                    {
                        return;
                    }

                    descriptor.File = new OverworldFile(descriptor.FileName, true, true);

                    Overworld.Map = descriptor.File.Data.CreateMap();

                    Overworld.Name = descriptor.File.Data.Name;
                    PlayState.WorldWidth = Overworld.Map.GetLength(1);
                    PlayState.WorldHeight = Overworld.Map.GetLength(0);

                    WorldGeneratorState state = (WorldGeneratorState)(StateManager.States["WorldGeneratorState"]);

                    WorldGeneratorState.worldMap = descriptor.File.Data.CreateTexture(Game.GraphicsDevice, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1));

                    JoinThreads();
                    StateManager.PopState();
                    StateManager.PushState("WorldGeneratorState");
                    state.Progress.Value = 1.0f;
                    state.GenerationComplete = true;
                    state.DoneGenerating = true;
                    state.Settings.Name = descriptor.WorldName;
                    state.worldData = new Color[Overworld.Map.GetLength(0) * Overworld.Map.GetLength(1)];
                    state.CreateMesh();
                    Worlds.Clear();
                }
            }
            catch (Exception e)
            {

                Dialog.Popup(GUI, "ERROR", "Failed to load world: " + e.Message, Dialog.ButtonType.OK);
            }

        }

        private void UpdateSelection()
        {
            if(SelectedDescriptor == null || !SelectedDescriptor.IsLoaded)
            {
                return;
            }

            PropertiesPanel.ClearChildren();
            GridLayout layout = new GridLayout(GUI, PropertiesPanel, 5, 1);

            ImagePanel worldPanel = new ImagePanel(GUI, layout, SelectedDescriptor.Button.Image)
            {
                KeepAspectRatio = true
            };

            layout.SetComponentPosition(worldPanel, 0, 1, 1, 1);

            Label worldLabel = new Label(GUI, PropertiesPanel, SelectedDescriptor.WorldName, GUI.DefaultFont);
            layout.SetComponentPosition(worldLabel, 0, 2, 1, 1);

            Button loadButton = new Button(GUI, layout, "Load", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Save));
            layout.SetComponentPosition(loadButton, 0, 3, 1, 1);

            loadButton.OnClicked += loadButton_OnClicked;

            Button deleteButton = new Button(GUI, layout, "Delete", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Ex));
            layout.SetComponentPosition(deleteButton, 0, 4, 1, 1);

            deleteButton.OnClicked += deleteButton_OnClicked;
        }

        void deleteButton_OnClicked()
        {
            Dialog deleteDialog = Dialog.Popup(GUI, "Delete World?",
                "Are you sure you want to delete " + SelectedDescriptor.Button.Text + "?", Dialog.ButtonType.OkAndCancel);

            deleteDialog.OnClosed += deleteDialog_OnClosed;
         
        }

        void deleteDialog_OnClosed(Dialog.ReturnStatus status)
        {
            DeleteDescriptor(SelectedDescriptor);
        }

        public void DeleteDescriptor(WorldLoadDescriptor selectedDescriptor)
        {
            Worlds.Remove(selectedDescriptor);
            int cols = Math.Max(scroller.LocalBounds.Width / 256, 1);
            CreateWorldPictures(scrollGrid, cols);
            PropertiesPanel.ClearChildren();

            try
            {
                System.IO.Directory.Delete(selectedDescriptor.DirectoryName, true);
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }

        }

        void loadButton_OnClicked()
        {
            LoadDescriptor(SelectedDescriptor);
        }

        private void worldPicture_OnClicked(int picture)
        {
            SelectedDescriptor = Worlds[picture];
            UpdateSelection();
        }

        private void back_OnClicked()
        {
            StateManager.PopState();
        }

        public override void OnEnter()
        {
            DefaultFont = Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
            GUI = new DwarfGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);
            Input = new InputManager();

            CreateGUI();
            SelectedDescriptor = null;
            IsInitialized = true;
            base.OnEnter();
        }

        public override void OnExit()
        {
            JoinThreads();
            base.OnExit();
        }

        private int iter = 0;
        private GridLayout scrollGrid;
        private ScrollView scroller;

        public override void Update(DwarfTime gameTime)
        {
            iter++;
            Input.Update();
            GUI.Update(gameTime);
            GUI.IsMouseVisible = true;

            foreach(WorldLoadDescriptor t in Worlds)
            {
                t.Lock.WaitOne();

                if(!t.IsLoaded)
                {
                    t.Button.Text = "Loading";
                    for(int j = 0; j < (iter / 10) % 4; j++)
                    {
                        t.Button.Text += ".";
                    }
                }

                t.Lock.ReleaseMutex();
            }

            base.Update(gameTime);
        }


        private void DrawGUI(DwarfTime gameTime, float dx)
        {
            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, null, rasterizerState);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));
            GUI.PostRender(gameTime);
            DwarfGame.SpriteBatch.End();
            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
        }

        public override void Render(DwarfTime gameTime)
        {
            switch(Transitioning)
            {
                case TransitionMode.Running:
                    DrawGUI(gameTime, 0);
                    break;
                case TransitionMode.Entering:
                {
                    float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                    DrawGUI(gameTime, dx);
                }
                    break;
                case TransitionMode.Exiting:
                {
                    float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                    DrawGUI(gameTime, dx);
                }
                    break;
            }

            base.Render(gameTime);
        }
    }

}