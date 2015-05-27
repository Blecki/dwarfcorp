using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This game state allows the player to load saved games.
    /// </summary>
    public class GameLoaderState : GameState
    {
        public class GameLoadDescriptor
        {
            public string FileName { get; set; }
            public Button Button { get; set; }
            public bool IsLoaded { get; set; }
            public Mutex Lock { get; set; }

            public GameLoadDescriptor()
            {
                IsLoaded = false;
                Lock = new Mutex();
            }
        }

        public DwarfGUI GUI { get; set; }
        public InputManager Input { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public string SaveDirectory = "Saves";
        public List<GameLoadDescriptor> Games { get; set; }
        public bool ExitThreads { get; set; }
        public List<Thread> Threads { get; set; }

        public GroupBox PropertiesPanel { get; set; }

        public GameLoadDescriptor SelectedDescriptor { get; set; }

        public GameLoaderState(DwarfGame game, GameStateManager stateManager) :
            base(game, "GameLoaderState", stateManager)
        {
            IsInitialized = false;
            Games = new List<GameLoadDescriptor>();
            ExitThreads = false;
            Threads = new List<Thread>();
        }

        public void JoinThreads()
        {
            ExitThreads = true;
            foreach (Thread t in Threads)
            {
                t.Join();
            }

            Threads.Clear();
        }

        public void WorldLoaderThread(int min, int max)
        {
            for (int i = min; i < max; i++)
            {
                if (ExitThreads)
                {
                    break;
                }

                Games[i].Lock.WaitOne();
                if (!Games[i].IsLoaded)
                {
                    Games[i].Lock.ReleaseMutex();

                    string[] screenshots = SaveData.GetFilesInDirectory(Games[i].FileName, false, "png", "png");

                    Games[i].Lock.WaitOne();
                    Games[i].Button.Text = Games[i].FileName.Split(ProgramData.DirChar).Last();
                    if(screenshots.Length > 0)
                    {
                        Games[i].Button.Image = new ImageFrame(TextureManager.LoadInstanceTexture(screenshots[0]));
                        Games[i].Button.Mode = Button.ButtonMode.ImageButton;
                    }
                    else
                    {
                        Games[i].Button.Text += "... No image.";
                        Games[i].Button.Mode = Button.ButtonMode.PushButton;
                    }

                    Games[i].Button.KeepAspectRatio = true;
                    Games[i].IsLoaded = true;
                    
                    Games[i].Lock.ReleaseMutex();
                }
                else
                {
                    Games[i].Lock.ReleaseMutex();
                }
            }
        }


        public void LoadWorlds()
        {
            ExitThreads = false;
            try
            {
                System.IO.DirectoryInfo savedirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetGameDirectory() + ProgramData.DirChar + SaveDirectory);
                foreach (System.IO.DirectoryInfo file in savedirectory.EnumerateDirectories())
                {
                    GameLoadDescriptor descriptor = new GameLoadDescriptor
                    {
                        FileName = file.FullName
                    };
                    Games.Add(descriptor);
                }
            }
            catch (System.IO.IOException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }

        public void CreateGamePictures(GUIComponent parent, int cols)
        {
            scrollGrid.ClearChildren();

            foreach (GameLoadDescriptor overworld in Games)
            {
                if(overworld.Button == null)
                {
                    Button image = new Button(GUI, parent, "Loading...", GUI.SmallFont, Button.ButtonMode.ImageButton, null)
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
                        DontMakeBigger =  true,
                        KeepAspectRatio = true
                    };
                }
            }

            for (int i = 0; i < Games.Count; i++)
            {
                Button worldPicture = Games[i].Button;
                int y = (int)(i / cols);
                int x = i - (y * cols);
                scrollGrid.SetComponentPosition(worldPicture, x, y, 1, 1);
                int j = i;
                worldPicture.OnClicked += () => worldPicture_OnClicked(j);
            }

            if (Games.Count == 0)
            {
                Label apology = new Label(GUI, scrollGrid, "No files found...", GUI.DefaultFont);
                scrollGrid.SetComponentPosition(apology, 0, 0, 1, 1);
            }
        }

        public void CreateLoadThreads(int num)
        {
            int numPerThread = (int)Math.Ceiling(Math.Max(((float)Games.Count / (float)num), 1.0f));
            for (int i = 0; i < num; i++)
            {
                int min = Math.Min((numPerThread) * i, Games.Count);
                int max = Math.Min(min + numPerThread, Games.Count);

                if (max - min > 0)
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
            Games.Clear();
            const int edgePadding = 32;
            Panel mainWindow = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(edgePadding, edgePadding, Game.GraphicsDevice.Viewport.Width - edgePadding * 2, Game.GraphicsDevice.Viewport.Height - edgePadding * 2)
            };
            GridLayout layout = new GridLayout(GUI, mainWindow, 10, 4);

            Label title = new Label(GUI, layout, "Load Game", GUI.TitleFont);
            layout.SetComponentPosition(title, 0, 0, 1, 1);

            scroller = new ScrollView(GUI, layout);
            layout.SetComponentPosition(scroller, 0, 1, 3, 8);

            LoadWorlds();

            layout.UpdateSizes();

            int cols = Math.Max(scroller.LocalBounds.Width / 256, 1);
            int rows = Math.Max(Math.Max(scroller.LocalBounds.Height / 256, 1), (int)Math.Ceiling((float)Games.Count / (float)cols));

            scrollGrid = new GridLayout(GUI, scroller, rows, cols)
            {
                LocalBounds = new Rectangle(edgePadding, edgePadding, scroller.LocalBounds.Width - edgePadding, rows * 256),
                WidthSizeMode = GUIComponent.SizeMode.Fixed,
                HeightSizeMode = GUIComponent.SizeMode.Fixed
            };

            CreateGamePictures(scrollGrid, cols);


            CreateLoadThreads(4);

            PropertiesPanel = new GroupBox(GUI, layout, "Selected");

            layout.SetComponentPosition(PropertiesPanel, 3, 1, 1, 8);

            Button back = new Button(GUI, layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow));
            layout.SetComponentPosition(back, 3, 9, 1, 1);
            back.OnClicked += back_OnClicked;
        }

        public void LoadDescriptor(GameLoadDescriptor descriptor)
        {
            lock (descriptor.Lock)
            {
                if (!descriptor.IsLoaded)
                {
                    return;
                }


                PlayState state = (PlayState)(StateManager.States["PlayState"]);
                state.ExistingFile = descriptor.FileName;
                GUI.MouseMode = GUISkin.MousePointer.Wait;
            
                JoinThreads();
                StateManager.PopState();
                StateManager.PushState("PlayState");
                Games.Clear();
            }
        }

        private void UpdateSelection()
        {
            if (SelectedDescriptor == null || !SelectedDescriptor.IsLoaded)
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

            Label worldLabel = new Label(GUI, PropertiesPanel, SelectedDescriptor.Button.Text, GUI.DefaultFont);
            layout.SetComponentPosition(worldLabel, 0, 2, 1, 1);

            Button loadButton = new Button(GUI, layout, "Load", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                GUI.Skin.GetSpecialFrame(GUISkin.Tile.Save));

            layout.SetComponentPosition(loadButton, 0, 3, 1, 1);

            loadButton.OnClicked += loadButton_OnClicked;

            Button deleteButton = new Button(GUI, layout, "Delete", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Ex));
            layout.SetComponentPosition(deleteButton, 0, 4, 1, 1);

            deleteButton.OnClicked += deleteButton_OnClicked;
        }



        void deleteButton_OnClicked()
        {
            Dialog dialog = Dialog.Popup(GUI, "Delete?","Are you sure you want to delete " + SelectedDescriptor.Button.Text + "?", Dialog.ButtonType.OkAndCancel);
            dialog.OnClosed += dialog_OnClosed;
        }

        void dialog_OnClosed(Dialog.ReturnStatus status)
        {
            if (status == Dialog.ReturnStatus.Ok)
            {
                DeleteDescriptor(SelectedDescriptor);
            }
        }

        public void DeleteDescriptor(GameLoadDescriptor selectedDescriptor)
        {
            Games.Remove(selectedDescriptor);
            int cols = Math.Max(scroller.LocalBounds.Width / 256, 1);
            CreateGamePictures(scrollGrid, cols);
            PropertiesPanel.ClearChildren();
            System.IO.Directory.Delete(selectedDescriptor.FileName, true);
        }

        void loadButton_OnClicked()
        {
            LoadDescriptor(SelectedDescriptor);
        }

        private void worldPicture_OnClicked(int picture)
        {
            if (Games.Count > picture)
            {
                SelectedDescriptor = Games[picture];
                UpdateSelection();
            }
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

            foreach (GameLoadDescriptor t in Games)
            {
                t.Lock.WaitOne();

                if (!t.IsLoaded)
                {
                    t.Button.Text = "Loading";
                    for (int j = 0; j < (iter / 10) % 4; j++)
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
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizerState);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));
            GUI.PostRender(gameTime);
            DwarfGame.SpriteBatch.End();

            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
        }

        public override void Render(DwarfTime gameTime)
        {
            switch (Transitioning)
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