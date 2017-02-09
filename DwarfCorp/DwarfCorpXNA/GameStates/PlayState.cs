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
        private bool IsShuttingDown { get; set; }
        private bool QuitOnNextUpdate { get; set; }
        public bool ShouldReset { get; set; }
        public static WorldManager World { get { return DwarfGame.World; } }
        public GameMaster Master
        {
            get { return DwarfGame.World.Master; }
            set { DwarfGame.World.Master = value; }
        }
        public static bool Paused
        {
            get { return DwarfGame.World.Paused; }
            set { DwarfGame.World.Paused = value; }
        }

        // ------GUI--------
        // Draws and manages the user interface 
        public static DwarfGUI GUI = null;

        private Gum.Widget MoneyLabel;
        private Gum.Widget StockLabel;
        private Gum.Widget LevelLabel;
        private Dictionary<GameMaster.ToolMode, Gum.Widget> ToolbarItems = new Dictionary<GameMaster.ToolMode, Gum.Widget>();
        private Gum.Widget BottomRightTray;
        private Gum.Widget TimeLabel;
        private Gum.Widget PausePanel;
        private NewGui.MinimapFrame MinimapFrame;
        private NewGui.MinimapRenderer MinimapRenderer;
        private NewGui.GameSpeedControls GameSpeedControls;
        private Gum.Widget ResourcePanel;
        private NewGui.InfoTray InfoTray;

        private Point RememberedScreenResolution = new Point(0, 0);
       // public Minimap MiniMap { get; set; }

        // Provides event-based keyboard and mouse input.
        public static InputManager Input;// = new InputManager();

        private Gum.Root NewGui;

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="game">The program currently running</param>
        /// <param name="stateManager">The game state manager this state will belong to</param>
        public PlayState(DwarfGame game, GameStateManager stateManager) :
            base(game, "PlayState", stateManager)
        {
            IsShuttingDown = false;
            QuitOnNextUpdate = false;
            ShouldReset = true;
            Paused = false;
            RenderUnderneath = true;

            IsInitialized = true;

            IsInitialized = false;
        }

        private void World_OnLoseEvent()
        {
            //Paused = true;
            //StateManager.PushState("LoseState");
        }

        /// <summary>
        /// Called when the PlayState is entered from the state manager.
        /// </summary>
        public override void OnEnter()
        {
            // Just toss out any pending input.
            DwarfGame.GumInputMapper.GetInputQueue();

            if (!IsInitialized)
            {
                // Setup new gui. Double rendering the mouse?
                NewGui = new Gum.Root(new Point(640, 480), DwarfGame.GumSkin);
                NewGui.MousePointer = new Gum.MousePointer("mouse", 4, 0);
                DwarfGame.World.NewGui = NewGui;

                // Setup input event handlers. All of the actions should already be established - just 
                // need handlers.
                DwarfGame.GumInput.ClearAllHandlers();

                DwarfGame.World.ShowInfo += (text) =>
                    {
                        InfoTray.AddMessage(text);
                    };

                DwarfGame.World.ShowTooltip += (text) =>
                    {
                        NewGui.ShowTooltip(NewGui.MousePosition, text);
                    };

                DwarfGame.World.SetMouse += (mouse) =>
                    {
                        NewGui.MousePointer = mouse;
                    };

                World.gameState = this;
                World.OnLoseEvent += World_OnLoseEvent;
                CreateGUIComponents();
                IsInitialized = true;
            }

            World.Unpause();
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
            //WorldManager.GUI.IsMouseVisible = false;

            if (NewGui.ResolutionChanged())
            {
                NewGui.ResizeVirtualScreen(new Point(640, 480));
                NewGui.ResetGui();
                CreateGUIComponents();

                if (PausePanel != null)
                {
                    PausePanel = null;
                    OpenPauseMenu();
                }
            }

            // If this playstate is not supposed to be running,
            // just exit.
            if (!Game.IsActive || !IsActiveState || IsShuttingDown)
            {
                return;
            }

            if (QuitOnNextUpdate)
            {
                QuitGame();
                IsShuttingDown = true;
                QuitOnNextUpdate = false;
                return;
            }

            // Needs to run before old input so tools work
            // Update new input system.
            DwarfGame.GumInput.FireActions(NewGui, (@event, args) =>
            {
                // Let old input handle mouse interaction for now. Will eventually need to be replaced.
            });

            World.Update(gameTime);
            GUI.Update(gameTime);
            Input.Update();

            #region Update time label
            TimeLabel.Text = String.Format("{0} {1}",
                DwarfGame.World.Time.CurrentDate.ToShortDateString(),
                DwarfGame.World.Time.CurrentDate.ToShortTimeString());
            TimeLabel.Invalidate();
            #endregion

            #region Update top left panel
            MoneyLabel.Text = Master.Faction.Economy.CurrentMoney.ToString();
            MoneyLabel.Invalidate();

            StockLabel.Text = Master.Faction.Economy.Company.StockPrice.ToString();
            StockLabel.Invalidate();

            LevelLabel.Text = String.Format("{0}/{1}",
                DwarfGame.World.ChunkManager.ChunkData.MaxViewingLevel,
                DwarfGame.World.ChunkHeight);
            LevelLabel.Invalidate();
            #endregion

            #region Update toolbar tray
            foreach (var tools in ToolbarItems)
            {
                tools.Value.Hidden = true;
                tools.Value.Invalidate();
            }

            if (Master.SelectedMinions.Count == 0)
            {
                if (Master.CurrentToolMode != GameMaster.ToolMode.God)
                    Master.CurrentToolMode = GameMaster.ToolMode.SelectUnits;
            }
            else
            {
                foreach (var tool in ToolbarItems)
                    tool.Value.Hidden = !Master.Faction.SelectedMinions.Any(minion => minion.Stats.CurrentClass.HasAction(tool.Key));
            }

            ToolbarItems[GameMaster.ToolMode.SelectUnits].Hidden = false;
            #endregion

            #region Update resource panel
            ResourcePanel.Clear();
            foreach (var resource in Master.Faction.ListResources().Where(p => p.Value.NumResources > 0))
            {
                var resourceTemplate = ResourceLibrary.GetResourceByName(resource.Key);

                var row = ResourcePanel.AddChild(new Gum.Widget
                    {
                        MinimumSize = new Point(0, 32),
                        AutoLayout = Gum.AutoLayout.DockTop
                    });

                row.AddChild(new Gum.Widget
                    {
                        Background = new Gum.TileReference("resources", resourceTemplate.NewGuiSprite),
                        MinimumSize = new Point(32, 32),
                        AutoLayout = Gum.AutoLayout.DockLeft,
                        Tooltip = String.Format("{0} - {1}",
                            resourceTemplate.ResourceName,
                            resourceTemplate.Description)
                    });

                row.AddChild(new Gum.Widget
                {
                    Text = resource.Value.NumResources.ToString(),
                    MinimumSize = new Point(32, 32),
                    AutoLayout = Gum.AutoLayout.DockLeft,
                    Tooltip = String.Format("{0} - {1}",
                        resourceTemplate.ResourceName,
                        resourceTemplate.Description),
                    Font = "outline-font",
                    TextVerticalAlign = Gum.VerticalAlign.Center,
                    TextColor = new Vector4(1,1,1,1)
                });
            }
            ResourcePanel.Layout();
            #endregion

            if (Paused && GameSpeedControls.CurrentSpeed != 0)
                GameSpeedControls.CurrentSpeed = 0;

            // Really just handles mouse pointer animation.
            NewGui.Update(gameTime.ToGameTime());
        }

        /// <summary>
        /// Called when a frame is to be drawn to the screen
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void Render(DwarfTime gameTime)
        {
            EnableScreensaver = !World.ShowingWorld;

            MinimapRenderer.PreRender(gameTime, DwarfGame.SpriteBatch);

            if (World.ShowingWorld)
            {
                GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
                World.Render(gameTime);

                if (!MinimapFrame.Hidden)
                    MinimapRenderer.Render(new Rectangle(0, NewGui.VirtualScreen.Bottom - 192, 192, 192), NewGui);
                NewGui.Draw();

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
                //WorldManager.SelectionBuffer.DebugDraw(0, 0);
            }        

            base.Render(gameTime);
        }

        /// <summary>
        /// If the game is not loaded yet, just draws a loading message centered
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void RenderUnitialized(DwarfTime gameTime)
        {
            EnableScreensaver = true;
            World.Render(gameTime);
            base.RenderUnitialized(gameTime);
        }

        /// <summary>
        /// Creates all of the sub-components of the GUI in for the PlayState (buttons, etc.)
        /// </summary>
        public void CreateGUIComponents()
        {
            GUI.RootComponent.ClearChildren();

            GUI.RootComponent.AddChild(Master.Debugger.MainPanel);

            #region Setup bottom right tray

            ToolbarItems[GameMaster.ToolMode.SelectUnits] = CreateIcon(5, GameMaster.ToolMode.SelectUnits);
            ToolbarItems[GameMaster.ToolMode.Dig] = CreateIcon(0, GameMaster.ToolMode.Dig);
            ToolbarItems[GameMaster.ToolMode.Build] = CreateIcon(2, GameMaster.ToolMode.Build);
            ToolbarItems[GameMaster.ToolMode.Cook] = CreateIcon(3, GameMaster.ToolMode.Cook);
            ToolbarItems[GameMaster.ToolMode.Farm] = CreateIcon(5, GameMaster.ToolMode.Farm);
            ToolbarItems[GameMaster.ToolMode.Magic] = CreateIcon(6, GameMaster.ToolMode.Magic);
            ToolbarItems[GameMaster.ToolMode.Gather] = CreateIcon(6, GameMaster.ToolMode.Gather);
            ToolbarItems[GameMaster.ToolMode.Chop] = CreateIcon(1, GameMaster.ToolMode.Chop);
            ToolbarItems[GameMaster.ToolMode.Guard] = CreateIcon(4, GameMaster.ToolMode.Guard);
            ToolbarItems[GameMaster.ToolMode.Attack] = CreateIcon(3, GameMaster.ToolMode.Attack);

            BottomRightTray = NewGui.RootItem.AddChild(new NewGui.IconTray
            {
                Corners = Gum.Scale9Corners.Left | Gum.Scale9Corners.Top,
                AutoLayout = Gum.AutoLayout.FloatBottomRight,
                ItemSource = ToolbarItems.Select(i => i.Value),
                SizeToGrid = new Point(5,2)
            });
            #endregion

            #region Setup company information section
            NewGui.RootItem.AddChild(new NewGui.CompanyLogo
                {
                    Rect = new Rectangle(8,8,32,32),
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    AutoLayout = Gum.AutoLayout.None,
                    CompanyInformation = DwarfGame.World.PlayerCompany.Information
                });

            NewGui.RootItem.AddChild(new Gum.Widget
                {
                    Rect = new Rectangle(48,8,256,20),
                    Text = DwarfGame.World.PlayerCompany.Information.Name,
                    AutoLayout = Gum.AutoLayout.None,
                    Font = "outline-font",
                    TextColor = new Vector4(1,1,1,1)
                });

            var infoPanel = NewGui.RootItem.AddChild(new Gum.Widget
            {
                Rect = new Rectangle(0, 40, 128, 102),
                AutoLayout = Gum.AutoLayout.None
            });

            var moneyRow = infoPanel.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(0, 34),
                AutoLayout = Gum.AutoLayout.DockTop
            });

            moneyRow.AddChild(new Gum.Widget
            {
                Background = new Gum.TileReference("resources", 40),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gum.AutoLayout.DockLeft
            });

            MoneyLabel = moneyRow.AddChild(new Gum.Widget
                {
                    Rect = new Rectangle(48, 32, 128, 20),
                    AutoLayout = Gum.AutoLayout.DockFill,
                    Font = "outline-font",
                    TextVerticalAlign = Gum.VerticalAlign.Center,
                    TextColor = new Vector4(1,1,1,1)
                });

            var stockRow = infoPanel.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(0, 34),
                AutoLayout = Gum.AutoLayout.DockTop
            });

            stockRow.AddChild(new Gum.Widget
            {
                Background = new Gum.TileReference("resources", 41),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gum.AutoLayout.DockLeft
            });

            StockLabel = stockRow.AddChild(new Gum.Widget
            {
                Rect = new Rectangle(48, 32, 128, 20),
                AutoLayout = Gum.AutoLayout.DockFill,
                Font = "outline-font",
                TextVerticalAlign = Gum.VerticalAlign.Center,
                TextColor = new Vector4(1, 1, 1, 1)
            });

            var levelRow = infoPanel.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(0, 34),
                AutoLayout = Gum.AutoLayout.DockTop
            });

            levelRow.AddChild(new Gum.Widget
            {
                Background = new Gum.TileReference("resources", 40), // Todo: level icon!
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gum.AutoLayout.DockLeft
            });

            levelRow.AddChild(new Gum.Widget
            {
                Background = new Gum.TileReference("round-buttons", 7),
                MinimumSize = new Point(16,16),
                MaximumSize = new Point(16,16),
                AutoLayout = Gum.AutoLayout.FloatLeft,
                OnLayout = (sender) => sender.Rect.X += 18,
                OnClick = (sender, args) =>
                {
                    DwarfGame.World.ChunkManager.ChunkData.SetMaxViewingLevel(
                        DwarfGame.World.ChunkManager.ChunkData.MaxViewingLevel - 1,
                        ChunkManager.SliceMode.Y);
                }
            });

            levelRow.AddChild(new Gum.Widget
            {
                Background = new Gum.TileReference("round-buttons", 3),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gum.AutoLayout.FloatLeft,
                OnClick = (sender, args) =>
                {
                    DwarfGame.World.ChunkManager.ChunkData.SetMaxViewingLevel(
                        DwarfGame.World.ChunkManager.ChunkData.MaxViewingLevel + 1,
                        ChunkManager.SliceMode.Y);
                }
            });            

            LevelLabel = levelRow.AddChild(new Gum.Widget
            {
                Rect = new Rectangle(48, 32, 128, 20),
                AutoLayout = Gum.AutoLayout.DockFill,
                Font = "outline-font",
                OnLayout = (sender) => sender.Rect.X += 36,
                TextVerticalAlign = Gum.VerticalAlign.Center,
                TextColor = new Vector4(1, 1, 1, 1)
            });            
            #endregion

            ResourcePanel = NewGui.RootItem.AddChild(new Gum.Widget
                {
                    Transparent = true,
                    Rect = new Rectangle(0, 104, 128, 128),
                    AutoLayout = Gum.AutoLayout.None,
                    OnLayout = sender => sender.Rect.Y = infoPanel.Rect.Bottom + 4
                });

            #region Setup time display
            TimeLabel = NewGui.RootItem.AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.FloatTop,
                    TextHorizontalAlign = Gum.HorizontalAlign.Center,
                    MinimumSize = new Point(128, 20),
                    Font = "outline-font",
                    TextColor = new Vector4(1,1,1,1)
                });
            #endregion

            #region Minimap

            // Little hack here - Normally this button his hidden by the minimap. Hide the minimap and it 
            // becomes visible! 
            // Todo: Doh, doesn't actually work.
            NewGui.RootItem.AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.FloatBottomLeft,
                    Background = new Gum.TileReference("round-buttons", 3),
                    MinimumSize = new Point(16,16),
                    MaximumSize = new Point(16,16),
                    OnClick = (sender, args) =>
                        {
                            MinimapFrame.Hidden = false;
                            MinimapFrame.Invalidate();
                        }
                });

            MinimapRenderer = new NewGui.MinimapRenderer(192, 192, World,
                TextureManager.GetTexture(ContentPaths.Terrain.terrain_colormap));

            MinimapFrame = NewGui.RootItem.AddChild(new NewGui.MinimapFrame
                {
                    AutoLayout = Gum.AutoLayout.FloatBottomLeft,
                    Renderer = MinimapRenderer
                }) as NewGui.MinimapFrame;
            #endregion

            #region Setup top right tray
            var topRightTray = NewGui.RootItem.AddChild(new NewGui.IconTray
                {
                    Corners = Gum.Scale9Corners.Left | Gum.Scale9Corners.Bottom,
                    AutoLayout = Gum.AutoLayout.FloatTopRight,
                    SizeToGrid = new Point(2,1),
                    ItemSource = new Gum.Widget[] 
                        { 
                            new NewGui.FramedIcon
                            {
                                Icon = new Gum.TileReference("tool-icons", 10),
                                OnClick = (sender, args) => StateManager.PushState(new EconomyState(Game, StateManager))
                        },
                        new NewGui.FramedIcon
                        {
                            Icon = new Gum.TileReference("tool-icons", 12),
                            OnClick = (sender, args) => { OpenPauseMenu(); }
                        }
                        }
                });
            #endregion

            #region Setup game speed controls
            GameSpeedControls = NewGui.RootItem.AddChild(new NewGui.GameSpeedControls
                {
                    AutoLayout = Gum.AutoLayout.None,
                    OnLayout = (sender) =>
                    {
                        // Want to position this directly above the bottom-right tray.
                        sender.Rect = new Rectangle(BottomRightTray.Rect.Right - sender.MinimumSize.X,
                            BottomRightTray.Rect.Top - sender.MinimumSize.Y, sender.MinimumSize.X, sender.MinimumSize.Y);
                    }
                }) as NewGui.GameSpeedControls;

            #endregion

            InputManager.KeyReleasedCallback += InputManager_KeyReleasedCallback;

            DwarfGame.World.OnAnnouncement = (title, message, clickAction) =>
                {
                    var announcer = NewGui.RootItem.AddChild(new NewGui.AnnouncementPopup
                    {
                        Text = title,
                        Message = message,
                        OnClick = (sender, args) => { if (clickAction != null) clickAction(); },
                        Rect = new Rectangle(
                            BottomRightTray.Rect.X,
                            GameSpeedControls.Rect.Y - 128,
                            BottomRightTray.Rect.Width,
                            128)
                    });

                    // Make the announcer stay behind other controls.
                    NewGui.RootItem.SendToBack(announcer);
                };

            InfoTray = NewGui.RootItem.AddChild(new NewGui.InfoTray
                {
                    Rect = new Rectangle(NewGui.VirtualScreen.X + 239,
                        NewGui.VirtualScreen.Bottom - 210, 256, 210),
                    Transparent = true
                }) as NewGui.InfoTray;
                        
            NewGui.RootItem.Layout();
        }

        private Gum.Widget CreateIcon(int Tile, GameMaster.ToolMode Mode)
        {
            return new NewGui.FramedIcon
            {
                Icon = new Gum.TileReference("tool-icons", Tile),
                OnClick = (sender, args) =>
                    {
                        Master.Tools[Mode].OnBegin();
                        if (Master.CurrentToolMode != Mode)
                            Master.CurrentTool.OnEnd();
                        Master.CurrentToolMode = Mode;
                    }
            };
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
                MinimapFrame.Hidden = true;
                MinimapFrame.Invalidate();
            }

            if (key == Keys.Escape)
            {
                if (Master.CurrentToolMode != GameMaster.ToolMode.SelectUnits)
                {
                    ToolbarItems[Master.CurrentToolMode].OnClick(null, null);
                }
                else if (PausePanel != null)
                {
                    PausePanel.Close();
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
                    foreach (var pair in ToolbarItems)
                    {
                        if (i == index)
                        {
                            List<CreatureAI> minions = Faction.FilterMinionsWithCapability(Master.SelectedMinions,
                                pair.Key);

                            if ((index == 0 || minions.Count > 0))
                            {
                                pair.Value.OnClick(null,null);
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
                if (Paused) GameSpeedControls.CurrentSpeed = 0;
                else GameSpeedControls.CurrentSpeed = GameSpeedControls.PlaySpeed;
            }
            else if (key == ControlSettings.Mappings.TimeForward)
            {
                GameSpeedControls.CurrentSpeed += 1;
            }
            else if (key == ControlSettings.Mappings.TimeBackward)
            {
                GameSpeedControls.CurrentSpeed -= 1;
            }
            else if (key == ControlSettings.Mappings.ToggleGUI)
            {
                // Todo: Reimplement.
                GUI.RootComponent.IsVisible = !GUI.RootComponent.IsVisible;
            }
        }

        private void MakeMenuItem(Gum.Widget Menu, string Name, string Tooltip, Action<Gum.Widget, Gum.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockTop,
                Border = "border-thin",
                Text = Name,
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                TextVerticalAlign = Gum.VerticalAlign.Center,
                TextSize = 2
            });
        }

        public void OpenPauseMenu()
        {
            if (PausePanel != null) return;
            Paused = true;

            PausePanel = new Gum.Widget
            {
                Rect = new Rectangle(NewGui.VirtualScreen.Center.X - 128,
                    NewGui.VirtualScreen.Center.Y - 100, 256, 200),
                Border = "border-fancy",
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                Text = "- Paused -",
                InteriorMargin = new Gum.Margin(12, 0, 0, 0),
                Padding = new Gum.Margin(2, 2, 2, 2),
                OnClose = (sender) => PausePanel = null
            };

            NewGui.ConstructWidget(PausePanel);

            MakeMenuItem(PausePanel, "Continue", "", (sender, args) =>
                {
                    PausePanel.Close();
                    Paused = false;
                    PausePanel = null;
                });

            MakeMenuItem(PausePanel, "Options", "", (sender, args) => StateManager.PushState(new OptionsState(Game, StateManager)));

            MakeMenuItem(PausePanel, "New Options", "", (sender, args) =>
                {
                    var state = new NewOptionsState(Game, StateManager)
                    {
                        OnClosed = () =>
                        {
                            OpenPauseMenu();
                        }
                    };

                    StateManager.PushState(state);
                });

            MakeMenuItem(PausePanel, "Save", "",
                (sender, args) =>
                {
                    NewGui.ShowPopup(new NewGui.Confirm
                    {
                        Text = "Warning: Saving is still an unstable feature. Are you sure you want to continue?",
                        OnClose = (s) =>
                        {
                            if ((s as NewGui.Confirm).DialogResult == DwarfCorp.NewGui.Confirm.Result.OKAY)
                                World.Save(
                                    String.Format("{0}_{1}", Overworld.Name, DwarfGame.World.GameID),
                                    (success, exception) =>
                                    {
                                        NewGui.ShowPopup(new NewGui.Popup
                                        {
                                            Text = success ? "File saved." : "Save failed - " + exception.Message,
                                            OnClose = (s2) => OpenPauseMenu()
                                        }, false);
                                    });
                        }
                    }, false);
                });
            
            MakeMenuItem(PausePanel, "Quit", "", (sender, args) => QuitOnNextUpdate = true);

            PausePanel.Layout();

            NewGui.ShowPopup(PausePanel, false);
        }
      
        public void Destroy()
        {
            InputManager.KeyReleasedCallback -= InputManager_KeyReleasedCallback;
            Input.Destroy();
        }

        public void QuitGame()
        {
            World.Quit();
            StateManager.ClearState();
            Destroy();

            // This line needs to stay in so the GC can properly collect all the items the PlayState keeps active.
            // If you want to remove this line you better be prepared to fully clean up the PlayState instance
            // using another method.
            //StateManager.States["PlayState"] = new PlayState(Game, StateManager);
            
            StateManager.PushState(new MainMenuState(Game, StateManager));
        }
    }
}   
