using System.IO;
using System.Net.Mime;
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

        private List<ContextCommands.ContextCommand> ContextCommands;

        private Gui.Widget MoneyLabel;
        private Gui.Widget LevelLabel;
        private Gui.Widget StocksLabel;
        private Gui.Widgets.FlatToolTray.RootTray BottomToolBar;
        private Gui.Widgets.FlatToolTray.Tray MainMenu;
        private Gui.Widget TimeLabel;
        private Gui.Widget PausePanel;
        private MinimapFrame MinimapFrame;
        private Gui.Widgets.MinimapRenderer MinimapRenderer;
        private Gui.Widgets.GameSpeedControls GameSpeedControls;
        private Widget PausedWidget;
        private Gui.Widgets.InfoTray InfoTray;
        private Gui.Widgets.ToggleTray BrushTray;
        private Gui.Widgets.ToggleTray CameraTray;
        private Gui.Widgets.GodMenu GodMenu;
        private AnnouncementPopup Announcer;
        private FramedIcon EconomyIcon;
        private Timer AutoSaveTimer;
        private EmployeeInfo SelectedEmployeeInfo;
        private Widget ContextMenu;
        private Widget MultiContextMenu;
        private Widget BottomBar;
        private Widget MinimapIcon;
        private ProgressBar ManaBar;

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
            if (MultiContextMenu != null)
            {
                MultiContextMenu.Close();
                MultiContextMenu = null;
            }
            //ManaBar.Hidden = Mode != GameMaster.ToolMode.Magic;
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
                GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
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


                World.ShowToolPopup += text =>
                {
                    if (String.IsNullOrEmpty(text))
                    {
                        if (GuiRoot.TooltipItem != null)
                        {
                            GuiRoot.DestroyWidget(GuiRoot.TooltipItem);
                        }
                        GuiRoot.TooltipItem = null;
                    }
                    else
                    {
                        GuiRoot.RootItem.AddChild(
                          new Gui.Widgets.ToolPopup
                          {
                              Text = text,
                              Rect = new Rectangle(GuiRoot.MousePosition.X - 16, GuiRoot.MousePosition.Y - 16, 128, 64)
                          });
                    }
                };
                 

                World.gameState = this;
                World.OnLoseEvent += World_OnLoseEvent;
                CreateGUIComponents();
                //InputManager.KeyReleasedCallback += TemporaryKeyPressHandler;
                IsInitialized = true;

                SoundManager.PlayMusic("main_theme_day");
                World.Time.Dawn += time =>
                {
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_daytime, 0.15f);
                    SoundManager.PlayMusic("main_theme_day");
                };

                World.Time.NewNight += time =>
                {
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_nighttime, 0.15f);
                    SoundManager.PlayMusic("main_theme_night");
                };

                World.UnpauseThreads();
                AutoSaveTimer = new Timer(GameSettings.Default.AutoSaveTimeMinutes * 60.0f, false, Timer.TimerMode.Real);

                // Todo: Use mod hooks to discover these.
                ContextCommands = new List<DwarfCorp.ContextCommands.ContextCommand>();
                ContextCommands.Add(new ContextCommands.ChopCommand());
                ContextCommands.Add(new ContextCommands.AttackCommand());
                ContextCommands.Add(new ContextCommands.WrangleCommand());
                ContextCommands.Add(new ContextCommands.CancelCommand());
                ContextCommands.Add(new ContextCommands.PriorityCommand());
                ContextCommands.Add(new ContextCommands.GatherCommand());
                ContextCommands.Add(new ContextCommands.DestroyCommand());
                ContextCommands.Add(new ContextCommands.MoveCommand());
                ContextCommands.Add(new DwarfCorp.ContextCommands.EmptyBackpackCommand());
                ContextCommands.Add(new DwarfCorp.ContextCommands.ViewAllowedTasksCommand());
                ContextCommands.Add(new DwarfCorp.ContextCommands.CancelDwarfCommand());
                ContextCommands.Add(new ContextCommands.ChatCommand());
                ContextCommands.Add(new ContextCommands.FireCommand());
                ContextCommands.Add(new ContextCommands.PromoteCommand());
                World.LogEvent(String.Format("We have arrived at {0}", Overworld.Name));
            }
            base.OnEnter();
        }

        /// <summary>
        /// Called when the PlayState is exited and another state (such as the main menu) is loaded.
        /// </summary>
        public override void OnExit()
        {
            World.PauseThreads();
            base.OnExit();
        }

        private float timeOnLastClick = 0.0f;
        private float doubleClickThreshold = 0.25f;

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

            // Needs to run before old input so tools work
            // Update new input system.
            DwarfGame.GumInput.FireActions(GuiRoot, (@event, args) =>
            {
                // Let old input handle mouse interaction for now. Will eventually need to be replaced.

                // Mouse down but not handled by GUI? Collapse menu.
                if (@event == Gui.InputEvents.MouseClick) 
                {
                    GodMenu.CollapseTrays();
                    if (ContextMenu != null)
                    {
                        ContextMenu.Close();
                        ContextMenu = null;
                    }

                    // double click logic.
                    if (args.MouseButton == 0)
                    {
                        float now = (float)gameTime.TotalRealTime.TotalSeconds;
                        if (now - timeOnLastClick < doubleClickThreshold)
                        {
                            World.Camera.ZoomTo(World.CursorLightPos);
                        }
                        timeOnLastClick = now;
                    }

                    if (args.MouseButton == 1) // Right mouse click.
                    {
                        var bodiesClicked = World.ComponentManager.SelectRootBodiesOnScreen(
                            new Rectangle(args.X, args.Y, 1, 1), World.Camera);

                        if (bodiesClicked.Count > 0)
                        {
                            var contextBody = bodiesClicked[0];
                            var availableCommands = ContextCommands.Where(c => c.CanBeAppliedTo(contextBody, World));

                            if (availableCommands.Count() > 0)
                            {
                                // Show context menu.
                                ContextMenu = GuiRoot.ConstructWidget(new ContextMenu
                                {
                                    Commands = availableCommands.ToList(),
                                    Body = contextBody,
                                    World = World
                                });
                                
                                GuiRoot.ShowDialog(ContextMenu);
                                args.Handled = true;
                            }
                        }
                    }
                }

                else if (@event == Gui.InputEvents.KeyUp)
                {
                    args.Handled = HandleKeyPress((Keys)args.KeyValue) || Master.OnKeyReleased((Keys)args.KeyValue);
                }

                else if (@event == Gui.InputEvents.KeyDown)
                {
                    args.Handled = Master.OnKeyPressed((Keys)args.KeyValue);
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
            var pulse = 0.25f * (float)Math.Sin(gameTime.TotalRealTime.TotalSeconds * 4) + 0.25f;
            MoneyLabel.Text = Master.Faction.Economy.CurrentMoney.ToString();
            MoneyLabel.TextColor = Master.Faction.Economy.CurrentMoney > 1.0m ? Color.White.ToVector4() : new Vector4(1.0f, pulse, pulse, 1.0f);
            MoneyLabel.Invalidate();
            int availableSpace = Master.Faction.ComputeRemainingStockpileSpace();
            int totalSpace = Master.Faction.ComputeTotalStockpileSpace();
            StocksLabel.Text = String.Format("    Stocks: {0}/{1}", totalSpace - availableSpace, totalSpace);
            StocksLabel.TextColor = availableSpace > 0 ? Color.White.ToVector4() : new Vector4(1.0f, pulse, pulse, 1.0f);
            StocksLabel.Invalidate();
            LevelLabel.Text = String.Format("{0}/{1}",
                Master.MaxViewingLevel,
                VoxelConstants.ChunkSizeY);
            LevelLabel.Invalidate();
            #endregion

            #region Update toolbar tray

            foreach (var tool in ToolbarItems)
                tool.Icon.Enabled = tool.Available();

            #endregion

            #region Update Economy Indicator

            EconomyIcon.IndicatorValue = World.GoalManager.NewAvailableGoals + World.GoalManager.NewCompletedGoals;

            #endregion

            BottomBar.Layout();

            if (GameSpeedControls.CurrentSpeed != (int) DwarfTime.LastTime.Speed)
            {
                World.Tutorial("time");
            }

            GameSpeedControls.CurrentSpeed = (int)DwarfTime.LastTime.Speed;
           
            if (PausedWidget.Hidden == Paused)
            {
                PausedWidget.Hidden = !Paused;
                PausedWidget.Invalidate();
            }

            // Really just handles mouse pointer animation.
            GuiRoot.Update(gameTime.ToRealTime());

            AutoSaveTimer.Update(gameTime);

            if (GameSettings.Default.AutoSave && AutoSaveTimer.HasTriggered)
            {
                AutoSave();   
            }

            #region select employee

            Master.SelectedMinions.RemoveAll(minion => minion.IsDead);
            if (Master.SelectedMinions.Count == 1)
            {
                // Lol this is evil just trying to reduce the update rate for speed
                if (MathFunctions.RandEvent(0.1f))
                {
                    SelectedEmployeeInfo.Employee = Master.SelectedMinions[0];
                }
            }
            else
            {
                bool update = MathFunctions.RandEvent(0.1f);
                if ((SelectedEmployeeInfo.Employee == null || SelectedEmployeeInfo.Employee.IsDead) && 
                    Master.Faction.Minions.Count > 0)
                {
                    SelectedEmployeeInfo.Employee = Master.Faction.Minions[0];
                }
                else if (update)
                {
                    SelectedEmployeeInfo.Employee = SelectedEmployeeInfo.Employee;
                }
            }
            #endregion

            if (DwarfGame.IsConsoleVisible)
            {
                var statsDisplay = DwarfGame.GetConsoleTile("STATS");

                statsDisplay.Lines.Clear();
                statsDisplay.Lines.Add("** STATISTICS **");
                statsDisplay.Lines.Add(String.Format("{0} ENTITIES", World.ComponentManager.RootComponent.Children.Count));
                statsDisplay.Lines.Add(String.Format("{0} INSTANCES DRAWN", World.InstanceRenderer.InstancesDrawn));
                statsDisplay.Lines.Add(String.Format("{0} MEMORY", BytesToString(System.GC.GetTotalMemory(false))));
                statsDisplay.Invalidate();

                // Todo: Employee AI debug display

                var scheduleDisplay = DwarfGame.GetConsoleTile("SCHEDULE");
                scheduleDisplay.TextSize = 1;
                scheduleDisplay.Lines.Clear();
                foreach (var scheduledEvent in World.GoalManager.EventScheduler.Forecast)
                    scheduleDisplay.Lines.Add(String.Format("{0} : {1}, {2}", scheduledEvent.Event.Name, (scheduledEvent.Date - World.Time.CurrentDate).ToString(@"hh\:mm"), scheduledEvent.Event.Difficulty));

                scheduleDisplay.Lines.Add(String.Format("Difficulty: {0} Forecast {1}", World.GoalManager.EventScheduler.CurrentDifficulty, World.GoalManager.EventScheduler.ForecastDifficulty(World.Time.CurrentDate)));

                scheduleDisplay.Invalidate();
            }
        }

        public static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return String.Format("{0:000} {1}", Math.Sign(byteCount) * num, suf[place]);
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
                        MinimapRenderer.Render(new Rectangle(MinimapFrame.Rect.X, MinimapFrame.Rect.Bottom - 192, 192, 192), GuiRoot);
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

        private bool BuildRequirementsEqual(VoxelType voxA, VoxelType voxB)
        {
            return voxA == voxB || !(voxA.BuildRequirements.Count != voxB.BuildRequirements.Count || voxA.BuildRequirements.Any(r => !voxB.BuildRequirements.Contains(r)));
        }

        void UpdateBlockWidget(Gui.Widget sender, VoxelType data)
        {

            int numResources;
            if (!int.TryParse(sender.Text, out numResources))
            {
                sender.Text = "";
                sender.Invalidate();
                numResources = 0;
            }

            var resourceCount = Master.Faction.ListResourcesInStockpilesPlusMinions()
                .Where(r => data.CanBuildWith(ResourceLibrary.GetResourceByName(r.Key))).Sum(r => r.Value.First.NumResources + r.Value.Second.NumResources);

            int newNum = Math.Max(resourceCount -
                World.PlayerFaction.Designations.EnumerateDesignations(DesignationType.Put).Count(d =>
                    BuildRequirementsEqual(VoxelLibrary.GetVoxelType(d.Tag.ToString()), data)), 0);

            if (newNum != numResources)
            {
                sender.Text = newNum.ToString();
                sender.Invalidate();
            }
        }

        /// <summary>
        /// Creates all of the sub-components of the GUI in for the PlayState (buttons, etc.)
        /// </summary>
        public void CreateGUIComponents()
        {
            var bottomBackground = GuiRoot.RootItem.AddChild(new TrayBackground
            {
                Corners = Scale9Corners.Top,
                MinimumSize = new Point(0, 102),
                AutoLayout = AutoLayout.DockBottom
            });

            BottomBar = bottomBackground.AddChild(new Gui.Widget
            {
                Transparent = false,
                Background = new TileReference("basic", 0),
                BackgroundColor = new Vector4(0, 0, 0, 0.5f),
                Padding = new Margin(0, 0, 2, 2),
                MinimumSize = new Point(0, 36),
                AutoLayout = AutoLayout.DockBottom
            });

            var secondBar = bottomBackground.AddChild(new Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 54),
                AutoLayout = AutoLayout.DockBottom,
                InteriorMargin = new Margin(2,0,0,0),
                Padding = new Margin(0,0,2,2)
            });

            #region Setup company information section
            BottomBar.AddChild(new CompanyLogo
            {
                Tag = "company info",
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gui.AutoLayout.DockLeftCentered,
                CompanyInformation = World.PlayerCompany.Information,
                Tooltip = World.PlayerCompany.Information.Name
            });

            BottomBar.AddChild(new Gui.Widget
            {
                Background = new Gui.TileReference("resources", 40),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gui.AutoLayout.DockLeftCentered
            });

            MoneyLabel = BottomBar.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockLeftCentered,
                Font = "font10",
                TextVerticalAlign = Gui.VerticalAlign.Center,
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = StringLibrary.GetString("money-amount")
            });

            StocksLabel = BottomBar.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockLeftCentered,
                Font = "font10",
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Tooltip = StringLibrary.GetString("stockpile-tooltip")
            });

            BottomBar.AddChild(new Gui.Widget
            {
                Background = new Gui.TileReference("resources", 42),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gui.AutoLayout.DockLeftCentered,
                Tooltip = StringLibrary.GetString("slicer-tooltip")
            });

            BottomBar.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 7),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.DockLeftCentered,
                OnClick = (sender, args) =>
                {
                    Master.SetMaxViewingLevel(Master.MaxViewingLevel - 1);
                },
                Tooltip = StringLibrary.GetString("slicer-down-tooltip")
            });

            BottomBar.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 3),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.DockLeftCentered,
                OnClick = (sender, args) =>
                {
                    Master.SetMaxViewingLevel(Master.MaxViewingLevel + 1);
                },
                Tooltip = StringLibrary.GetString("slicer-up-tooltip")
            });

            LevelLabel = BottomBar.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockLeftCentered,
                Font = "font10",
                TextVerticalAlign = Gui.VerticalAlign.Center,
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = StringLibrary.GetString("slicer-current-tooltip")
            });
            #endregion

            GuiRoot.RootItem.AddChild(new Gui.Widgets.ResourcePanel
            {
                AutoLayout = AutoLayout.FloatTop,
                Master = Master,
                Transparent = true,
            });

            #region Setup time display
            TimeLabel = BottomBar.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockRightCentered,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                MinimumSize = new Point(128, 20),
                Font = "font10",
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = StringLibrary.GetString("time-tooltip")
            });
            #endregion

            #region Toggle panel buttons

            MinimapRenderer = new Gui.Widgets.MinimapRenderer(192, 192, World, ContentPaths.Terrain.terrain_colormap);

            MinimapFrame = GuiRoot.RootItem.AddChild(new MinimapFrame
            {
                Tag = "minimap",
                Renderer = MinimapRenderer,
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(208, 204),
                OnLayout = (sender) => sender.Rect.Y += 4
            }) as MinimapFrame;

            SelectedEmployeeInfo = GuiRoot.RootItem.AddChild(new EmployeeInfo
            {
                Hidden = true,
                Border = "border-fancy",
                Employee = null,
                EnablePosession = true,
                Tag = "selected-employee-info",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(400, 500),
                OnFireClicked = (sender) =>
                {
                    GuiRoot.ShowModalPopup(GuiRoot.ConstructWidget(new Gui.Widgets.Confirm
                    {
                        OkayText = StringLibrary.GetString("fire-dwarf"),
                        CancelText = StringLibrary.GetString("keep-dwarf"),
                        Padding = new Margin(32, 10, 10, 10),
                        MinimumSize = new Point(512, 128),
                        OnClose = (confirm) =>
                        {
                            if ((confirm as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                            {
                                SoundManager.PlaySound(ContentPaths.Audio.change, 0.25f);
                                var employeeInfo = (sender as EmployeeInfo);
                                if (employeeInfo == null)
                                {
                                    Console.Error.WriteLine("Error firing dwarf. This should not have happened!");
                                    World.MakeAnnouncement(StringLibrary.GetString("firing-error"));
                                    return;
                                }
                                var selectedEmployee = (sender as EmployeeInfo).Employee;
                                if (selectedEmployee.IsDead)
                                {
                                    return;
                                }
                                selectedEmployee.GetRoot().GetComponent<Inventory>().Die();
                                World.MakeAnnouncement(StringLibrary.GetString("was-fired", selectedEmployee.Stats.FullName));
                                selectedEmployee.GetRoot().Delete();

                                Master.Faction.Minions.Remove(selectedEmployee);
                                Master.Faction.SelectedMinions.Remove(selectedEmployee);
                            }
                        }
                    }));
                }
            }) as EmployeeInfo;

            var markerFilter = GuiRoot.RootItem.AddChild(new DesignationFilter
            {
                DesignationDrawer = World.DesignationDrawer,
                DesignationSet = World.PlayerFaction.Designations,
                Hidden = true,
                Border = "border-fancy",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(300, 200)
            });

            var taskList = GuiRoot.RootItem.AddChild(new TaskListPanel
            {
                Border = "border-thin",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(600, 300),
                Hidden = true,
                World = this.World
            });

            MinimapIcon = new FramedIcon
            {
                Icon = new Gui.TileReference("tool-icons", 33),
                Text = "@play-map-icon-label",
                Tooltip = "@play-map-icon-tooltip",
                EnabledTextColor = Vector4.One,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                OnClick = (sender, args) =>
                {
                    if (MinimapFrame.Hidden)
                    {
                        MinimapFrame.Hidden = false;
                        SelectedEmployeeInfo.Hidden = true;
                        markerFilter.Hidden = true;
                        taskList.Hidden = true;
                    }
                    else
                        MinimapFrame.Hidden = true;
                }
            };

            var bottomLeft = secondBar.AddChild(new Gui.Widgets.IconTray
            {
                Corners = 0,
                Transparent = true,
                AutoLayout = Gui.AutoLayout.DockLeft,
                SizeToGrid = new Point(4, 1),
                ItemSource = new Gui.Widget[]
                        {
                            MinimapIcon,
                            new FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 34),
                                Text = "@play-employee-icon-label",
                                Tooltip = "@play-employee-icon-tooltip",
                                EnabledTextColor = Vector4.One,
                                TextHorizontalAlign = HorizontalAlign.Center,
                                TextVerticalAlign = VerticalAlign.Below,
                                OnClick = (sender, args) =>
                               {
                                   if (SelectedEmployeeInfo.Hidden)
                                   {
                                       MinimapFrame.Hidden = true;
                                       SelectedEmployeeInfo.Hidden = false;
                                       markerFilter.Hidden = true;
                                       taskList.Hidden = true;
                                   }
                                   else
                                       SelectedEmployeeInfo.Hidden = true;
                               }
                            },

                            new FramedIcon
                            {
                               Icon = null,
                               Text = "@play-marks-icon-label",
                               Tooltip = "@play-marks-icon-tooltip",
                               TextHorizontalAlign = HorizontalAlign.Center,
                               TextVerticalAlign = VerticalAlign.Center,
                               EnabledTextColor = Vector4.One,
                               OnClick = (sender, args) =>
                               {
                                   if (markerFilter.Hidden)
                                   {
                                       MinimapFrame.Hidden = true;
                                       SelectedEmployeeInfo.Hidden = true;
                                       taskList.Hidden = true;
                                       markerFilter.Hidden = false;
                                   }
                                   else
                                       markerFilter.Hidden = true;
                               }
                            },

                            new FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 35),
                                Text = "@play-tasks-icon-label",
                                Tooltip = "@play-tasks-icon-tooltip",
                                TextHorizontalAlign = HorizontalAlign.Center,
                                TextVerticalAlign = VerticalAlign.Below,
                                EnabledTextColor = Vector4.One,
                                OnClick = (sender, args) =>
                                {
                                    if (taskList.Hidden)
                                    {
                                        MinimapFrame.Hidden = true;
                                        SelectedEmployeeInfo.Hidden = true;
                                        markerFilter.Hidden = true;
                                        taskList.Hidden = false;
                                    }
                                    else
                                        taskList.Hidden = true;
                                }
                            }
                        },
            });

            secondBar.AddChild(new Widget
            {
                Transparent = true,
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(21, 0)
            });

            #endregion

            #region Setup right tray

            EconomyIcon = new Gui.Widgets.FramedIcon
            {
                Tag = "economy",
                Icon = new Gui.TileReference("tool-icons", 10),
                OnClick = (sender, args) => StateManager.PushState(new NewEconomyState(Game, StateManager, World)),
                DrawIndicator = true,
                Tooltip = StringLibrary.GetString("economy-tooltip"),
                Text = StringLibrary.GetString("economy-label"),
                TextVerticalAlign = VerticalAlign.Below
            };

            var topRightTray = secondBar.AddChild(new Gui.Widgets.IconTray
            {
                Corners = 0,//Gui.Scale9Corners.Top,
                Transparent = true,
                AutoLayout = Gui.AutoLayout.DockRight,
                SizeToGrid = new Point(4, 1),
                ItemSource = new Gui.Widget[] 
                        {
                            new Gui.Widgets.FramedIcon()
                            {
                                 Icon = new Gui.TileReference("tool-icons", 21),
                                OnClick = (sender, args) =>
                                {
                                    StateManager.PushState(new EventLogState(Game, StateManager, World.EventLog, World.Time.CurrentDate));
                                },
                                Text = StringLibrary.GetString("events-label"),
                                TextVerticalAlign = VerticalAlign.Below,
                                Tooltip = StringLibrary.GetString("events-tooltip")
                            },
#if DEBUG
                            new Gui.Widgets.FramedIcon()
                            {
                                 Icon = new Gui.TileReference("tool-icons", 21),
                                OnClick = (sender, args) =>
                                {
                                    StateManager.PushState(new FactionViewState(GameState.Game, GameState.Game.StateManager, World));
                                },
                                Text = "Diplo.",
                                TextVerticalAlign = VerticalAlign.Below,
                                Tooltip = "View diplomacy with other factions."
                            },
#endif
                            EconomyIcon,
                                                                   
                            new Gui.Widgets.FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 12),
                                OnClick = (sender, args) => { OpenPauseMenu(); },
                                Tooltip = StringLibrary.GetString("settings-tooltip"),
                                Text = StringLibrary.GetString("settings-label"),
                                TextVerticalAlign = VerticalAlign.Below
                            }
                        },
            });


            secondBar.AddChild(new Widget
            {
                Transparent = true,
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(21, 0)
            });

            #endregion

            #region Setup game speed controls

            GameSpeedControls = BottomBar.AddChild(new GameSpeedControls
            {
                Tag = "speed controls",
                AutoLayout = AutoLayout.DockRightCentered,
                
                OnSpeedChanged = (sender, speed) =>
                {
                    if ((int) DwarfTime.LastTime.Speed != speed)
                    {
                        World.Tutorial("time");
                        if ((int) DwarfTime.LastTime.Speed == 0)
                        {
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_unpause, 0.2f);
                        }
                        switch (speed)
                        {
                            case 1:
                                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_1x, 0.2f);
                                break;
                            case 2:
                                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_2x, 0.2f);
                                break;
                            case 3:
                                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_3x, 0.2f);
                                break;
                            case 0:
                                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_pause, 0.2f);
                                break;
                        }
                        DwarfTime.LastTime.Speed = (float)speed;
                        Paused = speed == 0;
                        PausedWidget.Hidden = !Paused;
                        PausedWidget.Tooltip = "(push " + ControlSettings.Mappings.Pause.ToString() + " to unpause)";
                        PausedWidget.Invalidate();
                    }
                },
                Tooltip = "Game speed controls."
            }) as GameSpeedControls;

            PausedWidget = GuiRoot.RootItem.AddChild(new Widget()
            {
                Text = "\n\nPaused",
                AutoLayout = Gui.AutoLayout.FloatCenter,
                Tooltip = "(push " + ControlSettings.Mappings.Pause.ToString() + " to unpause)",
                Font = "font18-outline",
                TextColor = Color.White.ToVector4(),
                MaximumSize = new Point(0, 0),
                WrapText = false,
                Hidden = true,
            });

            #endregion

            #region Announcer and info tray

            Announcer = GuiRoot.RootItem.AddChild(new AnnouncementPopup
            {
                OnLayout = (sender) =>
                {
                    sender.Rect = new Rectangle(GuiRoot.RenderData.VirtualScreen.Width - 350,
                        secondBar.Rect.Top - 130, 350, 128);
                }
            }) as AnnouncementPopup;

            World.OnAnnouncement = (message) =>
            {
                Announcer.QueueAnnouncement(message);
            };

            InfoTray = GuiRoot.RootItem.AddChild(new InfoTray
            {
                OnLayout = (sender) =>
                {
                    sender.Rect = new Rectangle(0,0,0,0);
                },
                Transparent = true
            }) as InfoTray;

            #endregion

            #region Setup brush

            BrushTray = BottomBar.AddChild(new Gui.Widgets.ToggleTray
            {
                Tag = "brushes",
                AutoLayout = AutoLayout.DockLeftCentered,
                SizeToGrid = new Point(3, 1),
                ItemSize = new Point(32, 32),
                InteriorMargin = new Margin(2,2,2,2),
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

            CameraTray = BottomBar.AddChild(new Gui.Widgets.ToggleTray
            {
                Tag = "camera_modes",
                AutoLayout = AutoLayout.DockLeftCentered,
                SizeToGrid = new Point(2, 1),
                ItemSize = new Point(32, 32),
                InteriorMargin = new Margin(2, 2, 2, 2),
                ToggledTint = Color.Yellow.ToVector4(),
                ItemSource = new Gui.Widget[]

                  {
                            new Gui.Widgets.FramedIcon
                            {
                                Text = "Orbit",
                                DrawFrame = true,
                                Tooltip = "Topdown orbit camera mode.",
                                TextVerticalAlign = VerticalAlign.Center,
                                TextHorizontalAlign = HorizontalAlign.Center,
                                ChangeColorOnHover = true,
                                HoverTextColor = Color.Yellow.ToVector4(),
                                TextColor = Color.Yellow.ToVector4(),
                                ChangeTextColorOnEnable = false,
                                OnClick = (widget, args) =>
                                {
                                    World.ChangeCameraMode(OrbitCamera.ControlType.Overhead);
                                }
                            },
                            new Gui.Widgets.FramedIcon
                            {
                                Text = "Walk",
                                DrawFrame = true,
                                Tooltip = "Walk camera mode.",
                                ChangeColorOnHover = true,
                                TextVerticalAlign = VerticalAlign.Center,
                                TextHorizontalAlign = HorizontalAlign.Center,
                                HoverTextColor = Color.Yellow.ToVector4(),
                                ChangeTextColorOnEnable = false,
                                OnClick = (widget, args) =>
                                {
                                    World.Tutorial("walk_camera");
                                    World.ChangeCameraMode(OrbitCamera.ControlType.Walk);
                                }
                            }
                  }
            }) as Gui.Widgets.ToggleTray;

            if (World.Camera.Control == OrbitCamera.ControlType.Overhead)
            {
                CameraTray.Select(0);
            }
            else
            {
                CameraTray.Select(1);
            }

            #endregion

            #region Setup tool tray

            #region icon_SelectTool

            var icon_SelectTool = new FlatToolTray.Icon
            {
                Tag = "select",
                Text = "Select",
                TextVerticalAlign = VerticalAlign.Below,
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
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
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
                        Text = TextGenerator.Shorten(data.Name, 5),
                        TextVerticalAlign = VerticalAlign.Below,
                        TextColor = Color.White.ToVector4(),
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
                            ChangeTool(GameMaster.ToolMode.BuildZone);
                            World.ShowToolPopup("Click and drag to build " + data.Name);
                            World.Tutorial("build rooms");
                        },
                        Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                        OnConstruct = (sender) =>
                        {
                            AddToolbarIcon(sender, () => { return true; });
                        }
                    }))
            };

            var icon_moveObjects = new FlatToolTray.Icon()
            {
                Text = "Move",
                TextVerticalAlign = VerticalAlign.Below,
                Tooltip = "Move/Destroy objects",
                Icon = new TileReference("mouse", 9),
                OnClick = (sender, args) =>
                {
                    World.ShowToolPopup("Left click objects to move them.\nRight click to destroy them.");
                    ChangeTool(GameMaster.ToolMode.MoveObjects);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            var icon_destroyObjects = new FlatToolTray.Icon()
            {
                Text = "Destroy",
                TextVerticalAlign = VerticalAlign.Below,
                Tooltip = "Deconstruct objects",
                Icon = new TileReference("round-buttons", 5),
                OnClick = (sender, args) =>
                {
                    World.ShowToolPopup("Left click objects to destroy them.");
                    ChangeTool(GameMaster.ToolMode.DeconstructObjects);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            var icon_BuildRoom = new FlatToolTray.Icon
            {
                EnabledTextColor = Vector4.One,
                Text = "Zone",
                Tooltip = "Designate zones/areas",
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
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
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
                        .Where(voxel => voxel.IsBuildable && World.PlayerFaction.ListResources().Any(r =>voxel.CanBuildWith(ResourceLibrary.GetResourceByName(r.Value.ResourceType))))
                        .Select(data => new FlatToolTray.Icon
                        {
                            Tooltip = "Build " + data.Name,
                            Icon = new Gui.TileReference("voxels", data.ID),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            Text = "",
                            EnabledTextColor = Color.White.ToVector4(),
                            Font = "font10-outline-numsonly",
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
                                var tool = Master.Tools[GameMaster.ToolMode.BuildWall] as BuildWallTool;
                                tool.CurrentVoxelType = (byte)data.ID;
                                ChangeTool(GameMaster.ToolMode.BuildWall);
                                World.ShowToolPopup("Click and drag to build " + data.Name + " wall.");
                                World.Tutorial("build blocks");
                            },
                            OnUpdate = (sender, args) => UpdateBlockWidget(sender, data),
                            Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                            Hidden = false
                        }));
                    
                    widget.Construct();
                    widget.Hidden = false;
                    widget.Layout();
                }
            };


            var menu_Floortypes = new FlatToolTray.Tray
            {
                Tag = "build floor",
                ItemSource = new List<Widget>(),
                OnShown = (widget) =>
                {
                    // Dynamically rebuild the tray
                    widget.Clear();
                    (widget as FlatToolTray.Tray).ItemSource =
                        (new Widget[] { icon_menu_WallTypes_Return }).Concat(
                        VoxelLibrary.GetTypes()
                        .Where(voxel => voxel.IsBuildable && World.PlayerFaction.ListResources().Any(r => voxel.CanBuildWith(ResourceLibrary.GetResourceByName(r.Value.ResourceType))))
                        .Select(data => new FlatToolTray.Icon
                        {
                            Tooltip = "Build " + data.Name,
                            Icon = new Gui.TileReference("voxels", data.ID),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            Text = "",
                            EnabledTextColor = Color.White.ToVector4(),
                            Font = "font10-outline-numsonly",
                            PopupChild = new BuildWallInfo
                            {
                                Data = data,
                                Rect = new Rectangle(0, 0, 256, 128),
                                Master = Master
                            },
                            OnClick = (sender, args) =>
                            {
                                Master.Faction.RoomBuilder.CurrentRoomData = null;
                                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                                var tool = Master.Tools[GameMaster.ToolMode.BuildWall] as BuildWallTool;
                                tool.CurrentVoxelType = (byte)data.ID;
                                ChangeTool(GameMaster.ToolMode.BuildWall); // Wut
                                World.ShowToolPopup("Click and drag to build " + data.Name + " floor.");
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
                Font = "font8",
                KeepChildVisible = true,
                ExpandChildWhenDisabled = true,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Tooltip = "Place blocks",
                Text = "Block",
                EnabledTextColor = Color.White.ToVector4(),
                ReplacementMenu = menu_WallTypes,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var icon_BuildFloor = new FlatToolTray.Icon
            {
                Icon = null,
                Font = "font8",
                KeepChildVisible = true,
                ExpandChildWhenDisabled = true,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Tooltip = "Place floor",
                Text = "Floor",
                EnabledTextColor = Color.White.ToVector4(),
                ReplacementMenu = menu_Floortypes,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_BuildCraft

            var icon_menu_CraftTypes_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
            };

            Func<CraftItem, FlatToolTray.Icon> createCraftIcon = (data) => new FlatToolTray.Icon
            {
                Icon = data.Icon,
                Tooltip = StringLibrary.GetString("craft", data.DisplayName),
                KeepChildVisible = true, // So the player can interact with the popup.
                ExpandChildWhenDisabled = true,
                Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                Text = data.ShortDisplayName,
                TextVerticalAlign = VerticalAlign.Below,
                TextColor = Color.White.ToVector4(),
                PopupChild = new TabPanel
                {
                    Rect = new Rectangle(0, 0, 450, 250),
                    TextColor = Color.Black.ToVector4(),
                    HoverTextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4(),
                    ButtonFont = "font10",
                    OnShown = (sender) =>
                    {
                        var panel = (sender as TabPanel);
                        panel.SelectedTab = 0;
                    },
                    OnSelectedTabChanged = (widget) =>
                    {
                        var panel = (widget as TabPanel);
                        this.GuiRoot.SafeCall(panel.GetTabButton(panel.SelectedTab).OnShown, panel.GetTabButton(panel.SelectedTab));
                    },
                    TabSource = new KeyValuePair<String, Widget>[] {
                        new KeyValuePair<string, Widget>(
                            StringLibrary.GetString("place"),
                            new BuildCraftInfo
                            {
                                Data = data,
                                AllowWildcard = true,
                                Rect = new Rectangle(0, 0, 450, 200),
                                Master = Master,
                                World = World,
                                Transparent = true,
                                BuildAction = (sender, args) =>
                                {
                                    var buildInfo = sender as BuildCraftInfo;
                                    if (buildInfo == null)
                                        return;
                                    sender.Parent.Parent.Hidden = true;
                                    var tool = Master.Tools[GameMaster.ToolMode.BuildObject] as BuildObjectTool;
                                    tool.SelectedResources = buildInfo.GetSelectedResources();
                                    Master.Faction.RoomBuilder.CurrentRoomData = null;
                                    Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                                    tool.CraftType = data;
                                    tool.Mode = BuildObjectTool.PlacementMode.BuildNew;

                                    // Todo: This should never be true.
                                    if (tool.PreviewBody != null)
                                    {
                                        tool.PreviewBody.GetRoot().Delete();
                                        tool.PreviewBody = null;
                                    }

                                    ChangeTool(GameMaster.ToolMode.BuildObject);
                                    World.ShowToolPopup(StringLibrary.GetString("click-and-drag", data.Verb, data.DisplayName));
                                },
                                PlaceAction = (sender, args) =>
                                {
                                    var buildInfo = sender as BuildCraftInfo;
                                    if (buildInfo == null)
                                        return;
                                    sender.Parent.Parent.Hidden = true;
                                    var tool = Master.Tools[GameMaster.ToolMode.BuildObject] as BuildObjectTool;
                                    tool.SelectedResources = null;
                                    Master.Faction.RoomBuilder.CurrentRoomData = null;
                                    Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                                    tool.CraftType = data;
                                    tool.Mode = BuildObjectTool.PlacementMode.PlaceExisting;

                                    // Todo: This should never be true.
                                    if (tool.PreviewBody != null)
                                    {
                                        tool.PreviewBody.GetRoot().Delete();
                                        tool.PreviewBody = null;
                                    }

                                    ChangeTool(GameMaster.ToolMode.BuildObject);
                                    World.ShowToolPopup(StringLibrary.GetString("place", data.DisplayName));
                                }
                            }),
                        new KeyValuePair<string, Widget>(
                            StringLibrary.GetString("stockpile"),
                            new BuildCraftInfo
                            {
                                Data = data.ObjectAsCraftableResource(),
                                Rect = new Rectangle(0, 0, 450, 200),
                                Master = Master,
                                World = World,
                                Transparent = true,
                                BuildAction = (sender, args) =>
                                {
                                    var buildInfo = (sender as Gui.Widgets.BuildCraftInfo);
                                    if (buildInfo == null)
                                        return;
                                    sender.Parent.Parent.Hidden = true;
                                    var assignments = new List<Task>();
                                    for (int i = 0; i < buildInfo.GetNumRepeats(); i++)
                                    {
                                        assignments.Add(new CraftResourceTask(data.ObjectAsCraftableResource(), 1, buildInfo.GetSelectedResources()));
                                    }
                                    World.Master.TaskManager.AddTasks(assignments);
                                    World.ShowToolPopup(data.CurrentVerb + " " + buildInfo.GetNumRepeats() + " " + data.Name);
                                    World.Tutorial("build crafts");
                                },
                            })
                    }
                },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () => true);
                },
            };

            Dictionary<string, bool> categoryExists = new Dictionary<string, bool>();

            var category_return_button = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                },
                //TODO
                ReplacementMenu = null
            };

            Func<string, Widget> createCraftCategory = category =>
            {
                var menu_category = new FlatToolTray.Tray
                {
                    ItemSource = (new Widget[] { category_return_button }).Concat(
                    CraftLibrary.EnumerateCraftables().ToList().Where(item => item.Type == CraftItem.CraftType.Object && item.Category == category)
                    .Select(data => createCraftIcon(data)))
                };
                var firstItem = CraftLibrary.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Object && item.Category == category).First();
                var category_icon = new FlatToolTray.Icon
                {
                    Icon = firstItem.Icon,
                    Tooltip = "Craft " + category,
                    Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                    ReplacementMenu = menu_category,
                    Text = TextGenerator.Shorten(category, 5),
                    TextVerticalAlign = VerticalAlign.Below,
                    TextColor = Color.White.ToVector4(),
                };
                return category_icon;
            };

            List<CraftItem> rootObjects = new List<CraftItem>();

            foreach (var item in CraftLibrary.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Object && item.AllowUserCrafting))
            {
                if (string.IsNullOrEmpty(item.Category) || !categoryExists.ContainsKey(item.Category))
                {
                    rootObjects.Add(item);
                    if (!string.IsNullOrEmpty(item.Category))
                    {
                        categoryExists[item.Category] = true;
                    }
                }
            };

            var menu_CraftTypes = new FlatToolTray.Tray
            {
                Tag = "craft item",
                ItemSource = (new Widget[]{ icon_menu_CraftTypes_Return }).Concat(
                    rootObjects.Select(data =>
                    {
                        if (string.IsNullOrEmpty(data.Category))
                        {
                            return createCraftIcon(data);
                        }
                        return createCraftCategory(data.Category);
                    }))
            };

            category_return_button.ReplacementMenu = menu_CraftTypes;

            var icon_BuildCraft = new FlatToolTray.Icon
            {
                Icon = null,
                Text = "Object",
                EnabledTextColor = Vector4.One,
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
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_ResourceTypes = new FlatToolTray.Tray
            {
                Tag = "craft resource",
                Tooltip = "Craft resource",
                ItemSource = (new Widget[] { icon_menu_ResourceTypes_Return }).Concat(
                    CraftLibrary.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Resource
                    && item.AllowUserCrafting
                    && ResourceLibrary.Resources.ContainsKey(item.ResourceCreated) &&
                    !ResourceLibrary.Resources[item.ResourceCreated].Tags.Contains(Resource.ResourceTags.Edible) &&
                    item.CraftLocation != "Apocethary")
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.Icon,
                        Tooltip = data.Verb + " a " + data.Name,
                        KeepChildVisible = true, // So the player can interact with the popup.
                        ExpandChildWhenDisabled = true,
                        Text = TextGenerator.Shorten(data.Name, 6),
                        TextVerticalAlign = VerticalAlign.Below,
                        TextColor = Color.White.ToVector4(),
                        Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                        PopupChild = new BuildCraftInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 450, 200),
                            Master = Master,
                            World = World,
                            BuildAction = (sender, args) =>
                            {
                                var buildInfo = (sender as Gui.Widgets.BuildCraftInfo);
                                if (buildInfo == null)
                                    return;
                                sender.Hidden = true;
                                var assignments = new List<Task>();
                                for (int i = 0; i < buildInfo.GetNumRepeats(); i++)
                                {
                                    assignments.Add(new CraftResourceTask(data, 1, buildInfo.GetSelectedResources()));
                                }
                                World.Master.TaskManager.AddTasks(assignments);
                                World.ShowToolPopup(data.CurrentVerb + " " + buildInfo.GetNumRepeats() + " " + data.Name);
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
                Tooltip = "Resource",
                EnabledTextColor = Vector4.One,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                KeepChildVisible = true,
                ReplacementMenu = menu_ResourceTypes,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_Rail

            var icon_menu_Rail_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
            };

            var icon_menu_Rail_Paint = new FlatToolTray.Icon
            {
                Icon = new TileReference("rail", 0),
                Tooltip = "Paint",
                Text = "paint",
                TextVerticalAlign = VerticalAlign.Below,
                TextColor = Color.White.ToVector4(),
                Behavior = FlatToolTray.IconBehavior.LeafIcon,
                OnClick = (widget, args) =>
                {
                    Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty; // This should be set by the tool.
                    var railTool = Master.Tools[GameMaster.ToolMode.PaintRail] as Rail.PaintRailTool;
                    railTool.SelectedResources = new List<ResourceAmount>
                                    {
                                        new ResourceAmount("Rail", 1)
                                    };
                    ChangeTool(GameMaster.ToolMode.PaintRail);
                }
            };

            var menu_Rail = new FlatToolTray.Tray
            {
                Tag = "build rail",
                ItemSource = new List<Widget>(),
                OnShown = (widget) =>
                {
                    // Dynamically rebuild the tray
                    widget.Clear();
                    (widget as FlatToolTray.Tray).ItemSource =
                        (new Widget[] { icon_menu_Rail_Return, icon_menu_Rail_Paint }).Concat(
                            Rail.RailLibrary.EnumeratePatterns()
                            .Select(data => new FlatToolTray.Icon
                            {
                                Tooltip = "Build " + data.Name,
                                Text = TextGenerator.Shorten(data.Name, 6),
                                TextVerticalAlign = VerticalAlign.Below,
                                TextColor = Color.White.ToVector4(),
                                Icon = new TileReference("rail", data.Icon),
                                KeepChildVisible = true,
                                ExpandChildWhenDisabled = true,
                                Behavior = FlatToolTray.IconBehavior.LeafIcon,
                                OnClick = (sender, args) =>
                                {
                                    Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty; // This should be set by the tool.
                                    var railTool = Master.Tools[GameMaster.ToolMode.BuildRail] as Rail.BuildRailTool;
                                    railTool.Pattern = data;
                                    ChangeTool(GameMaster.ToolMode.BuildRail);
                                },
                                Hidden = false
                            }));
                    widget.Construct();
                    widget.Hidden = false;
                    widget.Layout();
                }
            };

            var icon_RailTool = new FlatToolTray.Icon
            {
                Text = "Rail",
                EnabledTextColor = Vector4.One,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Tooltip = "Rail",
                KeepChildVisible = true,
                ReplacementMenu = menu_Rail,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_BuildTool

            var icon_menu_BuildTools_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
            };

            var menu_BuildTools = new FlatToolTray.Tray
            {
                ItemSource = new FlatToolTray.Icon[]
                    {
                        icon_menu_BuildTools_Return,
                        icon_moveObjects,
                        icon_destroyObjects,
                        icon_BuildRoom,
                        icon_BuildWall,
                        icon_BuildFloor,
                        icon_BuildCraft,
                        icon_BuildResource,
                        icon_RailTool,
                    }
            };

            icon_menu_CraftTypes_Return.ReplacementMenu = menu_BuildTools;
            icon_menu_ResourceTypes_Return.ReplacementMenu = menu_BuildTools;
            icon_menu_RoomTypes_Return.ReplacementMenu = menu_BuildTools;
            icon_menu_WallTypes_Return.ReplacementMenu = menu_BuildTools;
            icon_menu_Rail_Return.ReplacementMenu = menu_BuildTools;

            var icon_BuildTool = new FlatToolTray.Icon
            {
                Tag = "build",
                Text = "Build",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 2),
                KeepChildVisible = true,
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () => Master.Faction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.BuildZone)));
                    AddToolSelectIcon(GameMaster.ToolMode.BuildZone, sender);
                },
                Tooltip = "Build",
                ReplacementMenu = menu_BuildTools,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_CookTool

            var icon_menu_Edibles_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
            };

            var menu_Edibles = new FlatToolTray.Tray
            {
                ItemSource = (new Widget[] { icon_menu_Edibles_Return }).Concat(
                    CraftLibrary.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Resource
                    && item.AllowUserCrafting
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
                            Rect = new Rectangle(0, 0, 450, 200),
                            Master = Master,
                            World = World,
                            BuildAction = (sender, args) =>
                            {
                                var buildInfo = sender as Gui.Widgets.BuildCraftInfo;
                                sender.Hidden = true;
                                List<Task> assignments = new List<Task> { new CraftResourceTask(data, buildInfo.GetNumRepeats(), buildInfo.GetSelectedResources()) };
                                World.Master.TaskManager.AddTasks(assignments);
                                World.ShowToolPopup(data.CurrentVerb + " one " + data.Name);
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
                Text = "Cook",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 27),
                KeepChildVisible = true,
                Tooltip = "Cook food",
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Cook)));
                },
                ReplacementMenu = menu_Edibles,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_PotionTool
            var icon_menu_potions_return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
            };

            // TODO: Translation
            Func<string, string> potionNameToLabel = (string name) =>
            {
                var replacement = name.Replace("Potion", "").Replace("of", "");
                return TextGenerator.Shorten(replacement, 6);
            };

            var menu_potions = new FlatToolTray.Tray
            {
                ItemSource = (new Widget[] { icon_menu_Edibles_Return }).Concat(
                    CraftLibrary.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Resource
                    && item.AllowUserCrafting
                    && ResourceLibrary.Resources.ContainsKey(item.ResourceCreated)
                    && item.CraftLocation == "Apocethary")
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.Icon,
                        KeepChildVisible = true, // So the player can interact with the popup.
                        Tooltip = StringLibrary.GetString("verb-noun", data.Verb, data.DisplayName),
                        Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                        Text = potionNameToLabel(data.DisplayName),
                        TextVerticalAlign = VerticalAlign.Below,
                        TextHorizontalAlign = HorizontalAlign.Center,
                        TextColor = Color.White.ToVector4(),
                        PopupChild = new BuildCraftInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 450, 200),
                            Master = Master,
                            World = World,
                            BuildAction = (sender, args) =>
                            {
                                var buildInfo = sender as Gui.Widgets.BuildCraftInfo;
                                sender.Hidden = true;
                                List<Task> assignments = new List<Task> { new CraftResourceTask(data, buildInfo.GetNumRepeats(), buildInfo.GetSelectedResources()) };
                                World.Master.TaskManager.AddTasks(assignments);
                                World.ShowToolPopup(data.CurrentVerb + " " + data.Name);
                                World.Tutorial("potions");
                            },
                        },
                        OnConstruct = (sender) =>
                        {
                            AddToolbarIcon(sender, () => ((sender as FlatToolTray.Icon).PopupChild as BuildCraftInfo).CanBuild());
                        }
                    }))
            };

            var icon_Potions = new FlatToolTray.Icon
            {
                Tag = "potions",
                Text = "Potion",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("resources", 44),
                KeepChildVisible = true,
                Tooltip = "Brew potions",
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Research)) && Master.Faction.OwnedObjects.Any(obj => obj.Tags.Contains("Apocethary")));
                },
                ReplacementMenu = menu_potions,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };
            #endregion

            #region icon_DigTool

            var icon_DigTool = new FlatToolTray.Icon
            {
                Tag = "dig",
                Text = "Dig",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 0),
                Tooltip = "Dig",
                OnClick = (sender, args) => ChangeTool(GameMaster.ToolMode.Dig),
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Dig)));
                    AddToolSelectIcon(GameMaster.ToolMode.Dig, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_GatherTool

            var icon_GatherTool = new FlatToolTray.Icon
            {
                Tag = "gather",
                Text= "Pick",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 6),
                Tooltip = "Gather",
                OnClick = (sender, args) => { ChangeTool(GameMaster.ToolMode.Gather); World.Tutorial("gather"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Gather)));
                    AddToolSelectIcon(GameMaster.ToolMode.Gather, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_ChopTool

            var icon_ChopTool = new FlatToolTray.Icon
            {
                Tag = "chop",
                Text = "Chop",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 1),
                Tooltip = "Chop trees",
                OnClick = (sender, args) => { ChangeTool(GameMaster.ToolMode.Chop); World.Tutorial("chop"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Chop)));
                    AddToolSelectIcon(GameMaster.ToolMode.Chop, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_GuardTool

            var icon_GuardTool = new FlatToolTray.Icon
            {
                Tag = "guard",
                Text = "Guard",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 4),
                Tooltip = "Guard",
                OnClick = (sender, args) => { ChangeTool(GameMaster.ToolMode.Guard); World.Tutorial("guard"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Guard)));
                    AddToolSelectIcon(GameMaster.ToolMode.Guard, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_AttackTool

            var icon_AttackTool = new FlatToolTray.Icon
            {
                Tag = "attack",
                Text = "Hunt",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 3),
                Tooltip = "Attack",
                OnClick = (sender, args) => { ChangeTool(GameMaster.ToolMode.Attack); World.Tutorial("attack"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Attack)));
                    AddToolSelectIcon(GameMaster.ToolMode.Attack, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_FarmTool

            var icon_menu_Farm_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
            };

            #region icon_Plant

            #region menu_Plant
            var icon_menu_Plant_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
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
                               Text = TextGenerator.Shorten(resource.ResourceType, 6),
                               TextVerticalAlign = VerticalAlign.Below,
                               OnClick = (sender, args) =>
                               {
                                   World.ShowToolPopup("Click and drag to plant " + resource.ResourceType + ".");
                                   ChangeTool(GameMaster.ToolMode.Plant);
                                   var plantTool = Master.Tools[GameMaster.ToolMode.Plant] as PlantTool;
                                   plantTool.PlantType = resource.ResourceType;
                                   plantTool.RequiredResources = new List<ResourceAmount>()
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
                Icon = new Gui.TileReference("tool-icons", 13),
                Tooltip = "Farm",
                Text = "Farm",
                EnabledTextColor = new Vector4(1, 1, 1, 1),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                KeepChildVisible = true,
                ReplacementMenu = menu_Plant,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };
            #endregion

            #region icon_Wrangle
            var icon_Wrangle = new FlatToolTray.Icon
            {
                Tag = "wrangle",
                Icon = new Gui.TileReference("tool-icons", 32),
                Text = "Catch",
                EnabledTextColor = new Vector4(1, 1, 1, 1),
                Tooltip = "Catch Animals",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                KeepChildVisible = false,
                PopupChild = new Widget()
                {
                    Border = "border-fancy",
                    Text = "Catch Animals.\n Click and drag to catch animals.\nRequires animal pen.",
                    Rect = new Rectangle(0, 0, 256, 128),
                    TextColor = Color.Black.ToVector4()
                },
                OnClick = (sender, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.Wrangle);
                    World.Tutorial("wrangle");
                    World.ShowToolPopup(
                        "Left click to tell dwarves to catch animals.\nRight click to cancel catching.\nRequires animal pen.");
                },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    Master.Faction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Wrangle)));
                    AddToolSelectIcon(GameMaster.ToolMode.Wrangle, sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };
            #endregion

            #endregion

            /*
            #region icon_MagicTool

            ManaBar = GuiRoot.RootItem.AddChild(new ProgressBar()
            {
                AutoLayout = AutoLayout.FloatTopRight,
                Hidden = true,
                MinimumSize = new Point(256, 32),
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Center,
                FillColor = new Color(0, 255, 255, 255).ToVector4(),
                Percentage = 0,
                TextColor = Color.Black.ToVector4()
            }) as ProgressBar;


            #region icon_Cast
            var icon_menu_CastSpells_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
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
#if !DEMO
                                    ChangeTool(GameMaster.ToolMode.Magic);
                                    ((MagicTool)Master.Tools[GameMaster.ToolMode.Magic])
                                        .CurrentSpell =
                                        spell.Spell;
                                    World.Tutorial("cast spells");
#else
                                    GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm() { CancelText = "", Text = "Magic not available in demo." });
#endif
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
                Text = "Cast",
                TextVerticalAlign = VerticalAlign.Below,
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
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
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
                                spell.IsResearched)
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
#if !DEMO
                                    if (Master.Faction.OwnedObjects.Any(obj => obj.Tags.Contains("Magic")))
                                    {
                                        World.ShowToolPopup("Can't research. Need an object which the dwarf can do magical research with.");
                                        return;
                                    }
                                    ChangeTool(GameMaster.ToolMode.Magic);
                                    ((MagicTool)Master.Tools[GameMaster.ToolMode.Magic])
                                        .Research(spell);
                                    World.Tutorial("research spells");
                                    World.MakeAnnouncement(String.Format("Researching {0}", spell.Spell.Name));
#else
                                    GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm() { CancelText = "", Text = "Magic not available in demo." });
#endif
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
                Text = "Research",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 14),
                Tooltip = "Research",
                KeepChildVisible = true,
                ReplacementMenu = menu_ResearchSpells,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };
#endregion

            var icon_menu_Magic_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                }
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
                Text = "Magic",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 14),
                Tooltip = "Magic",
                //OnClick = (sender, args) => ChangeTool(GameMaster.ToolMode.Magic),
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                        Master.Faction.Minions.Any(minion =>
                            minion.Stats.IsTaskAllowed(Task.TaskCategory.Research)));
                    AddToolSelectIcon(GameMaster.ToolMode.Magic, sender);
                },
                KeepChildVisible = true,
                ReplacementMenu = menu_Magic,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion
            */
            #region icon_CancelTasks

            var icon_CancelTasks = new FlatToolTray.Icon()
            {
                Text = "Cancel",
                TextVerticalAlign = VerticalAlign.Below,
                Tooltip = "Cancel voxel tasks such as mining, guarding, and planting.",
                Icon = new TileReference("round-buttons", 5),
                OnClick = (sender, args) =>
                {
                    ChangeTool(GameMaster.ToolMode.CancelTasks);
                    (Master.Tools[GameMaster.ToolMode.CancelTasks] as CancelTasksTool).Options = (sender as FlatToolTray.Icon).PopupChild as CancelToolOptions;
                },
                Behavior = FlatToolTray.IconBehavior.ShowClickPopupAndLeafIcon,
                KeepChildVisible = true, // So the player can interact with the popup.
                ExpandChildWhenDisabled = true,
                TextColor = Color.White.ToVector4(),
                PopupChild = new CancelToolOptions
                {
                    Rect = new Rectangle(0, 0, 200, 100)
                }
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
                    //icon_GuardTool,
                    icon_AttackTool,
                    icon_Plant,
                    icon_Wrangle,
                    icon_Potions,
                    //icon_MagicTool,
                    icon_CancelTasks,
                },
                OnShown = (sender) => ChangeTool(GameMaster.ToolMode.SelectUnits),
                Tag = "tools"
            };

            icon_menu_BuildTools_Return.ReplacementMenu = MainMenu;
            icon_menu_Edibles_Return.ReplacementMenu = MainMenu;
            icon_menu_Farm_Return.ReplacementMenu = MainMenu;
            //icon_menu_Magic_Return.ReplacementMenu = MainMenu;
            icon_menu_Plant_Return.ReplacementMenu = MainMenu;

            BottomToolBar = secondBar.AddChild(new FlatToolTray.RootTray
            {
                AutoLayout = AutoLayout.DockFill,
                ItemSource = new Widget[]
                {
                    menu_BuildTools,
                    //menu_CastSpells,
                    menu_CraftTypes,
                    menu_Edibles,
                    //menu_Magic,
                    MainMenu,
                    menu_Plant,
                    //menu_ResearchSpells,
                    menu_ResourceTypes,
                    menu_RoomTypes,
                    menu_WallTypes,
                    menu_Floortypes,
                    menu_Rail,
                },
                /*OnLayout = (sender) =>
                {
                    sender.Rect = sender.ComputeBoundingChildRect();
                    sender.Rect.X = 208;
                    sender.Rect.Y = bottomBar.Rect.Top - sender.Rect.Height;
                },*/
            }) as FlatToolTray.RootTray;

            BottomToolBar.SwitchTray(MainMenu);
            ChangeTool(GameMaster.ToolMode.SelectUnits);

            #endregion

            #region GOD MODE

            GodMenu = GuiRoot.RootItem.AddChild(new Gui.Widgets.GodMenu
            {
                Master = Master,
                AutoLayout = AutoLayout.FloatTopLeft
            }) as Gui.Widgets.GodMenu;

            GodMenu.Hidden = true;

