using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates
{
    public class PlayState : GameState
    {
        // Handles loading of game assets
        public ContentManager Content;

        // Interfaces with the graphics card
        public GraphicsDevice GraphicsDevice;

        public bool ShouldReset { get; set; }
        public World World { get; set; }
        public GameMaster Master
        {
            get { return World.Master; }
            set { World.Master = value; }
        }
        public static bool Paused
        {
            get { return World.Paused; }
            set { World.Paused = value; }
        }

        // Displays tips when the game is loading.
        public List<string> LoadingTips = new List<string>()
        {
            "Can't get the right angle? Hold SHIFT to move the camera around!",
            "Need to see tiny dwarves? Use the mousewheel to zoom!",
            "Press Q to quickly slice the terrain at the height of the cursor.",
            "Press E to quickly un-slice the terrain.",
            "The number keys can be used to quickly switch between tools.",
            "Employees will not work if they are unhappy.",
            "Monsters got you down? Try hiring some thugs!",
            "The most lucrative resources are beneath the earth.",
            "Dwarves can swim!",
            "Stockpiles are free!",
            "Payday occurs at midnight. Make sure to sell your goods before then!",
            "Dwarves prefer to eat in common rooms, but they will eat out of stockpiles if necessary.",
            "The minimap can be closed and opened.",
            "Monsters are shown on the minimap.",
            "Axedwarves are better at chopping trees than miners."
        };
        private Timer TipTimer = new Timer(5, false);
        private int TipIndex = 0;

        // ------GUI--------
        // Draws and manages the user interface 
        public static DwarfGUI GUI = null;
        public Panel PausePanel;
        // Text displayed on the screen for the player's company
        public Label CompanyNameLabel { get; set; }

        // Text displayed on the screen for the player's logo
        public ImagePanel CompanyLogoPanel { get; set; }

        // Text displayed on the screen for the current amount of money the player has
        public Label MoneyLabel { get; set; }

        // Text displayed on the screen for the current amount of money the player has
        public Label StockLabel { get; set; }

        // Text displayed on the screen for the current game time
        public Label TimeLabel { get; set; }

        // Text displayed on the screen for the current slice
        public Label CurrentLevelLabel { get; set; }

        // When pressed, makes the current slice increase.
        public Button CurrentLevelUpButton { get; set; }

        //When pressed, makes the current slice decrease
        public Button CurrentLevelDownButton { get; set; }

        // When dragged, the current slice changes
        public Slider LevelSlider { get; set; }

        public AnnouncementViewer AnnouncementViewer { get; set; }

        public Minimap MiniMap { get; set; }

        // Provides event-based keyboard and mouse input.
        public InputManager Input = new InputManager();

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="game">The program currently running</param>
        /// <param name="stateManager">The game state manager this state will belong to</param>
        public PlayState(DwarfGame game, GameStateManager stateManager) :
            base(game, "PlayState", stateManager)
        {
            ShouldReset = true;
            Content = Game.Content;
            GraphicsDevice = Game.GraphicsDevice;
            World = new World(game);
            World.gameState = this;
            World.OnLoadedEvent += World_OnLoadedEvent;
            World.OnLoseEvent += World_OnLoseEvent;
            Paused = false;
            RenderUnderneath = true;
        }

        private void World_OnLoseEvent()
        {
            //Paused = true;
            //StateManager.PushState("LoseState");
        }

        private void World_OnLoadedEvent()
        {
            IsInitialized = true;
            CreateGUIComponents();  // create gui components after everything has been loaded
        }

        /// <summary>
        /// Called when the PlayState is entered from the state manager.
        /// </summary>
        public override void OnEnter()
        {
            // If the game should reset, we initialize everything
            if (ShouldReset)
            {
                IsInitialized = false;
                ShouldReset = false;
                CreateGUI();
                World.Setup(GUI);
            }
            else
            {
                // Otherwise, we just unpause everything and re-enter the game.
                World.Unpause();
            }
            base.OnEnter();
        }

        /// <summary>
        /// Called when the PlayState is exited and another state (such as the main menu) is loaded.
        /// </summary>
        public override void OnExit()
        {
            World.Pause();
            base.OnExit();
        }

        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void Update(DwarfTime gameTime)
        {
            // If this playstate is not supposed to be running,
            // just exit.
            if (!Game.IsActive || !IsActiveState)
            {
                return;
            }

            World.Update(gameTime);
            GUI.Update(gameTime);
            Input.Update();

            // Updates some of the GUI status
            if (Game.IsActive)
            {
                CurrentLevelLabel.Text = "Slice: " + World.ChunkManager.ChunkData.MaxViewingLevel + "/" + World.ChunkHeight;
                TimeLabel.Text = World.Time.CurrentDate.ToShortDateString() + " " + World.Time.CurrentDate.ToShortTimeString();
            }
        }

        /// <summary>
        /// Called when a frame is to be drawn to the screen
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void Render(DwarfTime gameTime)
        {
            EnableScreensaver = !World.ShowingWorld;
            if (World.ShowingWorld)
            {
                GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
                World.Render(gameTime);

                // SpriteBatch Begin and End must be called again. Hopefully we can factor this out with the new gui
                RasterizerState rasterizerState = new RasterizerState()
                {
                    ScissorTestEnable = true
                };
                DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp,
                    null, rasterizerState);
                GUI.Render(gameTime, DwarfGame.SpriteBatch, Vector2.Zero);
                GUI.PostRender(gameTime);
                DwarfGame.SpriteBatch.End();
            }
            base.Render(gameTime);
        }

        /// <summary>
        /// If the game is not loaded yet, just draws a loading message centered
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void RenderUnitialized(DwarfTime gameTime)
        {
            TipTimer.Update(gameTime);
            if (TipTimer.HasTriggered)
            {
                World.LoadingMessageBottom = LoadingTips[World.Random.Next(LoadingTips.Count)];
                TipIndex++;
            }

            EnableScreensaver = true;
            World.Render(gameTime);
            base.RenderUnitialized(gameTime);
        }

        /// <summary>
        /// Creates the user interface + player controls.
        /// </summary>
        /// <param name="createMaster">True if the Game Master needs to be created as well.</param>
        public void CreateGUI()
        {
            IndicatorManager.SetupStandards();

            GUI = new DwarfGUI(Game, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default),
                Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title),
                Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);

            GUI.ToolTipManager.InfoLocation = new Point(Game.GraphicsDevice.Viewport.Width / 2, Game.GraphicsDevice.Viewport.Height);
            GUI.MouseMode = GUISkin.MousePointer.Wait;
        }

        /// <summary>
        /// Creates all of the sub-components of the GUI in for the PlayState (buttons, etc.)
        /// </summary>
        public void CreateGUIComponents()
        {
            GUI.RootComponent.ClearChildren();
            AlignLayout layout = new AlignLayout(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fit,
                Mode = AlignLayout.PositionMode.Percent
            };

            GUI.RootComponent.AddChild(Master.Debugger.MainPanel);
            layout.AddChild(Master.ToolBar);
            Master.ToolBar.Parent = layout;
            Master.ToolBar.LocalBounds = new Rectangle(0, 0, 256, 100);

            layout.Add(Master.ToolBar, AlignLayout.Alignment.Right, AlignLayout.Alignment.Bottom, Vector2.Zero);
            //layout.SetComponentPosition(Master.ToolBar, 7, 10, 4, 1);

            GUIComponent companyInfoComponent = new GUIComponent(GUI, layout)
            {
                LocalBounds = new Rectangle(0, 0, 350, 200),
                TriggerMouseOver = false
            };

            layout.Add(companyInfoComponent, AlignLayout.Alignment.Left, AlignLayout.Alignment.Top, Vector2.Zero);
            //layout.SetComponentPosition(companyInfoComponent, 0, 0, 4, 2);

            GUIComponent resourceInfoComponent = new ResourceInfoComponent(GUI, layout, Master.Faction)
            {
                LocalBounds = new Rectangle(0, 0, 400, 256),
                TriggerMouseOver = false
            };
            layout.Add(resourceInfoComponent, AlignLayout.Alignment.None, AlignLayout.Alignment.Top,
                new Vector2(0.55f, 0.0f));
            //layout.SetComponentPosition(resourceInfoComponent, 7, 0, 2, 2);

            GridLayout infoLayout = new GridLayout(GUI, companyInfoComponent, 3, 4);

            CompanyLogoPanel = new ImagePanel(GUI, infoLayout, World.PlayerCompany.Logo)
            {
                ConstrainSize = true,
                KeepAspectRatio = true
            };
            infoLayout.SetComponentPosition(CompanyLogoPanel, 0, 0, 1, 1);

            CompanyNameLabel = new Label(GUI, infoLayout, World.PlayerCompany.Name, GUI.DefaultFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 255),
                ToolTip = "Our company Name.",
                Alignment = Drawer2D.Alignment.Top,
            };
            infoLayout.SetComponentPosition(CompanyNameLabel, 1, 0, 1, 1);

            MoneyLabel = new DynamicLabel(GUI, infoLayout, "Money:\n", "", GUI.DefaultFont, "C2",
                () => Master.Faction.Economy.CurrentMoney)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 255),
                ToolTip = "Amount of money in our treasury.",
                Alignment = Drawer2D.Alignment.Top,
                TriggerMouseOver = false
            };
            infoLayout.SetComponentPosition(MoneyLabel, 3, 0, 1, 1);


            StockLabel = new DynamicLabel(GUI, infoLayout, "Stock:\n", "", GUI.DefaultFont, "C2",
                () => Master.Faction.Economy.Company.StockPrice)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 255),
                ToolTip = "The price of our company stock.",
                Alignment = Drawer2D.Alignment.Top,
            };
            infoLayout.SetComponentPosition(StockLabel, 5, 0, 1, 1);


            TimeLabel = new Label(GUI, layout,
                World.Time.CurrentDate.ToShortDateString() + " " + World.Time.CurrentDate.ToShortTimeString(), GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 255),
                Alignment = Drawer2D.Alignment.Top,
                ToolTip = "Current time and date."
            };
            layout.Add(TimeLabel, AlignLayout.Alignment.Center, AlignLayout.Alignment.Top, Vector2.Zero);
            //layout.SetComponentPosition(TimeLabel, 6, 0, 1, 1);

            CurrentLevelLabel = new Label(GUI, infoLayout, "Slice: " + World.ChunkManager.ChunkData.MaxViewingLevel,
                GUI.DefaultFont)
            {
                TextColor = Color.White,
                StrokeColor = new Color(0, 0, 0, 255),
                ToolTip = "The maximum height of visible terrain"
            };
            infoLayout.SetComponentPosition(CurrentLevelLabel, 0, 1, 1, 1);

            CurrentLevelUpButton = new Button(GUI, CurrentLevelLabel, "", GUI.DefaultFont, Button.ButtonMode.ImageButton,
                GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallArrowUp))
            {
                ToolTip = "Go up one level of visible terrain",
                KeepAspectRatio = true,
                DontMakeBigger = true,
                DontMakeSmaller = true,
                LocalBounds = new Rectangle(100, 16, 32, 32)
            };

            CurrentLevelUpButton.OnClicked += CurrentLevelUpButton_OnClicked;

            CurrentLevelDownButton = new Button(GUI, CurrentLevelLabel, "", GUI.DefaultFont,
                Button.ButtonMode.ImageButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallArrowDown))
            {
                ToolTip = "Go down one level of visible terrain",
                KeepAspectRatio = true,
                DontMakeBigger = true,
                DontMakeSmaller = true,
                LocalBounds = new Rectangle(140, 16, 32, 32)
            };
            CurrentLevelDownButton.OnClicked += CurrentLevelDownButton_OnClicked;

            /*
            LevelSlider = new Slider(GUI, layout, "", ChunkManager.ChunkData.MaxViewingLevel, 0, ChunkManager.ChunkData.ChunkSizeY, Slider.SliderMode.Integer)
            {
                Orient = Slider.Orientation.Vertical,
                ToolTip = "Controls the maximum height of visible terrain",
                DrawLabel = false
            };

            layout.SetComponentPosition(LevelSlider, 0, 1, 1, 6);
            LevelSlider.OnClicked += LevelSlider_OnClicked;
            LevelSlider.InvertValue = true;
            */

            MiniMap = new Minimap(GUI, layout, 192, 192, World,
                TextureManager.GetTexture(ContentPaths.Terrain.terrain_colormap),
                TextureManager.GetTexture(ContentPaths.GUI.gui_minimap))
            {
                IsVisible = true,
                LocalBounds = new Rectangle(0, 0, 192, 192)
            };
            layout.Add(MiniMap, AlignLayout.Alignment.Left, AlignLayout.Alignment.Bottom, Vector2.Zero);
            //layout.SetComponentPosition(MiniMap, 0, 8, 4, 4);
            //Rectangle rect = layout.GetRect(new Rectangle(0, 8, 4, 4));
            //layout.SetComponentOffset(MiniMap,  new Point(0, rect.Height - 250));


            Tray topRightTray = new Tray(GUI, layout)
            {
                LocalBounds = new Rectangle(0, 0, 132, 68),
                TrayPosition = Tray.Position.TopRight
            };

            Button moneyButton = new Button(GUI, topRightTray, "Economy", GUI.SmallFont, Button.ButtonMode.ImageButton,
                new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.icons), 32, 2, 1))
            {
                KeepAspectRatio = true,
                ToolTip = "Opens the Economy Menu",
                DontMakeBigger = true,
                DrawFrame = true,
                TextColor = Color.White,
                LocalBounds = new Rectangle(8, 6, 32, 32)
            };

            moneyButton.OnClicked += EconomyState.PushEconomyState;


            Button settingsButton = new Button(GUI, topRightTray, "Settings", GUI.SmallFont,
                Button.ButtonMode.ImageButton,
                new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.icons), 32, 4, 1))
            {
                KeepAspectRatio = true,
                ToolTip = "Opens the Settings Menu",
                DontMakeBigger = true,
                DrawFrame = true,
                TextColor = Color.White,
                LocalBounds = new Rectangle(64 + 8, 6, 32, 32)
            };

            settingsButton.OnClicked += OpenPauseMenu;

            layout.Add(topRightTray, AlignLayout.Alignment.Right, AlignLayout.Alignment.Top, Vector2.Zero);


            InputManager.KeyReleasedCallback -= InputManager_KeyReleasedCallback;
            InputManager.KeyReleasedCallback += InputManager_KeyReleasedCallback;

            AnnouncementViewer = new AnnouncementViewer(GUI, layout, World.AnnouncementManager)
            {
                LocalBounds = new Rectangle(0, 0, 350, 80)
            };
            layout.Add(AnnouncementViewer, AlignLayout.Alignment.Center, AlignLayout.Alignment.Bottom, Vector2.Zero);
            //layout.SetComponentPosition(AnnouncementViewer, 3, 10, 3, 1);
            layout.UpdateSizes();
        }

        /// <summary>
        /// Called when the slice slider was moved.
        /// </summary>
        private void LevelSlider_OnClicked()
        {
            World.ChunkManager.ChunkData.SetMaxViewingLevel((int)LevelSlider.SliderValue, ChunkManager.SliceMode.Y);
        }

        /// <summary>
        /// Called when the "Slice -" button is pressed
        /// </summary>
        private void CurrentLevelDownButton_OnClicked()
        {
            World.ChunkManager.ChunkData.SetMaxViewingLevel(World.ChunkManager.ChunkData.MaxViewingLevel - 1,
                ChunkManager.SliceMode.Y);
        }


        /// <summary>
        /// Called when the "Slice +" button is pressed
        /// </summary>
        private void CurrentLevelUpButton_OnClicked()
        {
            World.ChunkManager.ChunkData.SetMaxViewingLevel(World.ChunkManager.ChunkData.MaxViewingLevel + 1,
                ChunkManager.SliceMode.Y);
        }


        /// <summary>
        /// Called when the user releases a key
        /// </summary>
        /// <param name="key">The keyboard key released</param>
        private void InputManager_KeyReleasedCallback(Keys key)
        {
            if (key == ControlSettings.Mappings.Map)
            {
                World.DrawMap = !World.DrawMap;
                MiniMap.SetMinimized(!World.DrawMap);
            }

            if (key == Keys.Escape)
            {
                if (Master.CurrentToolMode != GameMaster.ToolMode.SelectUnits)
                {
                    Master.ToolBar.ToolButtons[GameMaster.ToolMode.SelectUnits].InvokeClick();
                }
                else if (PausePanel != null && PausePanel.IsVisible)
                {
                    PausePanel.IsVisible = false;
                    Paused = false;
                }
                else
                {
                    OpenPauseMenu();
                }
            }

            // Special case: number keys reserved for changing tool mode
            else if (InputManager.IsNumKey(key))
            {
                int index = InputManager.GetNum(key) - 1;


                if (index < 0)
                {
                    index = 9;
                }

                // In this special case, all dwarves are selected
                if (index == 0 && Master.SelectedMinions.Count == 0)
                {
                    Master.SelectedMinions.AddRange(Master.Faction.Minions);
                }
                int i = 0;
                if (index == 0 || Master.SelectedMinions.Count > 0)
                {
                    foreach (var pair in Master.ToolBar.ToolButtons)
                    {
                        if (i == index)
                        {
                            List<CreatureAI> minions = Faction.FilterMinionsWithCapability(Master.SelectedMinions,
                                pair.Key);

                            if ((index == 0 || minions.Count > 0))
                            {
                                pair.Value.InvokeClick();
                                break;
                            }
                        }
                        i++;
                    }

                    //Master.ToolBar.CurrentMode = modes[index];
                }
            }
            else if (key == ControlSettings.Mappings.Pause)
            {
                Paused = !Paused;
                Master.ToolBar.SpeedButton.SetSpeed(Paused ? 0 : 1);
            }
            else if (key == ControlSettings.Mappings.TimeForward)
            {
                Master.ToolBar.SpeedButton.IncrementSpeed();
            }
            else if (key == ControlSettings.Mappings.TimeBackward)
            {
                Master.ToolBar.SpeedButton.DecrementSpeed();
            }
            else if (key == ControlSettings.Mappings.ToggleGUI)
            {
                GUI.RootComponent.IsVisible = !GUI.RootComponent.IsVisible;
            }
        }

        /// <summary>
        /// Called whenever the escape button is pressed. Opens a small menu for saving/loading, etc.
        /// </summary>
        public void OpenPauseMenu()
        {
            if (PausePanel != null && PausePanel.IsVisible) return;

            Paused = true;

            int w = 200;
            int h = 200;

            PausePanel = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds =
                    new Rectangle(GraphicsDevice.Viewport.Width / 2 - w / 2, GraphicsDevice.Viewport.Height / 2 - h / 2, w, h)
            };

            GridLayout pauseLayout = new GridLayout(GUI, PausePanel, 1, 1);

            ListSelector pauseSelector = new ListSelector(GUI, pauseLayout)
            {
                Label = "-Menu-",
                DrawPanel = false,
                Mode = ListItem.SelectionMode.Selector
            };
            pauseLayout.SetComponentPosition(pauseSelector, 0, 0, 1, 1);
            pauseLayout.UpdateSizes();
            pauseSelector.AddItem("Continue");
            pauseSelector.AddItem("Options");
            pauseSelector.AddItem("Save");
            pauseSelector.AddItem("Quit");

            pauseSelector.OnItemClicked += () => pauseSelector_OnItemClicked(pauseSelector);
        }

        /// <summary>
        /// Called whenever the pause menu is clicked.
        /// </summary>
        /// <param name="selector">The list of things the user could have clicked on.</param>
        private void pauseSelector_OnItemClicked(ListSelector selector)
        {
            string selected = selector.SelectedItem.Label;
            switch (selected)
            {
                case "Continue":
                    GUI.RootComponent.RemoveChild(PausePanel);
                    Paused = false;
                    PausePanel.Destroy();
                    PausePanel = null;
                    break;
                case "Options":
                    StateManager.PushState("OptionsState");
                    break;
                case "Save":
                    SaveGame(Overworld.Name + "_" + World.GameID);
                    break;
                case "Quit":
                    QuitGame();
                    break;
            }
        }

        /// <summary>
        /// Saves the game state to a file.
        /// </summary>
        /// <param name="filename">The file to save to</param>
        public void SaveGame(string filename)
        {
            Dialog dialog = Dialog.Popup(GUI, "Saving/Loading",
                "Warning: Saving is still an unstable feature. Are you sure you want to continue?",
                Dialog.ButtonType.OkAndCancel);

            dialog.OnClosed += (status) => savedialog_OnClosed(status, filename);
        }

        private void savedialog_OnClosed(Dialog.ReturnStatus status, string filename)
        {
            switch (status)
            {
                case Dialog.ReturnStatus.Ok:
                    {
                        World.Save(filename, waitforsave_OnFinished);
                        break;
                    }
            }
        }

        private void waitforsave_OnFinished(bool success, Exception exception)
        {
            if (success)
            {
                Dialog.Popup(GUI, "Save", "File saved.", Dialog.ButtonType.OK);
            }
            else
            {
                Dialog.Popup(GUI, "Save", "File save failed : " + exception.Message, Dialog.ButtonType.OK);
            }
        }

        public void QuitGame()
        {
            StateManager.StateStack.Clear();
            MainMenuState menuState = StateManager.GetState<MainMenuState>("MainMenuState");
            menuState.IsGameRunning = false;
            World.Quit();
            StateManager.States["PlayState"] = new PlayState(Game, StateManager);
            StateManager.CurrentState = "";
            StateManager.PushState("MainMenuState");
        }
    }
}