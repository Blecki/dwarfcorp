using System.IO;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    public class PlayState : GameState
    {
        private bool IsShuttingDown { get; set; }
        private bool QuitOnNextUpdate { get; set; }
        public bool ShouldReset { get; set; }
        private DateTime EnterTime;

        // Amount of time to wait when play begins, before accepting input,
        private float EnterInputDelaySeconds = 1.0f;

        public WorldManager World { get; set; }

        public GameMaster Master
        {
            get { return World.Master; }
            set { World.Master = value; }
        }

        public bool Paused
        {
            get { return World.Paused; }
            set { World.Paused = value; }
        }

        private Gui.Widget MoneyLabel;
        private Gui.Widget LevelLabel;
        private Gui.Widgets.FlatToolTray.RootTray BottomToolBar;
        private Gui.Widgets.FlatToolTray.Tray MainMenu;
        private Gui.Widget TimeLabel;
        private Gui.Widget PausePanel;
        private Gui.Widgets.MinimapFrame MinimapFrame;
        private Gui.Widgets.MinimapRenderer MinimapRenderer;
        private Gui.Widgets.GameSpeedControls GameSpeedControls;
        private Widget PausedWidget;
        private Gui.Widgets.InfoTray InfoTray;
        private Gui.Widgets.ToggleTray BrushTray;
        private Gui.Widgets.GodMenu GodMenu;
        private AnnouncementPopup Announcer;
        private FramedIcon EconomyIcon;
        private Timer AutoSaveTimer;
        private CollapsableFrame SelectedEmployeeInfo;

        private class ToolbarItem
        {
            public Gui.Widgets.FramedIcon Icon;
            public Func<bool> Available;

            public ToolbarItem(Gui.Widget Icon, Func<bool> Available)
            {
                System.Diagnostics.Debug.Assert(Icon is Gui.Widgets.FramedIcon);
                this.Icon = Icon as Gui.Widgets.FramedIcon;
                this.Available = Available;
            }
        }

        // These get enabled or disabled based on dwarf selection each frame.
        private List<ToolbarItem> ToolbarItems = new List<ToolbarItem>();

        private void AddToolbarIcon(Widget Icon, Func<bool> Available)
        {
            ToolbarItems.Add(new ToolbarItem(Icon, Available));
        }

        // These widgets get hilited when the associated tool is active.
        private Dictionary<GameMaster.ToolMode, Gui.Widgets.FramedIcon> ToolHiliteItems = new Dictionary<GameMaster.ToolMode, Gui.Widgets.FramedIcon>();

        private void AddToolSelectIcon(GameMaster.ToolMode Mode, Gui.Widget Icon)
        {
            if (!ToolHiliteItems.ContainsKey(Mode))
                ToolHiliteItems.Add(Mode, Icon as Gui.Widgets.FramedIcon);
        }

        private void ChangeTool(GameMaster.ToolMode Mode)
        {
            Master.ChangeTool(Mode);
            foreach (var icon in ToolHiliteItems)
                icon.Value.Hilite = icon.Key == Mode;
        }

        // Provides event-based keyboard and mouse input.
        public static InputManager Input;// = new InputManager();

        private Gui.Root GuiRoot;

        public Gui.Root GetGUI()
        {
            return GuiRoot;
        }

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="game">The program currently running</param>
        /// <param name="stateManager">The game state manager this state will belong to</param>
        /// <param name="world">The world manager</param>
        public PlayState(DwarfGame game, GameStateManager stateManager, WorldManager world) :
            base(game, "PlayState", stateManager)
        {
            World = world;
            IsShuttingDown = false;
            QuitOnNextUpdate = false;
            ShouldReset = true;
            Paused = false;
            RenderUnderneath = true;
            EnableScreensaver = false;
            IsInitialized = false;
        }

        private void World_OnLoseEvent()
        {
            //Paused = true;
            //StateManager.PushState("LoseState");a
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
                EnterTime = DateTime.Now;

                // Ensure game is not paused.
                Paused = false;
                DwarfTime.LastTime.Speed = 1.0f;

                // Setup new gui. Double rendering the mouse?
                GuiRoot = new Gui.Root(DwarfGame.GumSkin);
                GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
                World.Gui = GuiRoot;

                // Setup input event handlers. All of the actions should already be established - just 
                // need handlers.
                DwarfGame.GumInput.ClearAllHandlers();

                World.ShowInfo += (text) =>
                {
                    InfoTray.AddMessage(text);
                };

                World.ShowTooltip += (text) =>
                {
                    GuiRoot.ShowTooltip(GuiRoot.MousePosition, text);
                };

                World.SetMouse += (mouse) =>
                {
                    GuiRoot.MousePointer = mouse;
                };

                World.SetMouseOverlay += (mouse, frame) => GuiRoot.MouseOverlaySheet = new TileReference(mouse, frame);

                
                World.ShowToolPopup += text => GuiRoot.ShowTooltip(new Point(GuiRoot.MousePosition.X + 4, GuiRoot.MousePosition.Y - 16),
                    new Gui.Widgets.ToolPopup
                {
                    Text = text,
                });
                 

                World.gameState = this;
                World.OnLoseEvent += World_OnLoseEvent;
                CreateGUIComponents();
                InputManager.KeyReleasedCallback += TemporaryKeyPressHandler;
                IsInitialized = true;

                SoundManager.PlayMusic("main_theme_day");
                World.Time.Dawn += time =>
                {
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_daytime, 0.15f);
                    SoundManager.PlayMusic("main_theme_day");
                    DiseaseLibrary.SpreadRandomDiseases(World.PlayerFaction.Minions);
                };

                World.Time.NewNight += time =>
                {
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_nighttime, 0.15f);
                    SoundManager.PlayMusic("main_theme_night");
                };
            }
            World.Unpause();
            AutoSaveTimer = new Timer(GameSettings.Default.AutoSaveTimeMinutes * 60.0f, false, Timer.TimerMode.Real);
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
            if (!IsActiveState || IsShuttingDown)
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

            SoundManager.PlayAmbience("grassland_ambience_day");
            SoundManager.PlayAmbience("grassland_ambience_night");
            // Needs to run before old input so tools work
            // Update new input system.
            DwarfGame.GumInput.FireActions(GuiRoot, (@event, args) =>
            {
                // Let old input handle mouse interaction for now. Will eventually need to be replaced.

                // Mouse down but not handled by GUI? Collapse menu.
                if (@event == Gui.InputEvents.MouseClick) 
                {
                    //BottomToolBar.CollapseTrays();
                    GodMenu.CollapseTrays();
                }
            });

            World.Update(gameTime);
            Input.Update();


            #region Update time label
            TimeLabel.Text = String.Format("{0} {1}",
                World.Time.CurrentDate.ToShortDateString(),
                World.Time.CurrentDate.ToShortTimeString());
            TimeLabel.Invalidate();
            #endregion


            #region Update top left panel
            MoneyLabel.Text = Master.Faction.Economy.CurrentMoney.ToString();
            MoneyLabel.Invalidate();

            LevelLabel.Text = String.Format("{0}/{1}",
                World.ChunkManager.ChunkData.MaxViewingLevel,
                VoxelConstants.ChunkSizeY);
            LevelLabel.Invalidate();
            #endregion

            #region Update toolbar tray

            if (Master.SelectedMinions.Count == 0)
            {
                if (Master.CurrentToolMode != GameMaster.ToolMode.God)
                    Master.CurrentToolMode = GameMaster.ToolMode.SelectUnits;
            }

            foreach (var tool in ToolbarItems)
                tool.Icon.Enabled = tool.Available();

            #endregion

            #region Update Economy Indicator

            EconomyIcon.IndicatorValue = World.GoalManager.NewAvailableGoals + World.GoalManager.NewCompletedGoals;

            #endregion

            if (GameSpeedControls.CurrentSpeed != (int) DwarfTime.LastTime.Speed)
            {
                World.Tutorial("time");
            }

            GameSpeedControls.CurrentSpeed = (int)DwarfTime.LastTime.Speed;
           
            // Really just handles mouse pointer animation.
            GuiRoot.Update(gameTime.ToRealTime());

            AutoSaveTimer.Update(gameTime);

            if (GameSettings.Default.AutoSave && AutoSaveTimer.HasTriggered)
            {
                AutoSave();   
            }

