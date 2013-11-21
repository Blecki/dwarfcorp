using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    public class WorldLoaderState : GameState
    {
        public class WorldLoadDescriptor
        {
            public string FileName { get; set; }
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

        public SillyGUI GUI { get; set; }
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
            for(int i = min; i < max; i++)
            {
                if(ExitThreads)
                {
                    break;
                }

                Worlds[i].Lock.WaitOne();
                if(!Worlds[i].IsLoaded)
                {
                    Worlds[i].Lock.ReleaseMutex();

                    Worlds[i].File = new OverworldFile(Worlds[i].FileName, true);

                    Worlds[i].Lock.WaitOne();
                    Worlds[i].Button.Image = new ImageFrame(Worlds[i].File.Data.CreateTexture(Game.GraphicsDevice, 256, 256));
                    Worlds[i].Button.Mode = Button.ButtonMode.ImageButton;
                    Worlds[i].Button.KeepAspectRatio = true;
                    Worlds[i].IsLoaded = true;
                    Worlds[i].Button.Text = Worlds[i].File.Data.Name;
                    Worlds[i].Lock.ReleaseMutex();
                }
                else
                {
                    Worlds[i].Lock.ReleaseMutex();
                }
            }
        }


        public void LoadWorlds()
        {
            ExitThreads = false;
            try
            {
                System.IO.DirectoryInfo worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + System.IO.Path.DirectorySeparatorChar + OverworldDirectory);
                foreach(System.IO.FileInfo file in worldDirectory.EnumerateFiles("*." + OverworldFile.CompressedExtension))
                {
                    WorldLoadDescriptor descriptor = new WorldLoadDescriptor
                    {
                        FileName = file.FullName
                    };
                    Worlds.Add(descriptor);
                }
            }
            catch(System.IO.IOException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }

        public void CreateWorldPictures(SillyGUIComponent parent, int cols)
        {
            scrollGrid.ClearChildren();

            foreach(WorldLoadDescriptor overworld in Worlds)
            {
                Button image = new Button(GUI, parent, "Loading...", GUI.DefaultFont, Button.ButtonMode.ImageButton, null)
                {
                    TextColor = Color.Black,
                    ToggleTint = new Color(255, 255, 150)
                };

                overworld.Button = image;
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
                FitToParent = false
            };

            CreateWorldPictures(scrollGrid, cols);


            CreateLoadThreads(4);

            PropertiesPanel = new GroupBox(GUI, layout, "Selected");
            
            layout.SetComponentPosition(PropertiesPanel, 3, 1, 1, 8);

            Button back = new Button(GUI, layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.LeftArrow));
            layout.SetComponentPosition(back, 3, 9, 1, 1);
            back.OnClicked += back_OnClicked;
        }

        public void LoadDescriptor(WorldLoadDescriptor descriptor)
        {
            lock (descriptor.Lock)
            {
                if (!descriptor.IsLoaded)
                {
                    return;
                }

                Overworld.Map = descriptor.File.Data.CreateMap();
                Overworld.Name = descriptor.File.Data.Name;
                PlayState.WorldWidth = Overworld.Map.GetLength(1);
                PlayState.WorldHeight = Overworld.Map.GetLength(0);

                WorldGeneratorState state = (WorldGeneratorState)(StateManager.States["WorldGeneratorState"]);

                WorldGeneratorState.worldMap = descriptor.File.Data.CreateTexture(Game.GraphicsDevice, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1));
                JoinThreads();
                StateManager.PushState("WorldGeneratorState");
                state.Progress.Value = 1.0f;
                state.GenerationComplete = true;
                Worlds.Clear();
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

            ImagePanel worldPanel = new ImagePanel(GUI, layout, SelectedDescriptor.Button.Image);
            layout.SetComponentPosition(worldPanel, 0, 1, 1, 1);

            Label worldLabel = new Label(GUI, PropertiesPanel, SelectedDescriptor.File.Data.Name, GUI.DefaultFont);
            layout.SetComponentPosition(worldLabel, 0, 2, 1, 1);

            Button loadButton = new Button(GUI, layout, "Load", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Save));
            layout.SetComponentPosition(loadButton, 0, 3, 1, 1);

            loadButton.OnClicked += loadButton_OnClicked;

            Button deleteButton = new Button(GUI, layout, "Delete", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Ex));
            layout.SetComponentPosition(deleteButton, 0, 4, 1, 1);

            deleteButton.OnClicked += deleteButton_OnClicked;
        }

        void deleteButton_OnClicked()
        {
            DeleteDescriptor(SelectedDescriptor);
        }

        public void DeleteDescriptor(WorldLoadDescriptor selectedDescriptor)
        {
            Worlds.Remove(selectedDescriptor);
            int cols = Math.Max(scroller.LocalBounds.Width / 256, 1);
            CreateWorldPictures(scrollGrid, cols);
            PropertiesPanel.ClearChildren();
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
            DefaultFont = Game.Content.Load<SpriteFont>("Default");
            GUI = new SillyGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>("Title"), Game.Content.Load<SpriteFont>("Small"), Input);
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

        public override void Update(GameTime gameTime)
        {
            iter++;
            Input.Update();
            GUI.Update(gameTime);
            Game.IsMouseVisible = true;

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


        private void DrawGUI(GameTime gameTime, float dx)
        {
            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizerState);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));
            DwarfGame.SpriteBatch.End();

            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
        }

        public override void Render(GameTime gameTime)
        {
            switch(Transitioning)
            {
                case TransitionMode.Running:
                    Game.GraphicsDevice.Clear(Color.Black);
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