#endregion

            GuiRoot.RootItem.Layout();

            // Now that it's laid out, bring the second bar to the front so commands draw over other shit.
            secondBar.BringToFront();
            GodMenu.BringToFront();

            Master.BodySelector.LeftReleased += BodySelector_LeftReleased;
            (Master.Tools[GameMaster.ToolMode.SelectUnits] as DwarfSelectorTool).DrawSelectionRect = b => ContextCommands.Any(c => c.CanBeAppliedTo(b, World));
        }

        private List<Body> BodySelector_LeftReleased()
        {
            if (MultiContextMenu != null)
            {
                MultiContextMenu.Close();
                MultiContextMenu = null;
            }

            if (Master.CurrentToolMode != GameMaster.ToolMode.SelectUnits)
                return null;

            Master.SelectedObjects.RemoveAll(b => !ContextCommands.Any(c => c.CanBeAppliedTo(b, World)));
            var bodiesClicked = Master.SelectedObjects;

            if (bodiesClicked.Count > 0)
            {
                var availableCommands = ContextCommands.Where(c => bodiesClicked.Any(b => c.CanBeAppliedTo(b, World)));

                if (availableCommands.Count() > 0)
                {
                    // Show context menu.
                    MultiContextMenu = GuiRoot.ConstructWidget(new HorizontalContextMenu
                    {
                        Commands = availableCommands.ToList(),
                        MultiBody = bodiesClicked,
                        World = World,
                        ClickAction = () =>
                        {
                            BodySelector_LeftReleased();
                        }
                    });

                    MultiContextMenu.Rect = new Rectangle(MinimapFrame.Rect.Right + 2, MinimapFrame.Rect.Bottom - MultiContextMenu.Rect.Height, MultiContextMenu.Rect.Width, MultiContextMenu.Rect.Height);
                    MultiContextMenu.Layout();
                    GuiRoot.ShowDialog(MultiContextMenu);
                    GuiRoot.RootItem.SendToBack(MultiContextMenu);
                }
            }
            else if (MultiContextMenu != null)
            {
                MultiContextMenu.Close();
                MultiContextMenu = null;
            }
            return null;
        }

        /// <summary>
        /// Called when the user releases a key
        /// </summary>
        /// <param name="key">The keyboard key released</param>
        private void TemporaryKeyPressHandler(Keys key)
        {
            /*
            if ((DateTime.Now - EnterTime).TotalSeconds >= EnterInputDelaySeconds)
            {
                InputManager.KeyReleasedCallback -= TemporaryKeyPressHandler;
                InputManager.KeyReleasedCallback += HandleKeyPress;
                HandleKeyPress(key);
            }
            */
        }

        private bool HandleKeyPress(Keys key)
        {
            // Special case: number keys reserved for changing tool mode
            if (FlatToolTray.Tray.Hotkeys.Contains(key))
            {
                if (PausePanel == null || PausePanel.Hidden)
                {
                    (BottomToolBar.Children.First(w => w.Hidden == false) as FlatToolTray.Tray)
                       .Hotkey(key);
                    return true;
                }
            }
            else if (key == Keys.Escape)
            {
                if (PausePanel == null || PausePanel.Hidden)
                {
                    BrushTray.Select(0);
                }

                CameraTray.Select(0);

                if (MainMenu.Hidden && PausePanel == null)
                    (BottomToolBar.Children.First(w => w.Hidden == false) as FlatToolTray.Tray).Hotkey(FlatToolTray.Tray.Hotkeys[0]);
                else if (Master.CurrentToolMode != GameMaster.ToolMode.SelectUnits && PausePanel == null)
                    ChangeTool(GameMaster.ToolMode.SelectUnits);
                else if (PausePanel != null)
                {
                    PausePanel.Close();
                }
                else
                    OpenPauseMenu();
                return true;
            }
            else if (key == ControlSettings.Mappings.SelectAllDwarves)
            {
                if (PausePanel == null || PausePanel.Hidden)
                {
                    Master.SelectedMinions.AddRange(Master.Faction.Minions);
                    World.Tutorial("dwarf selected");
                    return true;
                }
            }
            else if (key == ControlSettings.Mappings.Pause)
            {
                if (PausePanel == null || PausePanel.Hidden)
                {
                    Paused = !Paused;
                    if (Paused) GameSpeedControls.Pause();
                    else  GameSpeedControls.Resume();
                    return true;
                }
            }
            else if (key == ControlSettings.Mappings.TimeForward)
            {
                if (PausePanel == null || PausePanel.Hidden)
                {
                    GameSpeedControls.CurrentSpeed += 1;
                    return true;
                }
            }
            else if (key == ControlSettings.Mappings.TimeBackward)
            {
                if (PausePanel == null || PausePanel.Hidden)
                {
                    GameSpeedControls.CurrentSpeed -= 1;
                    return true;
                }
            }
            else if (key == ControlSettings.Mappings.ToggleGUI)
            {
                GuiRoot.RootItem.Hidden = !GuiRoot.RootItem.Hidden;
                GuiRoot.RootItem.Invalidate();
                return true;
            }
            else if (key == ControlSettings.Mappings.Map)
            {
                if (PausePanel == null || PausePanel.Hidden)
                {
                    GuiRoot.SafeCall(MinimapIcon.OnClick, MinimapIcon, new InputEventArgs
                    {
                    });
                    return true;
                }
            }
#if !DEMO
            else if (key == ControlSettings.Mappings.GodMode)
            {
                if (PausePanel == null || PausePanel.Hidden)
                {
                    if (!GodMenu.Hidden)
                    {
                        ChangeTool(GameMaster.ToolMode.SelectUnits);
                    }
                    GodMenu.Hidden = !GodMenu.Hidden;
                    GodMenu.Invalidate();
                    return true;
                }
            }
#endif
            return false;
        }

        private void MakeMenuItem(Gui.Widget Menu, string Name, string Tooltip, Action<Gui.Widget, Gui.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                Border = "border-thin",
                Font = "font16",
                Text = Name,
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                HoverTextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4(),
                ChangeColorOnHover = true
            });
        }

        public void OpenPauseMenu()
        {
            if (PausePanel != null) return;
            bool pausedRightNow = Paused;
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
                    if (!pausedRightNow)
                    {
                        GameSpeedControls.Resume();
                    }
                    Paused = pausedRightNow;
                },
                Font = "font16"
            };

            GuiRoot.ConstructWidget(PausePanel);

            MakeMenuItem(PausePanel, "Continue", "", (sender, args) =>
            {
                PausePanel.Close();
                if (!pausedRightNow)
                {
                    GameSpeedControls.Resume();
                }
                else
                {
                    GameSpeedControls.Pause();
                }
                Paused = pausedRightNow;
                PausedWidget.Hidden = !Paused;
                PausedWidget.Invalidate();
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

#if !DEMO
            MakeMenuItem(PausePanel, "Save", "",
                (sender, args) =>
                {
                    World.Save(
                        String.Format("{0}_{1}_{2}", Overworld.Name, World.GameID, DateTime.Now.ToFileTimeUtc()),
                        (success, exception) =>
                        {
                            GuiRoot.ShowModalPopup(new Gui.Widgets.Popup
                            {
                                Text = success ? "File saved." : "Save failed - " + exception.Message,
                                OnClose = (s2) => OpenPauseMenu()
                            });
                        });
                });
#endif

            MakeMenuItem(PausePanel, "Quit", "", (sender, args) => QuitOnNextUpdate = true);

            PausePanel.Layout();

            GuiRoot.ShowModalPopup(PausePanel);
        }

        public void Destroy()
        {
            Input.Destroy();
        }

        public void QuitGame()
        {
            QuitGame(new MainMenuState(Game, StateManager));
        }

        public void QuitGame(GameState state)
        {
            World.Quit();
            StateManager.ClearState();
            Destroy();

            StateManager.PushState(state);
        }

        public void AutoSave()
        {
#if !DEMO
            bool paused = World.Paused;
            World.Save(
                    String.Format("{0}_{1}_{2}", Overworld.Name, World.GameID, "Autosave"),
                    (success, exception) =>
                    {
                        World.MakeAnnouncement(success ? "File autosaved." : "Autosave failed - " + exception.Message);
                        World.Paused = paused;
                    });
#endif
        }
    }
}