#region select employee
           
            if (Master.SelectedMinions.Count == 1 && SelectedEmployeeInfo != null)
            {
                // Lol this is evil just trying to reduce the update rate for speed
                if (MathFunctions.RandEvent(0.1f))
                {
                    (SelectedEmployeeInfo.ExpandedContents as Gui.Widgets.EmployeeInfo).Employee = Master.SelectedMinions[0];
                    SelectedEmployeeInfo.CollapsedContents.Text = Master.SelectedMinions[0].Stats.FullName;
                    SelectedEmployeeInfo.Hidden = false;
                    SelectedEmployeeInfo.Invalidate();
                }
            }

            if (Master.SelectedMinions.Count != 1 && SelectedEmployeeInfo != null && !SelectedEmployeeInfo.Hidden)
            {
                SelectedEmployeeInfo.Hidden = true;
            }
#endregion
        }

        /// <summary>
        /// Called when a frame is to be drawn to the screen
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void Render(DwarfTime gameTime)
        {
            Game.Graphics.GraphicsDevice.SetRenderTarget(null);
            Game.Graphics.GraphicsDevice.Clear(Color.Black);
            EnableScreensaver = !World.ShowingWorld;

            if (World.ShowingWorld)
            {
                /*For regenerating the voxel icon image! Do not delete!*/
                /*
                Texture2D tex = VoxelLibrary.RenderIcons(Game.GraphicsDevice, World.DefaultShader, World.ChunkManager, 256, 256, 32);
                Game.GraphicsDevice.SetRenderTarget(null);
                tex.SaveAsPng(new FileStream("voxels.png", FileMode.Create),  256, 256);
                Game.Exit();
                 */


                if (!MinimapFrame.Hidden && !GuiRoot.RootItem.Hidden)
                    MinimapRenderer.PreRender(gameTime, DwarfGame.SpriteBatch);

                World.Render(gameTime);

                if (Game.StateManager.CurrentState == this)
                {
                    if (!MinimapFrame.Hidden && !GuiRoot.RootItem.Hidden)
                        MinimapRenderer.Render(new Rectangle(0, GuiRoot.RenderData.VirtualScreen.Bottom - 192, 192, 192), GuiRoot);
                    GuiRoot.Draw();
                }
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
            #region Setup company information section
            GuiRoot.RootItem.AddChild(new Gui.Widgets.CompanyLogo
            {
                Tag = "company info",
                Rect = new Rectangle(8, 8, 32, 32),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gui.AutoLayout.None,
                CompanyInformation = World.PlayerCompany.Information,
                Tooltip = "Company information"
            });

            GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = new Rectangle(48, 8, 256, 20),
                Text = World.PlayerCompany.Information.Name,
                AutoLayout = Gui.AutoLayout.None,
                Font = "outline-font",
                TextColor = new Vector4(1, 1, 1, 1)
            });

            var infoPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = new Rectangle(0, 40, 128, 102),
                AutoLayout = Gui.AutoLayout.None
            });

            var moneyRow = infoPanel.AddChild(new Gui.Widget
            {
                Tag = "money",
                MinimumSize = new Point(0, 34),
                AutoLayout = Gui.AutoLayout.DockTop
            });

            moneyRow.AddChild(new Gui.Widget
            {
                Background = new Gui.TileReference("resources", 40),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gui.AutoLayout.DockLeft
            });

            MoneyLabel = moneyRow.AddChild(new Gui.Widget
            {
                Rect = new Rectangle(48, 32, 128, 20),
                AutoLayout = Gui.AutoLayout.DockFill,
                Font = "outline-font",
                TextVerticalAlign = Gui.VerticalAlign.Center,
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = "Amount of money in our treasury"
            });

            var levelRow = infoPanel.AddChild(new Gui.Widget
            {
                Tag = "slice",
                MinimumSize = new Point(0, 34),
                AutoLayout = Gui.AutoLayout.DockTop
            });

            levelRow.AddChild(new Gui.Widget
            {
                Background = new Gui.TileReference("resources", 42),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gui.AutoLayout.DockLeft,
                Tooltip = "Current viewing level."
            });

            levelRow.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 7),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.FloatLeft,
                OnLayout = (sender) => sender.Rect.X += 18,
                OnClick = (sender, args) =>
                {
                    World.ChunkManager.ChunkData.SetMaxViewingLevel(
                        World.ChunkManager.ChunkData.MaxViewingLevel - 1,
                        ChunkManager.SliceMode.Y);
                },
                Tooltip = "Go up down one viewing level."
            });

            levelRow.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 3),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.FloatLeft,
                OnClick = (sender, args) =>
                {
                    World.ChunkManager.ChunkData.SetMaxViewingLevel(
                        World.ChunkManager.ChunkData.MaxViewingLevel + 1,
                        ChunkManager.SliceMode.Y);
                },
                Tooltip = "Go down up one viewing level."
            });

            LevelLabel = levelRow.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockFill,
                Font = "outline-font",
                OnLayout = (sender) => sender.Rect.X += 36,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = "Current viewing level."
            });
            #endregion

            GuiRoot.RootItem.AddChild(new Gui.Widgets.ResourcePanel
            {
                AutoLayout = AutoLayout.FloatTop,
                MinimumSize = new Point(256, 0),
                Master = Master,
                Transparent = true
            });

            #region Setup time display
            TimeLabel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.FloatBottomRight,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                MinimumSize = new Point(128, 20),
                Font = "font",
                TextColor = new Vector4(1, 1, 1, 1),
                OnLayout = (sender) =>
                {
                    sender.Rect.X -= 8;
                    sender.Rect.Y += 8;
                },
                Tooltip = "Current time/date."
            });
            #endregion

            #region Minimap

            var minimapRestoreButton = GuiRoot.RootItem.AddChild(new Gui.Widgets.ImageButton
            {
                AutoLayout = Gui.AutoLayout.FloatBottomLeft,
                Background = new Gui.TileReference("round-buttons", 3),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                Hidden = true,
                OnClick = (sender, args) =>
                {
                    sender.Hidden = true;
                    sender.Invalidate();
                    MinimapFrame.Hidden = false;
                    MinimapFrame.Invalidate();
                },
                Tooltip = "Restore minimap"
            });

            MinimapRenderer = new Gui.Widgets.MinimapRenderer(192, 192, World,
                TextureManager.GetTexture(ContentPaths.Terrain.terrain_colormap));

            MinimapFrame = GuiRoot.RootItem.AddChild(new Gui.Widgets.MinimapFrame
            {
                Tag = "minimap",
                AutoLayout = Gui.AutoLayout.FloatBottomLeft,
                Renderer = MinimapRenderer,
                RestoreButton = minimapRestoreButton
            }) as Gui.Widgets.MinimapFrame;

            #endregion

            #region Employee Info

            SelectedEmployeeInfo = GuiRoot.RootItem.AddChild(new CollapsableFrame
            {
                ExpandedPosition = new Rectangle(0, GuiRoot.RenderData.VirtualScreen.Height / 2 - 450 / 2, 400, 450),
                Hidden = true,

                ExpandedContents = new Gui.Widgets.EmployeeInfo
                {
                    Employee = null,
                    EnablePosession = true,
                    OnFireClicked = (sender) =>
                    {
                        GuiRoot.ShowDialog(GuiRoot.ConstructWidget(new Gui.Widgets.Confirm
                        {
                            OkayText = "Fire this dwarf!",
                            CancelText = "Keep this dwarf.",
                            Padding = new Margin(32, 10, 10, 10),
                            MinimumSize = new Point(512, 128),
                            OnClose = (confirm) =>
                            {
                                if ((confirm as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                                {
                                    SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
                                    var selectedEmployee = (sender as EmployeeInfo).Employee;
                                    selectedEmployee.GetRoot().Delete();

                                    Master.Faction.Minions.Remove(selectedEmployee);
                                    Master.Faction.SelectedMinions.Remove(selectedEmployee);
                                }
                            }
                        }));
                    }
                },

                CollapsedContents = new Widget
                {
                    Text = "EMPLOYEE NAME"
                },

                OnLayout = (sender) =>
                {
                    (sender as CollapsableFrame).ExpandedPosition = new Rectangle(0,
                        Math.Max(0, MinimapFrame.Rect.Y - 450),
                        400,
                        Math.Min(MinimapFrame.Rect.Y, 450));
                    (sender as CollapsableFrame).CollapsedPosition = new Rectangle(0,
                        MinimapFrame.Rect.Y - 40, 200, 40);
                }
            }) as CollapsableFrame;

            #endregion

            #region Setup top right tray

            EconomyIcon = new Gui.Widgets.FramedIcon
            {
                Tag = "economy",
                Icon = new Gui.TileReference("tool-icons", 10),
                OnClick = (sender, args) => StateManager.PushState(new NewEconomyState(Game, StateManager, World)),
                DrawIndicator = true,
                Tooltip = "Click to open the Economy screen"
            };

            var topRightTray = GuiRoot.RootItem.AddChild(new Gui.Widgets.IconTray
            {
                Corners = Gui.Scale9Corners.Left | Gui.Scale9Corners.Bottom,
                AutoLayout = Gui.AutoLayout.FloatTopRight,
                SizeToGrid = new Point(2, 1),
                ItemSource = new Gui.Widget[] 
                        {
                            EconomyIcon,
                                                                   
                            new Gui.Widgets.FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 12),
                                OnClick = (sender, args) => { OpenPauseMenu(); },
                                Tooltip = "Click to open the Settings screen."
                            }
                        },
            });
            #endregion

            #region Setup game speed controls
            GameSpeedControls = GuiRoot.RootItem.AddChild(new GameSpeedControls
            {
                Tag = "speed controls",
                AutoLayout = Gui.AutoLayout.FloatBottomRight,
                OnLayout = (sender) =>
                {
                    sender.Rect.X -= 8;
                    sender.Rect.Y -= 16;
                },
                OnSpeedChanged = (sender, speed) =>
                {
                    if ((int) DwarfTime.LastTime.Speed != speed)
                    {
                        World.Tutorial("time");
                    }
                    DwarfTime.LastTime.Speed = (float)speed;
                    Paused = speed == 0;
                    PausedWidget.Hidden = !Paused;
                    PausedWidget.Tooltip = "(push " + ControlSettings.Mappings.Pause.ToString() + " to unpause)";
                    PausedWidget.Invalidate();
                },
                Tooltip = "Game speed controls."
            }) as GameSpeedControls;

            PausedWidget = GuiRoot.RootItem.AddChild(new Widget()
            {
                Text = "\n\nPaused",
                AutoLayout = Gui.AutoLayout.FloatCenter,
                Tooltip = "(push " + ControlSettings.Mappings.Pause.ToString() + " to unpause)",
                Font = "outline-font",
                TextColor = Color.White.ToVector4(),
                Hidden = true,
            });

            #endregion

            #region Announcer and info tray

            Announcer = GuiRoot.RootItem.AddChild(new AnnouncementPopup
            {
                OnLayout = (sender) =>
                {
                    sender.Rect = new Rectangle(GameSpeedControls.Rect.X - 128,
                        GameSpeedControls.Rect.Y - 128, GameSpeedControls.Rect.Width + 128, 128);
                }
            }) as AnnouncementPopup;

            World.OnAnnouncement = (message, clickAction) =>
            {
                Announcer.QueueAnnouncement(message, clickAction);
            };

            InfoTray = GuiRoot.RootItem.AddChild(new Gui.Widgets.InfoTray
            {
                OnLayout = (sender) =>
                {
                    sender.Rect = new Rectangle(MinimapFrame.Rect.Right,
                            MinimapFrame.Rect.Top, 256, MinimapFrame.Rect.Height);
                },
                Transparent = true
            }) as Gui.Widgets.InfoTray;

            #endregion

            #region Setup brush

            BrushTray = GuiRoot.RootItem.AddChild(new Gui.Widgets.ToggleTray
            {
                Tag = "brushes",
                AutoLayout = AutoLayout.FloatRight,
                Rect = new Rectangle(256, 0, 32, 128),
                SizeToGrid = new Point(1, 3),
                Border = null,
                ItemSource = new Gui.Widget[]
               
                        { 
                            new Gui.Widgets.FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 29),
                                DrawFrame = false,
                                Tooltip = "Block brush",
                                OnClick = (widget, args) =>
                                {
                                    Master.VoxSelector.Brush = VoxelBrush.Box;
                                    World.SetMouseOverlay("tool-icons", 29);
                                    World.Tutorial("brush");
                                }
                            },
                            new Gui.Widgets.FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 30),
                                DrawFrame = false,
                                Tooltip = "Shell brush",
                                OnClick = (widget, args) =>
                                {
                                    Master.VoxSelector.Brush = VoxelBrush.Shell;
                                    World.SetMouseOverlay("tool-icons", 30);
                                    World.Tutorial("brush");
                                }
                            },
                            new Gui.Widgets.FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 31),
                                DrawFrame = false,
                                Tooltip = "Stairs brush",
                                OnClick = (widget, args) =>
                                {
                                    Master.VoxSelector.Brush = VoxelBrush.Stairs;
                                    World.SetMouseOverlay("tool-icons", 31);
                                    World.Tutorial("brush");
                                }
                            }
                        }
            }) as Gui.Widgets.ToggleTray;


            #endregion

            #region Setup tool tray

            #region icon_SelectTool

            var icon_SelectTool = new FlatToolTray.Icon
            {
                Tag = "select",
                Icon = new Gui.TileReference("tool-icons", 5),
                OnClick = (sender, args) => ChangeTool(GameMaster.ToolMode.SelectUnits),
                Tooltip = "Select dwarves",
                Behavior = FlatToolTray.IconBehavior.LeafIcon,
                OnConstruct = (sender) =>
                {
                    // This could just be done after declaring icon_SelectTool, but is here for
                    // consistency with other icons where this is not possible.
                    AddToolbarIcon(sender, () => true);
                    AddToolSelectIcon(GameMaster.ToolMode.SelectUnits, sender);
                }
            };

            #endregion

            #region icon_BuildRoom

            var icon_menu_RoomTypes_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_RoomTypes = new FlatToolTray.Tray
            {
                ItemSource = (new Widget[] {
                    icon_menu_RoomTypes_Return
                }).Concat(RoomLibrary.GetRoomTypes().Select(RoomLibrary.GetData)
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.NewIcon,
                        ExpandChildWhenDisabled = true,
                        PopupChild = new BuildRoomInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 256, 164),
                            Master = Master
                        },
                        OnClick = (sender, args) =>
                        {
                            Master.Faction.RoomBuilder.CurrentRoomData = data;
                            Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                            Master.Faction.WallBuilder.CurrentVoxelType = null;
                            Master.Faction.CraftBuilder.IsEnabled = false;
                            ChangeTool(GameMaster.ToolMode.Build);
                            World.ShowToolPopup("Click and drag to build " + data.Name);
                            World.Tutorial("build rooms");
                        },
                        Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                        OnConstruct = (sender) =>
                        {
                            AddToolbarIcon(sender, () =>
                                ((sender as FlatToolTray.Icon).PopupChild as BuildRoomInfo).CanBuild());
                        }
                    }))
            };

            var icon_moveObjects = new FlatToolTray.Icon()
            {
                Text = "",
                Tooltip = "Move/Destroy objects",
                Icon = new TileReference("mouse", 9),
                OnClick = (sender, args) =>
                {
                    World.ShowToolPopup("Left click objects to move them.\nRight click to destroy them.");
                    Master.ChangeTool(GameMaster.ToolMode.MoveObjects);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            var icon_BuildRoom = new FlatToolTray.Icon
            {
                TextColor = Vector4.One,
                Text = "Room",
                Tooltip = "Build rooms",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                KeepChildVisible = true,
                ReplacementMenu = menu_RoomTypes,
                Tag = "build room",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_BuildWall

            var icon_menu_WallTypes_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_WallTypes = new FlatToolTray.Tray
            {
                Tag = "build wall",
                ItemSource = new List<Widget>(),
                OnShown = (widget) =>
                {
                    // Dynamically rebuild the tray
                    widget.Clear();
                    (widget as FlatToolTray.Tray).ItemSource =
                        (new Widget[] { icon_menu_WallTypes_Return }).Concat(
                        VoxelLibrary.GetTypes()
                        .Where(voxel => voxel.IsBuildable && World.PlayerFaction.HasResources(voxel.ResourceToRelease))
                        .Select(data => new FlatToolTray.Icon
                        {
                            Tooltip = "Build " + data.Name,
                            Icon = new Gui.TileReference("voxels", data.ID),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            Text = Master.Faction.ListResources()[data.ResourceToRelease].NumResources.ToString(),
                            TextColor = Color.White.ToVector4(),
                            PopupChild = new BuildWallInfo
                            {
                                Data = data,
                                Rect = new Rectangle(0, 0, 256, 128),
                                Master = Master
                            },
                            OnClick = (sender, args) =>
                            {
                                Master.Faction.RoomBuilder.CurrentRoomData = null;
                                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                                Master.Faction.WallBuilder.CurrentVoxelType = data;
                                Master.Faction.CraftBuilder.IsEnabled = false;
                                ChangeTool(GameMaster.ToolMode.Build);
                                World.ShowToolPopup("Click and drag to build " + data.Name + " wall.");
                                World.Tutorial("build blocks");
                            },
                            Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                            Hidden = false
                        }));
                    widget.Construct();
                    widget.Hidden = false;
                    widget.Layout();
                }
            };

            var icon_BuildWall = new FlatToolTray.Icon
            {
                Icon = null,
                Font = "font",
                KeepChildVisible = true,
                ExpandChildWhenDisabled = true,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Tooltip = "Place blocks",
                Text = "Block",
                TextColor = Color.White.ToVector4(),
                ReplacementMenu = menu_WallTypes,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_BuildCraft

            var icon_menu_CraftTypes_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_CraftTypes = new FlatToolTray.Tray
            {
                Tag = "craft item",
                ItemSource = (new Widget[]{ icon_menu_CraftTypes_Return }).Concat(
                    CraftLibrary.CraftItems.Values.Where(item => item.Type == CraftItem.CraftType.Object)
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.Icon,
                        Tooltip = "Craft " + data.Name,
                        KeepChildVisible = true, // So the player can interact with the popup.
                        ExpandChildWhenDisabled = true,
                        Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                        PopupChild = new BuildCraftInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 256, 150),
                            Master = Master,
                            World = World,
                            BuildAction = (sender, args) =>
                            {
                                var buildInfo = sender.Parent as Gui.Widgets.BuildCraftInfo;
                                if (buildInfo == null)
                                {
                                    return;
                                }

                                data.SelectedResources = buildInfo.GetSelectedResources();
                                data.NumRepeats = buildInfo.GetNumRepeats();
                                Master.Faction.RoomBuilder.CurrentRoomData = null;
                                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                                Master.Faction.WallBuilder.CurrentVoxelType = null;
                                Master.Faction.CraftBuilder.IsEnabled = true;
                                Master.Faction.CraftBuilder.CurrentCraftType = data;
                                if (Master.Faction.CraftBuilder.CurrentCraftBody != null)
                                {
                                    Master.Faction.CraftBuilder.CurrentCraftBody.Delete();
                                    Master.Faction.CraftBuilder.CurrentCraftBody = null;
                                }
                                ChangeTool(GameMaster.ToolMode.Build);
                                World.ShowToolPopup("Click and drag to " + data.Verb + " " + data.Name);
                            },
                        },
                        OnConstruct = (sender) =>
                        {
                            AddToolbarIcon(sender, () =>
                                ((sender as FlatToolTray.Icon).PopupChild as BuildCraftInfo).CanBuild());
                        }
                    }))
            };

            var icon_BuildCraft = new FlatToolTray.Icon
            {
                Icon = null,
                Text = "Object",
                TextColor = Vector4.One,
                Tooltip = "Craft objects",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                KeepChildVisible = true,
                MinimumSize = new Point(128, 32),
                ReplacementMenu = menu_CraftTypes,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_BuildResource

            var icon_menu_ResourceTypes_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_ResourceTypes = new FlatToolTray.Tray
            {
                Tag = "craft resource",
                Tooltip = "Craft resource",
                ItemSource = (new Widget[] { icon_menu_ResourceTypes_Return }).Concat(
                    CraftLibrary.CraftItems.Values.Where(item => item.Type == CraftItem.CraftType.Resource
                    && ResourceLibrary.Resources.ContainsKey(item.ResourceCreated) &&
                    !ResourceLibrary.Resources[item.ResourceCreated].Tags.Contains(Resource.ResourceTags.Edible))
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.Icon,
                        Tooltip = "Craft " + data.Name,
                        KeepChildVisible = true, // So the player can interact with the popup.
                        ExpandChildWhenDisabled = true,
                        Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                        PopupChild = new BuildCraftInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 256, 150),
                            Master = Master,
                            World = World,
                            BuildAction = (sender, args) =>
                            {
                                var buildInfo = (sender.Parent as Gui.Widgets.BuildCraftInfo);
                                if (buildInfo == null)
                                    return;

                                data.SelectedResources = buildInfo.GetSelectedResources();
                                data.NumRepeats = buildInfo.GetNumRepeats();
                                var assignments = new List<Task> { new CraftResourceTask(data) };
                                var minions = Faction.FilterMinionsWithCapability(Master.SelectedMinions,
                                    GameMaster.ToolMode.Craft);
                                if (minions.Count > 0)
                                {
                                    TaskManager.AssignTasks(assignments, minions);
                                    World.ShowToolPopup(data.CurrentVerb + " " + data.NumRepeats + " " + data.Name);
                                }
                                World.Tutorial("build crafts");
                            },
                        },
                        OnConstruct = (sender) =>
                        {
                            AddToolbarIcon(sender, () =>
                                ((sender as FlatToolTray.Icon).PopupChild as BuildCraftInfo).CanBuild());
                        }
                    }))
            };

            var icon_BuildResource = new FlatToolTray.Icon
            {
                Text = "Res.",
                TextColor = Vector4.One,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                KeepChildVisible = true,
                ReplacementMenu = menu_ResourceTypes,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_BuildTool

            var icon_menu_BuildTools_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_BuildTools = new FlatToolTray.Tray
            {
                ItemSource = new FlatToolTray.Icon[]
                    {
                        icon_menu_BuildTools_Return,
                        icon_moveObjects,
                        icon_BuildRoom,
                        icon_BuildWall,
                        icon_BuildCraft,
                        icon_BuildResource
                    }
            };

            icon_menu_CraftTypes_Return.ReplacementMenu = menu_BuildTools;
            icon_menu_ResourceTypes_Return.ReplacementMenu = menu_BuildTools;
            icon_menu_RoomTypes_Return.ReplacementMenu = menu_BuildTools;
            icon_menu_WallTypes_Return.ReplacementMenu = menu_BuildTools;

            var icon_BuildTool = new FlatToolTray.Icon
            {
                Tag = "build",
                Icon = new TileReference("tool-icons", 2),
                KeepChildVisible = true,
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () => Master.Faction.SelectedMinions.Any(minion =>
                        minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Build)));
                    AddToolSelectIcon(GameMaster.ToolMode.Build, sender);
                },
                Tooltip = "Build",
                ReplacementMenu = menu_BuildTools,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_CookTool

            var icon_menu_Edibles_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_Edibles = new FlatToolTray.Tray
            {
                ItemSource = (new Widget[] { icon_menu_Edibles_Return }).Concat(
                    CraftLibrary.CraftItems.Values.Where(item => item.Type == CraftItem.CraftType.Resource
                    && ResourceLibrary.Resources.ContainsKey(item.ResourceCreated)
                    && ResourceLibrary.Resources[item.ResourceCreated].Tags.Contains(Resource.ResourceTags.Edible))
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.Icon,
                        KeepChildVisible = true, // So the player can interact with the popup.
                        Tooltip = data.Verb + " " + data.Name,
                        Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                        PopupChild = new BuildCraftInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 256, 128),
                            Master = Master,
                            World = World,
                            BuildAction = (sender, args) =>
                            {
                                var buildInfo = sender.Parent as Gui.Widgets.BuildCraftInfo;
                                data.SelectedResources = buildInfo.GetSelectedResources();
                                data.NumRepeats = buildInfo.GetNumRepeats();
                                List<Task> assignments = new List<Task> { new CraftResourceTask(data) };
                                var minions = Faction.FilterMinionsWithCapability(Master.SelectedMinions,
                                    GameMaster.ToolMode.Cook);
                                if (minions.Count > 0)
                                {
                                    TaskManager.AssignTasks(assignments, minions);
                                    World.ShowToolPopup(data.CurrentVerb + " one " + data.Name);
                                }
                                World.Tutorial("cook");
                            },
                        },
                        OnConstruct = (sender) =>
                        {
                            AddToolbarIcon(sender, () => ((sender as FlatToolTray.Icon).PopupChild as BuildCraftInfo).CanBuild());
                        }
                    }))
            };

            var icon_CookTool = new FlatToolTray.Icon
            {
                Tag = "cook",
                Icon = new TileReference("tool-icons", 27),
                KeepChildVisible = true,
                Tooltip = "Cook food",
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.SelectedMinions.Any(minion =>
                        minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Cook)));
                    AddToolSelectIcon(GameMaster.ToolMode.Cook, sender);
                },
                ReplacementMenu = menu_Edibles,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_DigTool

            var icon_DigTool = new FlatToolTray.Icon
            {
                Tag = "dig",
                Icon = new TileReference("tool-icons", 0),
                Tooltip = "Dig",
                OnClick = (sender, args) => ChangeTool(GameMaster.ToolMode.Dig),
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.SelectedMinions.Any(minion =>
                        minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Dig)));
                    AddToolSelectIcon(GameMaster.ToolMode.Dig, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_GatherTool

            var icon_GatherTool = new FlatToolTray.Icon
            {
                Tag = "gather",
                Icon = new TileReference("tool-icons", 6),
                Tooltip = "Gather",
                OnClick = (sender, args) => { ChangeTool(GameMaster.ToolMode.Gather); World.Tutorial("gather"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.SelectedMinions.Any(minion =>
                        minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Gather)));
                    AddToolSelectIcon(GameMaster.ToolMode.Gather, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_ChopTool

            var icon_ChopTool = new FlatToolTray.Icon
            {
                Tag = "chop",
                Icon = new TileReference("tool-icons", 1),
                Tooltip = "Chop trees",
                OnClick = (sender, args) => { ChangeTool(GameMaster.ToolMode.Chop); World.Tutorial("chop"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.SelectedMinions.Any(minion =>
                        minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Chop)));
                    AddToolSelectIcon(GameMaster.ToolMode.Chop, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_GuardTool

            var icon_GuardTool = new FlatToolTray.Icon
            {
                Tag = "guard",
                Icon = new TileReference("tool-icons", 4),
                Tooltip = "Guard",
                OnClick = (sender, args) => { ChangeTool(GameMaster.ToolMode.Guard); World.Tutorial("guard"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.SelectedMinions.Any(minion =>
                        minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Guard)));
                    AddToolSelectIcon(GameMaster.ToolMode.Guard, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_AttackTool

            var icon_AttackTool = new FlatToolTray.Icon
            {
                Tag = "attack",
                Icon = new TileReference("tool-icons", 3),
                Tooltip = "Attack",
                OnClick = (sender, args) => { ChangeTool(GameMaster.ToolMode.Attack); World.Tutorial("attack"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.SelectedMinions.Any(minion =>
                        minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Attack)));
                    AddToolSelectIcon(GameMaster.ToolMode.Attack, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_FarmTool

            var icon_menu_Farm_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #region icon_Till
            var icon_Till = new FlatToolTray.Icon
            {
                Tag = "till",
                Text = "Till",
                Tooltip = "Till soil",
                TextColor = new Vector4(1, 1, 1, 1),
                KeepChildVisible = true,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                OnClick = (sender, args) =>
                {
                    World.ShowToolPopup("Click and drag to till soil.");
                    ChangeTool(GameMaster.ToolMode.Farm);
                    ((FarmTool)(Master.Tools[GameMaster.ToolMode.Farm])).Mode = FarmTool.FarmMode.Tilling;
                    World.Tutorial("till");
                },
                PopupChild = new Widget()
                {
                    Border = "border-fancy",
                    Text = "Till Soil.\n Click and drag to till soil for planting.",
                    Rect = new Rectangle(0, 0, 256, 128),
                    TextColor = Color.Black.ToVector4(),
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };
            #endregion

            #region icon_Plant
            #region menu_Plant
            var icon_menu_Plant_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_Plant = new FlatToolTray.Tray
            {
                ItemSource = new List<Widget>(),
                OnShown = (widget) =>
                {
                    widget.Clear();

                    (widget as FlatToolTray.Tray).ItemSource = 
                        (new Widget[] { icon_menu_Plant_Return }).Concat(
                         Master.Faction.ListResourcesWithTag(Resource.ResourceTags.Plantable)
                        .Select(resource => new FlatToolTray.Icon
                           {
                               Icon = resource.ResourceType.GetResource().GuiLayers[0],
                               Tooltip = "Plant " + resource.ResourceType,
                               Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                               OnClick = (sender, args) =>
                               {
                                   World.ShowToolPopup("Click and drag to plant " + resource.ResourceType + ".");
                                   ChangeTool(GameMaster.ToolMode.Farm);
                                   var farmTool = Master.Tools[GameMaster.ToolMode.Farm] as FarmTool;
                                   farmTool.Mode = FarmTool.FarmMode.Planting;
                                   farmTool.PlantType = resource.ResourceType;
                                   farmTool.RequiredResources = new List<ResourceAmount>()
                                       {
                                          new ResourceAmount(resource.ResourceType)
                                       };
                                   World.Tutorial("plant");
                               },
                               PopupChild = new PlantInfo()
                               {
                                   Type = resource.ResourceType,
                                   Rect = new Rectangle(0, 0, 256, 128),
                                   Master = Master,
                                   TextColor = Color.Black.ToVector4()
                               },
                           }
                       ));
                    widget.Construct();
                    widget.Hidden = false;
                    widget.Layout();
                }
            };
            #endregion

            var icon_Plant = new FlatToolTray.Icon
            {
                Tag = "plant",
                Text = "Plant",
                Tooltip = "Plant",
                TextColor = new Vector4(1, 1, 1, 1),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                KeepChildVisible = true,
                ReplacementMenu = menu_Plant,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };
            #endregion

            #region icon_Harvest
            var icon_Harvest = new FlatToolTray.Icon
            {
                Text = "Harv.",
                Tag = "harvest",
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = "Harvest",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                KeepChildVisible = true,
                PopupChild = new Widget()
                {
                    Border = "border-fancy",
                    Text = "Harvest Plants.\n Click and drag to harvest plants.",
                    Rect = new Rectangle(0, 0, 256, 128),
                    TextColor = Color.Black.ToVector4(),
                },
                OnClick = (sender, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.Farm);
                    (Master.Tools[GameMaster.ToolMode.Farm] as FarmTool).Mode = FarmTool.FarmMode.Harvesting;
                    World.Tutorial("harvest");
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };
            #endregion

            #region icon_Wrangle
            var icon_Wrangle = new FlatToolTray.Icon
            {
                Tag = "wrangle",
                Text = "Wrng.",
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = "Wrangle Animals",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                KeepChildVisible = true,
                PopupChild = new Widget()
                {
                    Border = "border-fancy",
                    Text = "Wrangle Animals.\n Click and drag to wrangle animals.",
                    Rect = new Rectangle(0, 0, 256, 128),
                    TextColor = Color.Black.ToVector4()
                },
                OnClick = (sender, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.Farm);
                    (Master.Tools[GameMaster.ToolMode.Farm] as FarmTool).Mode = FarmTool.FarmMode.WranglingAnimals;
                    World.Tutorial("wrangle");
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };
            #endregion

            var menu_Farm = new FlatToolTray.Tray
            {
                ItemSource = new FlatToolTray.Icon[]
                    {
                        icon_menu_Farm_Return,
                        icon_Till,
                        icon_Plant,
                        icon_Harvest,
                        icon_Wrangle
                    }
            };

            icon_menu_Plant_Return.ReplacementMenu = menu_Farm;

            var icon_FarmTool = new FlatToolTray.Icon
            {
                Icon = new Gui.TileReference("tool-icons", 13),
                Tooltip = "Farm",

                KeepChildVisible = true,
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.SelectedMinions.Any(minion =>
                        minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Farm)));
                    AddToolSelectIcon(GameMaster.ToolMode.Farm, sender);
                },
                ReplacementMenu = menu_Farm,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_MagicTool

            #region icon_Cast
            var icon_menu_CastSpells_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_CastSpells = new FlatToolTray.Tray()
            {
                OnShown = (widget) =>
                {
                    widget.Clear();
                    (widget as FlatToolTray.Tray).ItemSource =
                        (new Widget[] { icon_menu_CastSpells_Return }).Concat(
                        Master.Spells.Enumerate()
                            .Where(spell => spell.IsResearched)
                            .Select(spell => new FlatToolTray.Icon
                            {
                                Icon = new TileReference("tool-icons", spell.Spell.TileRef),
                                Tooltip = "Cast " + spell.Spell.Name,
                                PopupChild = new SpellInfo()
                                {
                                    Spell = spell,
                                    Rect = new Rectangle(0, 0, 256, 128),
                                    Master = Master
                                },
                                OnClick = (widget2, args2) =>
                                {
                                    ChangeTool(GameMaster.ToolMode.Magic);
                                    ((MagicTool)Master.Tools[GameMaster.ToolMode.Magic])
                                        .CurrentSpell =
                                        spell.Spell;
                                    World.Tutorial("cast spells");
                                },
                                Behavior = FlatToolTray.IconBehavior.ShowHoverPopup
                            }));
                    widget.Construct();
                    widget.Hidden = false;
                    widget.Layout();
                }
            };

            var icon_Cast = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 14),
                Tooltip = "Cast",
                KeepChildVisible = true,
                ReplacementMenu = menu_CastSpells,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };
            #endregion

            #region icon_Research

            var icon_menu_ResearchSpells_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_ResearchSpells = new FlatToolTray.Tray()
            {
                OnShown = (widget) =>
                {
                    widget.Clear();
                    (widget as FlatToolTray.Tray).ItemSource =
                    (new Widget[] { icon_menu_ResearchSpells_Return }).Concat(
                        Master.Spells.EnumerateSubtrees
                        (spell => !spell.IsResearched,
                            spell =>
                                spell.IsResearched &&
                                spell.Children.Any(child => !child.IsResearched))
                        .Select(spell =>
                            new FlatToolTray.Icon
                            {
                                Icon = new TileReference("tool-icons", spell.Spell.TileRef),
                                Tooltip = "Research " + spell.Spell.Name,
                                PopupChild = new SpellInfo()
                                {
                                    Spell = spell,
                                    Rect = new Rectangle(0, 0, 256, 128),
                                    Master = Master
                                },
                                OnClick = (button, args2) =>
                                {
                                    ChangeTool(GameMaster.ToolMode.Magic);
                                    ((MagicTool)Master.Tools[GameMaster.ToolMode.Magic])
                                        .Research(spell);
                                    World.Tutorial("research spells");
                                },
                                Behavior = FlatToolTray.IconBehavior.ShowHoverPopup
                            }));
                    widget.Construct();
                    widget.Hidden = false;
                    widget.Layout();
                }
            };

            var icon_Research = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 14),
                Tooltip = "Research",
                KeepChildVisible = true,
                ReplacementMenu = menu_ResearchSpells,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };
            #endregion

            var icon_menu_Magic_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 32),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_Magic = new FlatToolTray.Tray
            {
                ItemSource = new FlatToolTray.Icon[]
                {
                    icon_menu_Magic_Return,
                    icon_Cast,
                    icon_Research
                }
            };

            icon_menu_CastSpells_Return.ReplacementMenu = menu_Magic;
            icon_menu_ResearchSpells_Return.ReplacementMenu = menu_Magic;

            var icon_MagicTool = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 14),
                Tooltip = "Magic",
                //OnClick = (sender, args) => ChangeTool(GameMaster.ToolMode.Magic),
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                        Master.Faction.SelectedMinions.Any(minion =>
                            minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Magic)));
                    AddToolSelectIcon(GameMaster.ToolMode.Magic, sender);
                },
                KeepChildVisible = true,
                ReplacementMenu = menu_Magic,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            MainMenu = new FlatToolTray.Tray
            {
                ItemSource = new Gui.Widget[]
                {
                    icon_SelectTool,
                    icon_BuildTool,
                    icon_CookTool,
                    icon_DigTool,
                    icon_GatherTool,
                    icon_ChopTool,
                    icon_GuardTool,
                    icon_AttackTool,
                    icon_FarmTool,
                    icon_MagicTool
                },
                OnShown = (sender) => ChangeTool(GameMaster.ToolMode.SelectUnits),
                Tag = "tools"
            };

            icon_menu_BuildTools_Return.ReplacementMenu = MainMenu;
            icon_menu_Edibles_Return.ReplacementMenu = MainMenu;
            icon_menu_Farm_Return.ReplacementMenu = MainMenu;
            icon_menu_Magic_Return.ReplacementMenu = MainMenu;

            BottomToolBar = GuiRoot.RootItem.AddChild(new FlatToolTray.RootTray
            {
                ItemSource = new Widget[]
                {
                    menu_BuildTools,
                    menu_CastSpells,
                    menu_CraftTypes,
                    menu_Edibles,
                    menu_Farm,
                    menu_Magic,
                    MainMenu,
                    menu_Plant,
                    menu_ResearchSpells,
                    menu_ResourceTypes,
                    menu_RoomTypes,
                    menu_WallTypes
                },
                OnLayout = (sender) =>
                {
                    sender.Rect = sender.ComputeBoundingChildRect();
                    sender.Rect.X = GuiRoot.RenderData.VirtualScreen.Center.X - 128; 
                    sender.Rect.Y = GuiRoot.RenderData.VirtualScreen.Bottom - MainMenu.MinimumSize.Y;
                },
            }) as FlatToolTray.RootTray;

            BottomToolBar.SwitchTray(MainMenu);
            ChangeTool(GameMaster.ToolMode.SelectUnits);

            #endregion

            #region GOD MODE

            GodMenu = GuiRoot.RootItem.AddChild(new Gui.Widgets.GodMenu
            {
                Master = Master,
                AutoLayout = AutoLayout.FloatLeft
            }) as Gui.Widgets.GodMenu;

            GodMenu.Hidden = true;

            #endregion

            GuiRoot.RootItem.Layout();
        }

        /// <summary>
        /// Called when the user releases a key
        /// </summary>
        /// <param name="key">The keyboard key released</param>
        private void TemporaryKeyPressHandler(Keys key)
        {
            if ((DateTime.Now - EnterTime).TotalSeconds >= EnterInputDelaySeconds)
            {
                InputManager.KeyReleasedCallback -= TemporaryKeyPressHandler;
                InputManager.KeyReleasedCallback += HandleKeyPress;
                HandleKeyPress(key);
            }
        }

        private void HandleKeyPress(Keys key)
        {
            // Special case: number keys reserved for changing tool mode
            if (InputManager.IsNumKey(key))
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
                    World.Tutorial("dwarf selected");
                }

                if (index == 0 || Master.SelectedMinions.Count > 0)
                {
                    (BottomToolBar.Children.First(w => w.Hidden == false) as FlatToolTray.Tray)
                        .Hotkey(index);
                }
            }
            else if (key == Keys.Escape)
            {
                if (MainMenu.Hidden)
                {
                    (BottomToolBar.Children.First(w => w.Hidden == false) as FlatToolTray.Tray).Hotkey(0);
                }
                else if (Master.CurrentToolMode != GameMaster.ToolMode.SelectUnits)
                {
                    Master.ChangeTool(GameMaster.ToolMode.SelectUnits);
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
                GuiRoot.RootItem.Hidden = !GuiRoot.RootItem.Hidden;
                GuiRoot.RootItem.Invalidate();
            }
            else if (key == ControlSettings.Mappings.Map)
            {
                World.DrawMap = !World.DrawMap;
                MinimapFrame.Hidden = true;
                MinimapFrame.Invalidate();
            }
            else if (key == ControlSettings.Mappings.GodMode)
            {
                GodMenu.Hidden = !GodMenu.Hidden;
                GodMenu.Invalidate();
            }
        }

        private void MakeMenuItem(Gui.Widget Menu, string Name, string Tooltip, Action<Gui.Widget, Gui.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                Border = "border-thin",
                Font = "font-hires",
                Text = Name,
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                HoverTextColor = Color.DarkRed.ToVector4(),
                ChangeColorOnHover = true
            });
        }

        public void OpenPauseMenu()
        {
            if (PausePanel != null) return;
            GameSpeedControls.Pause();

            PausePanel = new Gui.Widget
            {
                Rect = new Rectangle(GuiRoot.RenderData.VirtualScreen.Center.X - 128,
                    GuiRoot.RenderData.VirtualScreen.Center.Y - 100, 256, 200),
                Border = "border-fancy",
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                Text = "- Paused -",
                InteriorMargin = new Gui.Margin(12, 0, 0, 0),
                Padding = new Gui.Margin(2, 2, 2, 2),
                OnClose = (sender) =>
                {
                    PausePanel = null;
                },
                Font = "font-hires"
            };

            GuiRoot.ConstructWidget(PausePanel);

            MakeMenuItem(PausePanel, "Continue", "", (sender, args) =>
            {
                PausePanel.Close();
                GameSpeedControls.Resume();
                PausePanel = null;
            });

            MakeMenuItem(PausePanel, "Options", "", (sender, args) =>
            {
                var state = new OptionsState(Game, StateManager)
                {
                    OnClosed = () =>
                    {
                        PausePanel = null;
                        GuiRoot.RenderData.CalculateScreenSize();
                        GuiRoot.ResetGui();
                        CreateGUIComponents();
                        OpenPauseMenu();
                    },
                    World = World
                };

                StateManager.PushState(state);
            });

            MakeMenuItem(PausePanel, "Save", "",
                (sender, args) =>
                {
                    GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm
                    {
                        Text = "Warning: Saving is still an unstable feature. Are you sure you want to continue?",
                        OnClose = (s) =>
                        {
                            if ((s as Gui.Widgets.Confirm).DialogResult == DwarfCorp.Gui.Widgets.Confirm.Result.OKAY)
                                World.Save(
                                    String.Format("{0}_{1}", Overworld.Name, World.GameID),
                                    (success, exception) =>
                                    {
                                        GuiRoot.ShowModalPopup(new Gui.Widgets.Popup
                                        {
                                            Text = success ? "File saved." : "Save failed - " + exception.Message,
                                            OnClose = (s2) => OpenPauseMenu()
                                        });
                                    });
                        }
                    });
                });

            MakeMenuItem(PausePanel, "Quit", "", (sender, args) => QuitOnNextUpdate = true);

            PausePanel.Layout();

            GuiRoot.ShowModalPopup(PausePanel);
        }

        public void Destroy()
        {
            InputManager.KeyReleasedCallback -= TemporaryKeyPressHandler;
            InputManager.KeyReleasedCallback -= HandleKeyPress;

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

        public void AutoSave()
        {
            World.Save(
                    String.Format("{0}_{1}", Overworld.Name, World.GameID),
                    (success, exception) =>
                    {
                        World.MakeAnnouncement(success ? "File autosaved." : "Autosave failed - " + exception.Message);
                    });
        }
    }
}
