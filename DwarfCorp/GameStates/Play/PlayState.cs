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
        public WorldRenderer Renderer;

        public VoxelSelector VoxSelector;
        public BodySelector BodySelector;
        private List<GameComponent> SelectedObjects = new List<GameComponent>();

        public bool Paused // Todo: Kill
        {
            get { return World.Paused; }
            set { World.Paused = value; }
        }

        private List<ContextCommands.ContextCommand> ContextCommands = new List<ContextCommands.ContextCommand>();

        private bool sliceDownheld = false;
        private bool sliceUpheld = false;
        private Timer sliceDownTimer = new Timer(0.5f, true, Timer.TimerMode.Real);
        private Timer sliceUpTimer = new Timer(0.5f, true, Timer.TimerMode.Real);
        private int rememberedViewValue = 0;

        private Gui.Widget MoneyLabel;
        private Gui.Widget LevelLabel;
        private Widget SupervisionLabel;
        private Gui.Widget StocksLabel;
        private Gui.Widgets.FlatToolTray.RootTray BottomToolBar;
        private Gui.Widgets.FlatToolTray.Tray MainMenu;
        private Gui.Widget TimeLabel;
        private Gui.Widget PausePanel;
        private Gui.Widgets.Minimap.MinimapFrame MinimapFrame;
        private Gui.Widgets.Minimap.MinimapRenderer MinimapRenderer;
        private Gui.Widgets.GameSpeedControls GameSpeedControls;
        private Widget PausedWidget;
        private Gui.Widgets.InfoTray InfoTray;
        private Gui.Widgets.ToggleTray BrushTray;
        private Gui.Widgets.ToggleTray CameraTray;
        private Gui.Widgets.CheckBox Xray;
        private Gui.Widgets.GodMenu GodMenu;
        private AnnouncementPopup Announcer;
        private FramedIcon EconomyIcon;
        private Timer AutoSaveTimer;
        private EmployeeInfo SelectedEmployeeInfo;
        private Widget ContextMenu;
        private Widget MultiContextMenu;
        private Widget BottomBar;
        private Widget BottomBackground;
        private Widget MinimapIcon;
        public Dictionary<String, PlayerTool> Tools;
        public PlayerTool CurrentTool { get { return Tools[CurrentToolMode]; } }
        public String CurrentToolMode = "SelectUnits";
        private Dictionary<uint, WorldPopup> LastWorldPopup = new Dictionary<uint, WorldPopup>();


        private class ToolbarItem
        {
            public Gui.Widgets.FramedIcon Icon;
            public Func<bool> Available;

            public ToolbarItem(Gui.Widget Icon, Func<bool> Available)
            {
                global::System.Diagnostics.Debug.Assert(Icon is Gui.Widgets.FramedIcon);
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
        private Dictionary<String, Gui.Widgets.FramedIcon> ToolHiliteItems = new Dictionary<String, Gui.Widgets.FramedIcon>();

        private void AddToolSelectIcon(String Mode, Gui.Widget Icon)
        {
            if (!ToolHiliteItems.ContainsKey(Mode))
                ToolHiliteItems.Add(Mode, Icon as Gui.Widgets.FramedIcon);
        }

        public void ChangeTool(String Mode)
        {
            if (MultiContextMenu != null)
            {
                MultiContextMenu.Close();
                MultiContextMenu = null;
            }

            if (Mode != "SelectUnits")
                SelectedObjects = new List<GameComponent>();

            // Todo: Should probably clean up existing tool even if they are the same tool.
            Tools[Mode].OnBegin();
            if (CurrentToolMode != Mode)
                CurrentTool.OnEnd();
            CurrentToolMode = Mode;

            foreach (var icon in ToolHiliteItems)
                icon.Value.Hilite = icon.Key == Mode;
        }

        // Provides event-based keyboard and mouse input.
        public static InputManager Input;// = new InputManager();

        public Gui.Root Gui;

        public PlayState(DwarfGame game, WorldManager World) :
            base(game)
        {
            this.World = World;
            IsShuttingDown = false;
            QuitOnNextUpdate = false;
            ShouldReset = true;
            Paused = false;
            RenderUnderneath = true;
            EnableScreensaver = false;
            IsInitialized = false;

            Renderer = World.Renderer; // Todo: Kill

            rememberedViewValue = World.WorldSizeInVoxels.Y;

            VoxSelector = new VoxelSelector(World);
            BodySelector = new BodySelector(World.Renderer.Camera, GameState.Game.GraphicsDevice, World.ComponentManager);
        }

        #region UserInterface Callbacks

        public Gui.MousePointer MousePointer = new Gui.MousePointer("mouse", 1, 0);

        public void ShowTooltip(String Text)
        {
            Gui.ShowTooltip(Gui.MousePosition, Text);
        }

        public void ShowInfo(String Text)
        {
            InfoTray.AddMessage(Text);
        }

        public void ShowToolPopup(String Text)
        {
            if (String.IsNullOrEmpty(Text))
            {
                if (Gui.TooltipItem != null)
                {
                    Gui.DestroyWidget(Gui.TooltipItem);
                }
                Gui.TooltipItem = null;
            }
            else
            {
                Gui.RootItem.AddChild(
                  new Gui.Widgets.ToolPopup
                  {
                      Text = Text,
                      Rect = new Rectangle(Gui.MousePosition.X - 16, Gui.MousePosition.Y - 16, 128, 64)
                  });
            }
        }

        public void SetMouse(MousePointer Mouse)
        {
            Gui.MousePointer = Mouse;
        }

        public void SetMouseOverlay(String Mouse, int Frame)
        {
            Gui.MouseOverlaySheet = new TileReference(Mouse, Frame);
        }

        public bool IsMouseOverGui
        {
            get
            {
                return Gui.HoverItem != null;
                // Don't detect tooltips and tool popups.
            }
        }

        public WorldPopup MakeWorldPopup(string text, GameComponent body, float screenOffset = -10, float time = 30.0f)
        {
            return MakeWorldPopup(new Events.TimedIndicatorWidget() { Text = text, DeathTimer = new Timer(time, true, Timer.TimerMode.Real) }, body, new Vector2(0, screenOffset));
        }

        public WorldPopup MakeWorldPopup(Widget widget, GameComponent body, Vector2 ScreenOffset)
        {
            if (LastWorldPopup.ContainsKey(body.GlobalID))
                Gui.DestroyWidget(LastWorldPopup[body.GlobalID].Widget);

            Gui.RootItem.AddChild(widget);

            // Todo: Uh - what cleans these up if the body is destroyed?
            LastWorldPopup[body.GlobalID] = new WorldPopup()
            {
                Widget = widget,
                BodyToTrack = body,
                ScreenOffset = ScreenOffset
            };

            Gui.RootItem.SendToBack(widget);

            return LastWorldPopup[body.GlobalID];
        }

        public bool IsCameraRotationModeActive()
        {
            return KeyManager.RotationEnabled(World.Renderer.Camera);
        }

        #endregion

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

                // Setup tool list.
                Tools = new Dictionary<String, PlayerTool>();

                foreach (var method in AssetManager.EnumerateModHooks(typeof(ToolFactoryAttribute), typeof(PlayerTool), new Type[] { typeof(WorldManager) }))
                {
                    var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is ToolFactoryAttribute) as ToolFactoryAttribute;
                    if (attribute == null) continue;
                    Tools[attribute.Name] = method.Invoke(null, new Object[] { World }) as PlayerTool;
                }

                VoxSelector.Selected += (voxels, button) => CurrentTool.OnVoxelsSelected(voxels, button);
                VoxSelector.Dragged += (voxels, button) => CurrentTool.OnVoxelsDragged(voxels, button);
                BodySelector.Selected += (bodies, button) =>
                {
                    CurrentTool.OnBodiesSelected(bodies, button);
                    if (CurrentToolMode == "SelectUnits")
                        SelectedObjects = bodies;
                };
                BodySelector.MouseOver += (bodies) => CurrentTool.OnMouseOver(bodies);

                // Ensure game is not paused.
                Paused = false;
                DwarfTime.LastTime.Speed = 1.0f;

                // Setup new gui. Double rendering the mouse?
                Gui = new Gui.Root(DwarfGame.GuiSkin);
                Gui.MousePointer = new Gui.MousePointer("mouse", 4, 0);

                World.UserInterface = this;
                CreateGUIComponents();
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

                foreach (var contextCommandFactory in AssetManager.EnumerateModHooks(typeof(ContextCommandAttribute), typeof(ContextCommands.ContextCommand), new Type[] { }))
                    ContextCommands.Add(contextCommandFactory.Invoke(null, new Object[] { }) as ContextCommands.ContextCommand);

                World.LogEvent(String.Format("We have arrived at {0}", World.Overworld.Name));
            }

            base.OnEnter();
        }

        /// <summary>
        /// Called when the PlayState is exited and another state (such as the main menu) is loaded.
        /// </summary>
        public override void OnCovered()
        {
            World.PauseThreads();
            base.OnCovered();
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

            #region Input for GUI

            DwarfGame.GumInput.FireActions(Gui, (@event, args) =>
            {
                // Let old input handle mouse interaction for now. Will eventually need to be replaced.

                // Mouse down but not handled by GUI? Collapse menu.
                if (@event == DwarfCorp.Gui.InputEvents.MouseClick)
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
                            World.Renderer.Camera.ZoomTo(World.Renderer.CursorLightPos);
                            Renderer.Camera.ZoomTo(Renderer.CursorLightPos);
                        }
                        timeOnLastClick = now;
                    }

                    if (args.MouseButton == 1) // Right mouse click.
                    {
                        var bodiesClicked = World.ComponentManager.SelectRootBodiesOnScreen(
                            new Rectangle(args.X, args.Y, 1, 1), Renderer.Camera);

                        if (bodiesClicked.Count > 0)
                        {
                            var contextBody = bodiesClicked[0];
                            var availableCommands = ContextCommands.Where(c => c.CanBeAppliedTo(contextBody, World));

                            if (availableCommands.Count() > 0)
                            {
                                // Show context menu.
                                ContextMenu = Gui.ConstructWidget(new ContextMenu
                                {
                                    Commands = availableCommands.ToList(),
                                    Body = contextBody,
                                    World = World
                                });

                                Gui.ShowDialog(ContextMenu);
                                args.Handled = true;
                            }
                        }
                    }
                }

                else if (@event == DwarfCorp.Gui.InputEvents.KeyUp)
                {
                    if (FlatToolTray.Tray.Hotkeys.Contains((Keys)args.KeyValue))
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                        {
                            (BottomToolBar.Children.First(w => w.Hidden == false) as FlatToolTray.Tray)
                               .Hotkey((Keys)args.KeyValue);
                        }
                    }
                    else if ((Keys)args.KeyValue == Keys.Escape)
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                        {
                            BrushTray.Select(0);
                            World.TutorialManager.HideTutorial();
                        }
                        else
                        {
                            World.TutorialManager.ShowTutorial();
                        }

                        CameraTray.Select(0);


                        if (MainMenu.Hidden && PausePanel == null)
                            (BottomToolBar.Children.First(w => w.Hidden == false) as FlatToolTray.Tray).Hotkey(FlatToolTray.Tray.Hotkeys[0]);
                        else if (CurrentToolMode != "SelectUnits" && PausePanel == null)
                            ChangeTool("SelectUnits");
                        else if (PausePanel != null)
                        {
                            PausePanel.Close();
                        }
                        else
                            OpenPauseMenu();
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.SelectAllDwarves)
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                        {
                            World.PersistentData.SelectedMinions.AddRange(World.PlayerFaction.Minions);
                            World.Tutorial("dwarf selected");
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Pause)
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                        {
                            Paused = !Paused;
                            if (Paused) GameSpeedControls.Pause();
                            else GameSpeedControls.Resume();
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.TimeForward)
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                        {
                            GameSpeedControls.CurrentSpeed += 1;
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.TimeBackward)
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                        {
                            GameSpeedControls.CurrentSpeed -= 1;
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.ToggleGUI)
                    {
                        Gui.RootItem.Hidden = !Gui.RootItem.Hidden;
                        Gui.RootItem.Invalidate();
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Map)
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                        {
                            Gui.SafeCall(MinimapIcon.OnClick, MinimapIcon, new InputEventArgs
                            {
                            });
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Xray)
                    {
                        Xray.CheckState = !Xray.CheckState;
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.GodMode)
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                        {
                            if (!GodMenu.Hidden)
                            {
                                ChangeTool("SelectUnits");
                            }
                            GodMenu.Hidden = !GodMenu.Hidden;
                            GodMenu.Invalidate();
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.SliceUp)
                    {
                        sliceUpheld = false;
                        args.Handled = true;
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.SliceDown)
                    {
                        sliceDownheld = false;
                        args.Handled = true;
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.SliceSelected)
                    {
                        if (args.Control)
                        {
                            World.Renderer.SetMaxViewingLevel(rememberedViewValue);
                            args.Handled = true;
                        }
                        else if (VoxSelector.VoxelUnderMouse.IsValid)
                        {
                            World.Tutorial("unslice");
                            World.Renderer.SetMaxViewingLevel(VoxSelector.VoxelUnderMouse.Coordinate.Y + 1);
                            Drawer3D.DrawBox(VoxSelector.VoxelUnderMouse.GetBoundingBox(), Color.White, 0.15f, true);
                            args.Handled = true;
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Unslice)
                    {
                        rememberedViewValue = World.Renderer.PersistentSettings.MaxViewingLevel;
                        World.Renderer.SetMaxViewingLevel(World.WorldSizeInVoxels.Y);
                        args.Handled = true;
                    }
                }
                else if (@event == DwarfCorp.Gui.InputEvents.KeyDown)
                {
                    if ((Keys)args.KeyValue == ControlSettings.Mappings.SliceUp)
                    {
                        if (!sliceUpheld)
                        {
                            sliceUpheld = true;
                            World.Tutorial("unslice");
                            sliceUpTimer.Reset(0.5f);
                            World.Renderer.SetMaxViewingLevel(World.Renderer.PersistentSettings.MaxViewingLevel + 1);
                            args.Handled = true;
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.SliceDown)
                    {
                        if (!sliceDownheld)
                        {
                            World.Tutorial("unslice");
                            sliceDownheld = true;
                            sliceDownTimer.Reset(0.5f);
                            World.Renderer.SetMaxViewingLevel(World.Renderer.PersistentSettings.MaxViewingLevel - 1);
                            args.Handled = true;
                        }
                    }
                }
            });

            #endregion

            #region Slice hotkeys

            if (sliceDownheld)
            {
                sliceDownTimer.Update(gameTime);
                if (sliceDownTimer.HasTriggered)
                {
                    World.Renderer.SetMaxViewingLevel(World.Renderer.PersistentSettings.MaxViewingLevel - 1);
                    sliceDownTimer.Reset(sliceDownTimer.TargetTimeSeconds * 0.6f);
                }
            }
            else if (sliceUpheld)
            {
                sliceUpTimer.Update(gameTime);
                if (sliceUpTimer.HasTriggered)
                {
                    World.Renderer.SetMaxViewingLevel(World.Renderer.PersistentSettings.MaxViewingLevel + 1);
                    sliceUpTimer.Reset(sliceUpTimer.TargetTimeSeconds * 0.6f);
                }
            }

            #endregion

            World.Update(gameTime);

            #region Vox and Body selectors
            if (!IsMouseOverGui)
            {
                if (KeyManager.RotationEnabled(World.Renderer.Camera))
                    SetMouse(null);
                VoxSelector.Update();
                BodySelector.Update();
            }
            #endregion

            Renderer.Update(gameTime);
            Input.Update();
            CurrentTool.Update(Game, gameTime);

            #region World Popups

            if (LastWorldPopup != null)
            {
                var removals = new List<uint>();
                foreach (var popup in LastWorldPopup)
                {
                    popup.Value.Update(gameTime, Renderer.Camera, Game.GraphicsDevice.Viewport);
                    if (popup.Value.Widget == null || !Gui.RootItem.Children.Contains(popup.Value.Widget) || popup.Value.BodyToTrack == null || popup.Value.BodyToTrack.IsDead)
                        removals.Add(popup.Key);
                }

                foreach (var removal in removals)
                {
                    if (LastWorldPopup[removal].Widget != null && Gui.RootItem.Children.Contains(LastWorldPopup[removal].Widget))
                        Gui.DestroyWidget(LastWorldPopup[removal].Widget);
                    LastWorldPopup.Remove(removal);
                }
            }

            #endregion

            #region Update time label
            TimeLabel.Text = String.Format("{0} {1}",
                World.Time.CurrentDate.ToShortDateString(),
                World.Time.CurrentDate.ToShortTimeString());
            TimeLabel.Invalidate();
            #endregion


            #region Update top left panel
            var pulse = 0.25f * (float)Math.Sin(gameTime.TotalRealTime.TotalSeconds * 4) + 0.25f;
            MoneyLabel.Text = World.PlayerFaction.Economy.Funds.ToString();
            MoneyLabel.TextColor = World.PlayerFaction.Economy.Funds > 1.0m ? Color.White.ToVector4() : new Vector4(1.0f, pulse, pulse, 1.0f);
            MoneyLabel.Invalidate();
            int availableSpace = World.ComputeRemainingStockpileSpace();
            int totalSpace = World.ComputeTotalStockpileSpace();
            StocksLabel.Text = String.Format("    Stocks: {0}/{1}", totalSpace - availableSpace, totalSpace);
            StocksLabel.TextColor = availableSpace > 0 ? Color.White.ToVector4() : new Vector4(1.0f, pulse, pulse, 1.0f);
            StocksLabel.Invalidate();
            LevelLabel.Text = String.Format("{0}/{1}", Renderer.PersistentSettings.MaxViewingLevel, World.WorldSizeInVoxels.Y);
            LevelLabel.Invalidate();
            SupervisionLabel.Text = String.Format("{0}/{1}", World.PlayerFaction.Minions.Count, World.CalculateSupervisionCap());
            SupervisionLabel.Invalidate();
            #endregion

            #region Update toolbar tray

            foreach (var tool in ToolbarItems)
                tool.Icon.Enabled = tool.Available();

            #endregion

            BottomBar.Layout();

            if (GameSpeedControls.CurrentSpeed != (int)DwarfTime.LastTime.Speed)
                World.Tutorial("time");

            GameSpeedControls.CurrentSpeed = (int)DwarfTime.LastTime.Speed;

            if (PausedWidget.Hidden == Paused)
            {
                PausedWidget.Hidden = !Paused;
                PausedWidget.Invalidate();
            }

            // Really just handles mouse pointer animation.
            Gui.Update(gameTime.ToRealTime());

            AutoSaveTimer.Update(gameTime);

            if (GameSettings.Default.AutoSave && AutoSaveTimer.HasTriggered)
            {
                AutoSave();
            }

            #region select employee

            World.PersistentData.SelectedMinions.RemoveAll(minion => minion.IsDead);
            if (World.PersistentData.SelectedMinions.Count == 1)
            {
                // Lol this is evil just trying to reduce the update rate for speed
                if (MathFunctions.RandEvent(0.1f))
                    SelectedEmployeeInfo.Employee = World.PersistentData.SelectedMinions[0];
            }
            else
            {
                bool update = MathFunctions.RandEvent(0.1f);
                if ((SelectedEmployeeInfo.Employee == null || SelectedEmployeeInfo.Employee.IsDead || !SelectedEmployeeInfo.Employee.Active) &&
                    World.PlayerFaction.Minions.Count > 0)
                {
                    SelectedEmployeeInfo.Employee = World.PlayerFaction.Minions[0];
                }
                else if (update)
                {
                    SelectedEmployeeInfo.Employee = SelectedEmployeeInfo.Employee;
                }
            }
            #endregion

            #region Console
            if (DwarfGame.IsConsoleVisible)
            {
                PerformanceMonitor.SetMetric("MEMORY", BytesToString(System.GC.GetTotalMemory(false)));

                var statsDisplay = DwarfGame.GetConsoleTile("STATS");

                statsDisplay.Lines.Clear();
                statsDisplay.Lines.Add("** STATISTICS **");
                foreach (var metric in PerformanceMonitor.EnumerateMetrics())
                    statsDisplay.Lines.Add(String.Format("{0} {1}", metric.Value.ToString(), metric.Key));
                statsDisplay.Invalidate();

                // Todo: Employee AI debug display

                var scheduleDisplay = DwarfGame.GetConsoleTile("SCHEDULE");
                scheduleDisplay.TextSize = 1;
                scheduleDisplay.Lines.Clear();
                foreach (var scheduledEvent in World.EventScheduler.Forecast)
                    scheduleDisplay.Lines.Add(String.Format("{0} : {1}, {2}", scheduledEvent.Event.Name, (scheduledEvent.Date - World.Time.CurrentDate).ToString(@"hh\:mm"), scheduledEvent.Event.Difficulty));

                scheduleDisplay.Lines.Add(String.Format("Difficulty: {0} Forecast {1}", World.EventScheduler.CurrentDifficulty, World.EventScheduler.ForecastDifficulty(World.Time.CurrentDate)));

                scheduleDisplay.Invalidate();
            }
            #endregion
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
                Renderer.ValidateShader();

                if (!MinimapFrame.Hidden && !Gui.RootItem.Hidden)
                    MinimapRenderer.PreRender(DwarfGame.SpriteBatch);

                Renderer.Render(gameTime);

                CurrentTool.Render3D(Game, gameTime);
                VoxSelector.Render();

                foreach (var obj in SelectedObjects)
                    if (obj.IsVisible && !obj.IsDead)
                        Drawer3D.DrawBox(obj.GetBoundingBox(), Color.White, 0.01f, true);

                CurrentTool.Render2D(Game, gameTime);

                foreach (CreatureAI creature in World.PersistentData.SelectedMinions)
                {
                    foreach (Task task in creature.Tasks)
                        if (task.IsFeasible(creature.Creature) == Task.Feasibility.Feasible)
                            task.Render(gameTime);

                    if (creature.CurrentTask != null)
                        creature.CurrentTask.Render(gameTime);
                }

                DwarfGame.SpriteBatch.Begin();
                BodySelector.Render(DwarfGame.SpriteBatch);
                DwarfGame.SpriteBatch.End();

                if (Gui.RenderData.RealScreen.Width != Gui.RenderData.Device.Viewport.Width || Gui.RenderData.RealScreen.Height != Gui.RenderData.Device.Viewport.Height)
                {
                    Gui.RenderData.CalculateScreenSize();
                    Gui.RootItem.Rect = Gui.RenderData.VirtualScreen;
                    Gui.ResetGui();
                    CreateGUIComponents();
                }

                if (!MinimapFrame.Hidden && !Gui.RootItem.Hidden)
                {
                    Gui.Draw(new Point(0, 0), false);
                    MinimapRenderer.Render(new Rectangle(MinimapFrame.Rect.X, MinimapFrame.Rect.Bottom - 192, 192, 192), Gui);
                    Gui.DrawMesh(MinimapFrame.GetRenderMesh(), Gui.RenderData.Texture);
                    Gui.RedrawPopups();
                    Gui.DrawMouse();
                }
                else
                    Gui.Draw();
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
            Renderer.Render(gameTime);
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

            var resourceCount = World.ListResourcesInStockpilesPlusMinions()
                .Where(r => data.CanBuildWith(ResourceLibrary.GetResourceByName(r.Key))).Sum(r => r.Value.First.Count + r.Value.Second.Count);

            int newNum = Math.Max(resourceCount -
                World.PersistentData.Designations.EnumerateDesignations(DesignationType.Put).Count(d =>
                    BuildRequirementsEqual(Library.GetVoxelType(d.Tag.ToString()), data)), 0);

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
            DwarfCorp.Gui.Widgets.FlatToolTray.Tray.DetectHotKeys();

            BottomBackground = Gui.RootItem.AddChild(new TrayBackground
            {
                Corners = Scale9Corners.Top,
                MinimumSize = new Point(0, 112),
                AutoLayout = AutoLayout.DockBottom
            });

            BottomBar = BottomBackground.AddChild(new Gui.Widget
            {
                Transparent = false,
                Background = new TileReference("basic", 0),
                BackgroundColor = new Vector4(0, 0, 0, 0.5f),
                Padding = new Margin(0, 0, 2, 2),
                MinimumSize = new Point(0, 36),
                AutoLayout = AutoLayout.DockBottom
            });

            var secondBar = BottomBackground.AddChild(new Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 64),
                AutoLayout = AutoLayout.DockBottom,
                InteriorMargin = new Margin(2, 0, 0, 0),
                Padding = new Margin(0, 0, 2, 2)
            });

            #region Setup company information section
            BottomBar.AddChild(new CompanyLogo
            {
                Tag = "company info",
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeftCentered,
                CompanyInformation = World.PlayerFaction.Economy.Information,
                Tooltip = World.PlayerFaction.Economy.Information.Name
            });

            BottomBar.AddChild(new Gui.Widget
            {
                Background = new Gui.TileReference("resources", 40),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeftCentered
            });

            MoneyLabel = BottomBar.AddChild(new Gui.Widget
            {
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeftCentered,
                Font = "font10",
                TextVerticalAlign = DwarfCorp.Gui.VerticalAlign.Center,
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = Library.GetString("money-amount")
            });

            StocksLabel = BottomBar.AddChild(new Gui.Widget
            {
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeftCentered,
                Font = "font10",
                TextVerticalAlign = DwarfCorp.Gui.VerticalAlign.Center,
                Tooltip = Library.GetString("stockpile-tooltip")
            });

            BottomBar.AddChild(new Gui.Widget
            {
                Background = new Gui.TileReference("resources", 42),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeftCentered,
                Tooltip = Library.GetString("slicer-tooltip")
            });

            BottomBar.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 7),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeftCentered,
                OnClick = (sender, args) =>
                {
                    Renderer.SetMaxViewingLevel(Renderer.PersistentSettings.MaxViewingLevel - 1);
                },
                Tooltip = Library.GetString("slicer-down-tooltip")
            });

            BottomBar.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 3),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeftCentered,
                OnClick = (sender, args) =>
                {
                    Renderer.SetMaxViewingLevel(Renderer.PersistentSettings.MaxViewingLevel + 1);
                },
                Tooltip = Library.GetString("slicer-up-tooltip")
            });

            LevelLabel = BottomBar.AddChild(new Gui.Widget
            {
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeftCentered,
                Font = "font10",
                TextVerticalAlign = DwarfCorp.Gui.VerticalAlign.Center,
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = Library.GetString("slicer-current-tooltip")
            });

            BottomBar.AddChild(new Gui.Widget
            {
                Background = new Gui.TileReference("dwarves", 0),
                MinimumSize = new Point(24, 32),
                MaximumSize = new Point(24, 32),
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeftCentered,
                Tooltip = "Dwarves vs Available Supervision"
            });

            SupervisionLabel = BottomBar.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockLeftCentered,
                Font = "font10",
                TextVerticalAlign = VerticalAlign.Center,
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = "You need supervisors to manage more dwarves."
            });
            #endregion

            Gui.RootItem.AddChild(new Gui.Widgets.ResourcePanel
            {
                AutoLayout = AutoLayout.FloatTop,
                World = World,
                Transparent = true,
            });

            #region Setup time display
            TimeLabel = BottomBar.AddChild(new Gui.Widget
            {
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockRightCentered,
                TextHorizontalAlign = DwarfCorp.Gui.HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                MinimumSize = new Point(128, 20),
                Font = "font10",
                TextColor = new Vector4(1, 1, 1, 1),
                Tooltip = Library.GetString("time-tooltip")
            });
            #endregion

            #region Toggle panel buttons

            MinimapRenderer = new Gui.Widgets.Minimap.MinimapRenderer(192, 192, World);

            MinimapFrame = Gui.RootItem.AddChild(new Gui.Widgets.Minimap.MinimapFrame
            {
                Tag = "minimap",
                Renderer = MinimapRenderer,
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(208, 204),
                OnLayout = (sender) => sender.Rect.Y += 4
            }) as Gui.Widgets.Minimap.MinimapFrame;

            SelectedEmployeeInfo = Gui.RootItem.AddChild(new EmployeeInfo
            {
                Hidden = true,
                Border = "border-fancy",
                Employee = null,
                EnablePosession = true,
                Tag = "selected-employee-info",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(450, 500 - (50 * (GameSettings.Default.GuiScale - 1))),
            }) as EmployeeInfo;

            var markerFilter = Gui.RootItem.AddChild(new DesignationFilter
            {
                DesignationDrawer = Renderer.DesignationDrawer,
                DesignationSet = World.PersistentData.Designations,
                World = World,
                Hidden = true,
                Border = "border-fancy",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(300, 200)
            });

            var taskList = Gui.RootItem.AddChild(new TaskListPanel
            {
                Border = "border-thin",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(600, 300),
                Hidden = true,
                World = this.World
            });

            var roomList = Gui.RootItem.AddChild(new RoomListPanel
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
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeft,
                SizeToGrid = new Point(5, 1),
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
                                       roomList.Hidden = true;
                                   }
                                   else
                                       SelectedEmployeeInfo.Hidden = true;
                               }
                            },

                            new FramedIcon
                            {
                               Icon  = new Gui.TileReference("tool-icons", 17),
                               Text = "@play-marks-icon-label",
                               Tooltip = "@play-marks-icon-tooltip",
                               TextHorizontalAlign = HorizontalAlign.Center,
                               TextVerticalAlign = VerticalAlign.Below,
                               EnabledTextColor = Vector4.One,
                               OnClick = (sender, args) =>
                               {
                                   if (markerFilter.Hidden)
                                   {
                                       MinimapFrame.Hidden = true;
                                       SelectedEmployeeInfo.Hidden = true;
                                       taskList.Hidden = true;
                                       markerFilter.Hidden = false;
                                       roomList.Hidden = true;
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
                                        roomList.Hidden = true;
                                    }
                                    else
                                        taskList.Hidden = true;
                                }
                            },

                            new FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 37),
                                Text = "@play-rooms-icon-label",
                                Tooltip = "@play-rooms-icon-tooltip",
                                TextHorizontalAlign = HorizontalAlign.Center,
                                TextVerticalAlign = VerticalAlign.Below,
                                EnabledTextColor = Vector4.One,
                                OnClick = (sender, args) =>
                                {
                                    if (roomList.Hidden)
                                    {
                                        MinimapFrame.Hidden = true;
                                        SelectedEmployeeInfo.Hidden = true;
                                        markerFilter.Hidden = true;
                                        taskList.Hidden = true;
                                        roomList.Hidden = false;
                                    }
                                    else
                                        roomList.Hidden = true;
                                }
                            }
                        },
            });

            secondBar.AddChild(new Widget
            {
                Transparent = true,
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(8, 0)
            });

            #endregion

            #region Setup right tray

            EconomyIcon = new Gui.Widgets.FramedIcon
            {
                Tag = "economy",
                Icon = new Gui.TileReference("tool-icons", 10),
                OnClick = (sender, args) => GameStateManager.PushState(new EconomyState(Game, World)),
                DrawIndicator = true,
                Tooltip = Library.GetString("economy-tooltip"),
                Text = Library.GetString("economy-label"),
                TextVerticalAlign = VerticalAlign.Below
            };

            var topRightTray = secondBar.AddChild(new Gui.Widgets.IconTray
            {
                Corners = 0,//Gui.Scale9Corners.Top,
                Transparent = true,
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockRight,
                SizeToGrid = new Point(4, 1),
                ItemSource = new Gui.Widget[]
                        {
                            new Gui.Widgets.FramedIcon()
                            {
                                 Icon = new Gui.TileReference("tool-icons", 21),
                                OnClick = (sender, args) =>
                                {
                                    GameStateManager.PushState(new EventLogState(Game, World.EventLog, World.Time.CurrentDate));
                                },
                                Text = Library.GetString("events-label"),
                                TextVerticalAlign = VerticalAlign.Below,
                                Tooltip = Library.GetString("events-tooltip")
                            },
                            new Gui.Widgets.FramedIcon()
                            {
                                 Icon = new Gui.TileReference("tool-icons", 36),
                                OnClick = (sender, args) =>
                                {
                                    GameStateManager.PushState(new PlayFactionViewState(GameState.Game, World));
                                },
                                Text =  Library.GetString("diplomacy-label"),
                                TextVerticalAlign = VerticalAlign.Below,
                                Tooltip = Library.GetString("diplomacy-tooltip")
                            },
                            EconomyIcon,

                            new Gui.Widgets.FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 12),
                                OnClick = (sender, args) => { OpenPauseMenu(); },
                                Tooltip = Library.GetString("settings-tooltip"),
                                Text = Library.GetString("settings-label"),
                                TextVerticalAlign = VerticalAlign.Below
                            }
                        },
            });


            secondBar.AddChild(new Widget
            {
                Transparent = true,
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(8, 0)
            });

            #endregion

            #region Setup game speed controls

            GameSpeedControls = BottomBar.AddChild(new GameSpeedControls
            {
                Tag = "speed controls",
                AutoLayout = AutoLayout.DockRightCentered,

                OnSpeedChanged = (sender, speed) =>
                {
                    if ((int)DwarfTime.LastTime.Speed != speed)
                    {
                        World.Tutorial("time");
                        if ((int)DwarfTime.LastTime.Speed == 0)
                        {
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_unpause, 0.1f);
                        }
                        switch (speed)
                        {
                            case 1:
                                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_1x, 0.1f);
                                break;
                            case 2:
                                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_2x, 0.1f);
                                break;
                            case 3:
                                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_3x, 0.1f);
                                break;
                            case 0:
                                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_speed_pause, 0.1f);
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

            PausedWidget = Gui.RootItem.AddChild(new Widget()
            {
                Text = "\n\nPaused",
                AutoLayout = DwarfCorp.Gui.AutoLayout.FloatCenter,
                Tooltip = "(push " + ControlSettings.Mappings.Pause.ToString() + " to unpause)",
                Font = "font18-outline",
                TextColor = Color.White.ToVector4(),
                MaximumSize = new Point(0, 0),
                WrapText = false,
                Hidden = true,
            });

            #endregion

            #region Announcer and info tray

            Announcer = Gui.RootItem.AddChild(new AnnouncementPopup
            {
                OnLayout = (sender) =>
                {
                    sender.Rect = new Rectangle(Gui.RenderData.VirtualScreen.Width - 350,
                        secondBar.Rect.Top - 130, 350, 128);
                }
            }) as AnnouncementPopup;

            World.OnAnnouncement = (message) =>
            {
                Announcer.QueueAnnouncement(message);
            };

            InfoTray = Gui.RootItem.AddChild(new InfoTray
            {
                OnLayout = (sender) =>
                {
                    sender.Rect = new Rectangle(0, 0, 0, 0);
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
                InteriorMargin = new Margin(2, 2, 2, 2),
                ItemSource = new Gui.Widget[]

                        {
                            new Gui.Widgets.FramedIcon
                            {
                                Icon = new Gui.TileReference("tool-icons", 29),
                                DrawFrame = false,
                                Tooltip = "Block brush",
                                OnClick = (widget, args) =>
                                {
                                    VoxSelector.Brush = VoxelBrushes.BoxBrush;
                                    SetMouseOverlay("tool-icons", 29);
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
                                    VoxSelector.Brush = VoxelBrushes.ShellBrush;
                                    SetMouseOverlay("tool-icons", 30);
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
                                    VoxSelector.Brush = VoxelBrushes.StairBrush;
                                    SetMouseOverlay("tool-icons", 31);
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
                                    Renderer.ChangeCameraMode(OrbitCamera.ControlType.Overhead);
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
                                    Renderer.ChangeCameraMode(OrbitCamera.ControlType.Walk);
                                }
                            }
                  }
            }) as Gui.Widgets.ToggleTray;

            Xray = BottomBar.AddChild(new Gui.Widgets.CheckBox()
            {
                Text = "X-ray",
                Tooltip = "When checked, enables XRAY view.",
                MaximumSize = new Point(64, 16),
                TextColor = Color.White.ToVector4(),
                Tag = "xray",
                OnCheckStateChange = (sender) =>
                {
                    bool isChecked = (sender as CheckBox).CheckState;
                    Renderer.TargetCaveView = isChecked ? 1.0f : 0.0f;
                    World.Tutorial("xray");
                },
                AutoLayout = AutoLayout.DockLeftCentered
            }) as CheckBox;

            if (Renderer.Camera.Control == OrbitCamera.ControlType.Overhead)
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
                OnClick = (sender, args) => ChangeTool("SelectUnits"),
                Tooltip = "Select dwarves",
                Behavior = FlatToolTray.IconBehavior.LeafIcon,
                OnConstruct = (sender) =>
                {
                    // This could just be done after declaring icon_SelectTool, but is here for
                    // consistency with other icons where this is not possible.
                    AddToolbarIcon(sender, () => true);
                    AddToolSelectIcon("SelectUnits", sender);
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
                    ChangeTool("SelectUnits");
                }
            };

            var icon_destroy_room = new FlatToolTray.Icon
            {
                Text = "Destroy",
                TextVerticalAlign = VerticalAlign.Below,
                Tooltip = "Deconstruct objects",
                Icon = new TileReference("round-buttons", 5),
                OnClick = (sender, args) =>
                {
                    ShowToolPopup("Left click zones to destroy them.");
                    ChangeTool("DestroyZone");
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            var menu_RoomTypes = new FlatToolTray.Tray
            {
                ItemSource = (new Widget[] {
                    icon_menu_RoomTypes_Return,
                    icon_destroy_room
                }).Concat(RoomLibrary.GetRoomTypes().Select(RoomLibrary.GetData)
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.NewIcon,
                        ExpandChildWhenDisabled = true,
                        Text = data.DisplayName,
                        TextVerticalAlign = VerticalAlign.Below,
                        TextColor = Color.White.ToVector4(),
                        PopupChild = new BuildRoomInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 256, 164),
                            World = World
                        },
                        OnClick = (sender, args) =>
                        {
                            World.RoomBuilder.CurrentRoomData = data;
                            VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                            ChangeTool("BuildZone");
                            ShowToolPopup("Click and drag to build " + data.Name);
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
                    ShowToolPopup("Left click objects to move them.\nRight click to destroy them.");
                    ChangeTool("MoveObjects");
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
                    ShowToolPopup("Left click objects to destroy them.");
                    ChangeTool("DeconstructObjects");
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            var icon_BuildRoom = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 37),
                EnabledTextColor = Vector4.One,
                Text = "Zone",
                Tooltip = "Designate zones/areas.",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
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
                    ChangeTool("SelectUnits");
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
                        Library.EnumerateVoxelTypes()
                        .Where(voxel => voxel.IsBuildable && World.ListResources().Any(r => voxel.CanBuildWith(ResourceLibrary.GetResourceByName(r.Value.Type))))
                        .Select(data => new FlatToolTray.Icon
                        {
                            Tooltip = "Build " + data.Name,
                            Icon = new Gui.TileReference("voxels", data.ID),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            Text = data.Name,
                            EnabledTextColor = Color.White.ToVector4(),
                            Font = "font10-outline-numsonly",
                            PopupChild = new BuildWallInfo
                            {
                                Data = data,
                                Rect = new Rectangle(0, 0, 256, 128),
                                World = World
                            },
                            OnClick = (sender, args) =>
                            {
                                World.RoomBuilder.CurrentRoomData = null;
                                VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                                var tool = Tools["BuildWall"] as BuildWallTool;
                                tool.BuildFloor = false;
                                tool.CurrentVoxelType = (byte)data.ID;
                                ChangeTool("BuildWall");
                                ShowToolPopup("Click and drag to build " + data.Name + " wall.");
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
                        Library.EnumerateVoxelTypes()
                        .Where(voxel => voxel.IsBuildable && World.ListResources().Any(r => voxel.CanBuildWith(ResourceLibrary.GetResourceByName(r.Value.Type))))
                        .Select(data => new FlatToolTray.Icon
                        {
                            Tooltip = "Build " + data.Name,
                            Icon = new Gui.TileReference("voxels", data.ID),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            Text = data.Name,
                            EnabledTextColor = Color.White.ToVector4(),
                            Font = "font10-outline-numsonly",
                            PopupChild = new BuildWallInfo
                            {
                                Data = data,
                                Rect = new Rectangle(0, 0, 256, 128),
                                World = World
                            },
                            OnClick = (sender, args) =>
                            {
                                World.RoomBuilder.CurrentRoomData = null;
                                VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                                var tool = Tools["BuildWall"] as BuildWallTool;
                                tool.BuildFloor = true;
                                tool.CurrentVoxelType = (byte)data.ID;
                                ChangeTool("BuildWall"); // Wut
                                ShowToolPopup("Click and drag to build " + data.Name + " floor.");
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

            var icon_BuildWall = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 24),
                Font = "font8",
                KeepChildVisible = true,
                ExpandChildWhenDisabled = true,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                Tooltip = "Place blocks",
                Text = "Block",
                EnabledTextColor = Color.White.ToVector4(),
                ReplacementMenu = menu_WallTypes,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var icon_BuildFloor = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 25),
                Font = "font8",
                KeepChildVisible = true,
                ExpandChildWhenDisabled = true,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
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
                    ChangeTool("SelectUnits");
                }
            };


            // TODO: Translation
            Func<string, string> objectNameToLabel = (string name) =>
            {
                var replacement = name.Replace("Potion", "").Replace("of", "");
                return replacement;
            };

            Func<CraftItem, FlatToolTray.Icon> createCraftIcon = (data) => new FlatToolTray.Icon
            {
                Icon = data.Icon,
                Tooltip = Library.GetString("craft", data.DisplayName),
                KeepChildVisible = true, // So the player can interact with the popup.
                ExpandChildWhenDisabled = true,
                Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                Text = objectNameToLabel(data.ShortDisplayName),
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
                        World.Tutorial(data.Name);
                    },
                    OnSelectedTabChanged = (widget) =>
                    {
                        var panel = (widget as TabPanel);
                        this.Gui.SafeCall(panel.GetTabButton(panel.SelectedTab).OnShown, panel.GetTabButton(panel.SelectedTab));
                    },
                    TabSource = new KeyValuePair<String, Widget>[] {
                        new KeyValuePair<string, Widget>(
                            Library.GetString("place"),
                            new BuildCraftInfo
                            {
                                Data = data,
                                AllowWildcard = true,
                                Rect = new Rectangle(0, 0, 450, 200),
                                World = World,
                                Transparent = true,
                                BuildAction = (sender, args) =>
                                {
                                    var buildInfo = sender as BuildCraftInfo;
                                    if (buildInfo == null)
                                        return;
                                    sender.Parent.Parent.Hidden = true;
                                    var tool = Tools["BuildObject"] as BuildObjectTool;
                                    tool.SelectedResources = buildInfo.GetSelectedResources();
                                    World.RoomBuilder.CurrentRoomData = null;
                                    VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                                    tool.CraftType = data;
                                    tool.Mode = BuildObjectTool.PlacementMode.BuildNew;

                                    // Todo: This should never be true.
                                    if (tool.PreviewBody != null)
                                    {
                                        tool.PreviewBody.GetRoot().Delete();
                                        tool.PreviewBody = null;
                                    }

                                    ChangeTool("BuildObject");
                                    ShowToolPopup(Library.GetString("click-and-drag", data.Verb, data.DisplayName));
                                },
                                PlaceAction = (sender, args) =>
                                {
                                    var buildInfo = sender as BuildCraftInfo;
                                    if (buildInfo == null)
                                        return;
                                    sender.Parent.Parent.Hidden = true;
                                    var tool = Tools["BuildObject"] as BuildObjectTool;
                                    tool.SelectedResources = null;
                                    World.RoomBuilder.CurrentRoomData = null;
                                    VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                                    tool.CraftType = data;
                                    tool.Mode = BuildObjectTool.PlacementMode.PlaceExisting;

                                    // Todo: This should never be true.
                                    if (tool.PreviewBody != null)
                                    {
                                        tool.PreviewBody.GetRoot().Delete();
                                        tool.PreviewBody = null;
                                    }

                                    ChangeTool("BuildObject");
                                    ShowToolPopup(Library.GetString("place", data.DisplayName));
                                }
                            }),
                        new KeyValuePair<string, Widget>(
                            Library.GetString("stockpile"),
                            new BuildCraftInfo
                            {
                                Data = data.ObjectAsCraftableResource(),
                                Rect = new Rectangle(0, 0, 450, 200),
                                World = World,
                                Transparent = true,
                                BuildAction = (sender, args) =>
                                {
                                    var buildInfo = (sender as Gui.Widgets.BuildCraftInfo);
                                    if (buildInfo == null)
                                        return;
                                    sender.Parent.Parent.Hidden = true;
                                    var assignments = new List<Task>();
                                    var numRepeats = buildInfo.GetNumRepeats();
                                    for (int i = 0; i < numRepeats; i++)
                                        assignments.Add(new CraftResourceTask(data.ObjectAsCraftableResource(), i + 1, numRepeats, buildInfo.GetSelectedResources()));
                                    World.TaskManager.AddTasks(assignments);
                                    ShowToolPopup(data.CurrentVerb + " " + numRepeats.ToString() + " " + (numRepeats == 1 ? data.DisplayName : data.PluralDisplayName));
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
                    ChangeTool("SelectUnits");
                },
                //TODO
                ReplacementMenu = null
            };

            Func<string, Widget> createCraftCategory = category =>
            {
                var menu_category = new FlatToolTray.Tray
                {
                    ItemSource = (new Widget[] { category_return_button }).Concat(
                    Library.EnumerateCraftables().ToList().Where(item => item.Type == CraftItem.CraftType.Object && item.Category == category)
                    .Select(data => createCraftIcon(data)))
                };
                var firstItem = Library.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Object && item.Category == category).First();
                var category_icon = new FlatToolTray.Icon
                {
                    Icon = firstItem.Icon,
                    Tooltip = "Craft " + category,
                    Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                    ReplacementMenu = menu_category,
                    Text = category,
                    TextVerticalAlign = VerticalAlign.Below,
                    TextColor = Color.White.ToVector4(),
                };
                return category_icon;
            };

            List<CraftItem> rootObjects = new List<CraftItem>();

            foreach (var item in Library.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Object && item.AllowUserCrafting))
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
                ItemSource = (new Widget[] { icon_menu_CraftTypes_Return }).Concat(
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
                Icon = new TileReference("tool-icons", 39),
                Text = "Objects",
                EnabledTextColor = Vector4.One,
                Tooltip = "Craft objects and furniture.",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                KeepChildVisible = true,
                MinimumSize = new Point(128, 32),
                ReplacementMenu = menu_CraftTypes,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            #endregion

            #region icon_BuildResource

            var icon_menu_Strings_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

            var menu_Strings = new FlatToolTray.Tray
            {
                Tag = "craft resource",
                Tooltip = "Craft resource",
                ItemSource = (new Widget[] { icon_menu_Strings_Return }).Concat(
                    Library.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Resource
                    && item.AllowUserCrafting
                    && ResourceLibrary.Exists(item.ResourceCreated) &&
                    !ResourceLibrary.GetResourceByName(item.ResourceCreated).Tags.Contains(Resource.ResourceTags.Edible) &&
                    item.CraftLocation != "Apothecary")
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.Icon,
                        Tooltip = data.Verb + " a " + data.Name,
                        KeepChildVisible = true, // So the player can interact with the popup.
                        ExpandChildWhenDisabled = true,
                        Text = data.Name,
                        TextVerticalAlign = VerticalAlign.Below,
                        TextColor = Color.White.ToVector4(),
                        Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                        PopupChild = new BuildCraftInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 450, 200),
                            World = World,
                            BuildAction = (sender, args) =>
                            {
                                var buildInfo = (sender as Gui.Widgets.BuildCraftInfo);
                                if (buildInfo == null)
                                    return;
                                sender.Hidden = true;
                                var assignments = new List<Task>();
                                for (int i = 0; i < buildInfo.GetNumRepeats(); i++)
                                    assignments.Add(new CraftResourceTask(data, i + 1, buildInfo.GetNumRepeats(), buildInfo.GetSelectedResources()));
                                World.TaskManager.AddTasks(assignments);
                                ShowToolPopup(data.CurrentVerb + " " + buildInfo.GetNumRepeats() + " " + data.Name);
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
                Text = "Goods",
                Tooltip = "Craft tradeable resources.",
                Icon = new TileReference("tool-icons", 38),
                EnabledTextColor = Vector4.One,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                KeepChildVisible = true,
                ReplacementMenu = menu_Strings,
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
                    ChangeTool("SelectUnits");
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
                    VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty; // This should be set by the tool.
                    var railTool = Tools["PaintRail"] as Rail.PaintRailTool;
                    railTool.SelectedResources = new List<ResourceAmount>
                                    {
                                        new ResourceAmount("Rail", 1)
                                    };
                    ChangeTool("PaintRail");
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
                            Library.EnumerateRailPatterns()
                            .Select(data => new FlatToolTray.Icon
                            {
                                Tooltip = "Build " + data.Name,
                                Text = data.Name,
                                TextVerticalAlign = VerticalAlign.Below,
                                TextColor = Color.White.ToVector4(),
                                Icon = new TileReference("rail", data.Icon),
                                KeepChildVisible = true,
                                ExpandChildWhenDisabled = true,
                                Behavior = FlatToolTray.IconBehavior.LeafIcon,
                                OnClick = (sender, args) =>
                                {
                                    VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty; // This should be set by the tool.
                                    var railTool = Tools["BuildRail"] as Rail.BuildRailTool;
                                    railTool.Pattern = data;
                                    ChangeTool("BuildRail");
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
                Icon = new TileReference("tool-icons", 23),
                EnabledTextColor = Vector4.One,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
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
                    ChangeTool("SelectUnits");
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
            icon_menu_Strings_Return.ReplacementMenu = menu_BuildTools;
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
                    AddToolbarIcon(sender, () => World.PlayerFaction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.BuildZone)));
                    AddToolSelectIcon("BuildZone", sender);
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
                    ChangeTool("SelectUnits");
                }
            };

            var menu_Edibles = new FlatToolTray.Tray
            {
                ItemSource = (new Widget[] { icon_menu_Edibles_Return }).Concat(
                    Library.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Resource
                    && item.AllowUserCrafting
                    && ResourceLibrary.Exists(item.ResourceCreated)
                    && ResourceLibrary.GetResourceByName(item.ResourceCreated).Tags.Contains(Resource.ResourceTags.Edible))
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.Icon,
                        KeepChildVisible = true, // So the player can interact with the popup.
                        Tooltip = data.Verb + " " + data.Name,
                        Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                        Text = data.Name,
                        TextVerticalAlign = VerticalAlign.Below,
                        PopupChild = new BuildCraftInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 450, 200),
                            World = World,
                            BuildAction = (sender, args) =>
                            {
                                var buildInfo = sender as Gui.Widgets.BuildCraftInfo;
                                var numRepeats = buildInfo.GetNumRepeats();

                                sender.Hidden = true;
                                var assignments = new List<Task>();
                                for (int i = 0; i < buildInfo.GetNumRepeats(); i++)
                                {
                                    assignments.Add(new CraftResourceTask(data, i + 1, buildInfo.GetNumRepeats(), buildInfo.GetSelectedResources()));
                                }
                                World.TaskManager.AddTasks(assignments);
                                ShowToolPopup(data.CurrentVerb + " " + numRepeats.ToString() + " " + (numRepeats == 1 ? data.DisplayName : data.PluralDisplayName));
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
                    World.PlayerFaction.Minions.Any(minion =>
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
                    ChangeTool("SelectUnits");
                }
            };

            var menu_potions = new FlatToolTray.Tray
            {
                ItemSource = (new Widget[] { icon_menu_Edibles_Return }).Concat(
                    Library.EnumerateCraftables().Where(item => item.Type == CraftItem.CraftType.Resource
                    && item.AllowUserCrafting
                    && ResourceLibrary.Exists(item.ResourceCreated)
                    && item.CraftLocation == "Apothecary")
                    .Select(data => new FlatToolTray.Icon
                    {
                        Icon = data.Icon,
                        KeepChildVisible = true, // So the player can interact with the popup.
                        Tooltip = Library.GetString("verb-noun", data.Verb, data.DisplayName),
                        Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                        Text = objectNameToLabel(data.DisplayName),
                        TextVerticalAlign = VerticalAlign.Below,
                        TextHorizontalAlign = HorizontalAlign.Center,
                        TextColor = Color.White.ToVector4(),
                        PopupChild = new BuildCraftInfo
                        {
                            Data = data,
                            Rect = new Rectangle(0, 0, 450, 200),
                            World = World,
                            OnShown = (sender) =>
                            {
                                World.Tutorial("potions");
                            },
                            BuildAction = (sender, args) =>
                            {
                                var buildInfo = sender as Gui.Widgets.BuildCraftInfo;
                                sender.Hidden = true;
                                var assignments = new List<Task>();
                                for (int i = 0; i < buildInfo.GetNumRepeats(); i++)
                                {
                                    assignments.Add(new CraftResourceTask(data, i + 1, buildInfo.GetNumRepeats(), buildInfo.GetSelectedResources()));
                                }
                                World.TaskManager.AddTasks(assignments);
                                ShowToolPopup(data.CurrentVerb + " " + data.Name);
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
                    World.PlayerFaction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Research)) && World.PlayerFaction.OwnedObjects.Any(obj => obj.Tags.Contains("Apothecary")));
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
                OnClick = (sender, args) => ChangeTool("Dig"),
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    World.PlayerFaction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Dig)));
                    AddToolSelectIcon("Dig", sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

            #endregion

            #region icon_GatherTool

            var icon_GatherTool = new FlatToolTray.Icon
            {
                Tag = "gather",
                Text = "Pick",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 6),
                Tooltip = "Gather",
                OnClick = (sender, args) => { ChangeTool("Gather"); World.Tutorial("gather"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    World.PlayerFaction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Gather)));
                    AddToolSelectIcon("Gather", sender);
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
                OnClick = (sender, args) => { ChangeTool("Chop"); World.Tutorial("chop"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    World.PlayerFaction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Chop)));
                    AddToolSelectIcon("Chop", sender);
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
                OnClick = (sender, args) => { ChangeTool("Attack"); World.Tutorial("attack"); },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    World.PlayerFaction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Attack)));
                    AddToolSelectIcon("Attack", sender);
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
                    ChangeTool("SelectUnits");
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
                    ChangeTool("SelectUnits");
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
                         World.ListResourcesWithTag(Resource.ResourceTags.Plantable)
                        .Select(resource => new FlatToolTray.Icon
                        {
                            Icon = ResourceLibrary.GetResourceByName(resource.Type)?.GuiLayers[0],
                            Tooltip = "Plant " + resource.Type,
                            Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                            Text = resource.Type,
                            TextVerticalAlign = VerticalAlign.Below,
                            OnClick = (sender, args) =>
                            {
                                ShowToolPopup("Click and drag to plant " + resource.Type + ".");
                                ChangeTool("Plant");
                                var plantTool = Tools["Plant"] as PlantTool;
                                plantTool.PlantType = resource.Type;
                                plantTool.RequiredResources = new List<ResourceAmount>()
                                    {
                                          new ResourceAmount(resource.Type)
                                    };
                                World.Tutorial("plant");
                            },
                            PopupChild = new PlantInfo()
                            {
                                Type = resource.Type,
                                Rect = new Rectangle(0, 0, 256, 128),
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
                    ChangeTool("Wrangle");
                    World.Tutorial("wrangle");
                    ShowToolPopup("Left click to tell dwarves to catch animals.\nRight click to cancel catching.\nRequires animal pen.");
                },
                OnConstruct = (sender) =>
                {
                    AddToolbarIcon(sender, () =>
                    World.PlayerFaction.Minions.Any(minion =>
                        minion.Stats.IsTaskAllowed(Task.TaskCategory.Wrangle)));
                    AddToolSelectIcon("Wrangle", sender);
                },
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };
            #endregion

            #endregion

            #region icon_CancelTasks

            var icon_CancelTasks = new FlatToolTray.Icon()
            {
                Text = "Cancel",
                TextVerticalAlign = VerticalAlign.Below,
                Tooltip = "Cancel voxel tasks such as mining, guarding, and planting.",
                Icon = new TileReference("round-buttons", 5),
                OnClick = (sender, args) =>
                {
                    ChangeTool("CancelTasks");
                    (Tools["CancelTasks"] as CancelTasksTool).Options = (sender as FlatToolTray.Icon).PopupChild as CancelToolOptions;
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
                    icon_AttackTool,
                    icon_Plant,
                    icon_Wrangle,
                    icon_Potions,
                    icon_CancelTasks,
                },
                OnShown = (sender) => ChangeTool("SelectUnits"),
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
                    menu_Strings,
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
            ChangeTool("SelectUnits");

            #endregion

            #region GOD MODE

            GodMenu = Gui.RootItem.AddChild(new Gui.Widgets.GodMenu
            {
                World = World,
                AutoLayout = AutoLayout.FloatTopLeft
            }) as Gui.Widgets.GodMenu;

            GodMenu.Hidden = true;

            #endregion

            Gui.RootItem.Layout();

            // Now that it's laid out, bring the second bar to the front so commands draw over other shit.
            secondBar.BringToFront();
            GodMenu.BringToFront();

            BodySelector.LeftReleased += BodySelector_LeftReleased;
            (Tools["SelectUnits"] as DwarfSelectorTool).DrawSelectionRect = b => ContextCommands.Any(c => c.CanBeAppliedTo(b, World));
        }

        private List<GameComponent> BodySelector_LeftReleased()
        {
            if (MultiContextMenu != null)
            {
                MultiContextMenu.Close();
                MultiContextMenu = null;
            }

            if (CurrentToolMode != "SelectUnits")
                return null;

            SelectedObjects.RemoveAll(b => !ContextCommands.Any(c => c.CanBeAppliedTo(b, World)));
            var bodiesClicked = SelectedObjects;

            if (bodiesClicked.Count > 0)
            {
                var availableCommands = ContextCommands.Where(c => bodiesClicked.Any(b => c.CanBeAppliedTo(b, World)));

                if (availableCommands.Count() > 0)
                {
                    // Show context menu.
                    MultiContextMenu = Gui.ConstructWidget(new HorizontalContextMenu
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
                    Gui.ShowDialog(MultiContextMenu);
                    Gui.RootItem.SendToBefore(MultiContextMenu, BottomBackground);
                }
            }
            else if (MultiContextMenu != null)
            {
                MultiContextMenu.Close();
                MultiContextMenu = null;
            }
            return null;
        }

        private void MakeMenuItem(Gui.Widget Menu, string Name, string Tooltip, Action<Gui.Widget, Gui.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gui.Widget
            {
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockTop,
                Border = "border-thin",
                Font = "font16",
                Text = Name,
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = DwarfCorp.Gui.HorizontalAlign.Center,
                TextVerticalAlign = DwarfCorp.Gui.VerticalAlign.Center,
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
                Rect = new Rectangle(Gui.RenderData.VirtualScreen.Center.X - 128,
                    Gui.RenderData.VirtualScreen.Center.Y - 150, 256, 230),
                Border = "border-fancy",
                TextHorizontalAlign = DwarfCorp.Gui.HorizontalAlign.Center,
                Text = "- Paused -",
                InteriorMargin = new Gui.Margin(24, 0, 0, 0),
                Padding = new Gui.Margin(2, 2, 2, 2),
                OnClose = (sender) =>
                {
                    PausePanel = null;
                    if (!pausedRightNow)
                        GameSpeedControls.Resume();
                    Paused = pausedRightNow;
                },
                Font = "font16"
            };

            Gui.ConstructWidget(PausePanel);

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
                var state = new OptionsState(Game)
                {
                    OnClosed = () =>
                    {
                        PausePanel = null;
                        Gui.RenderData.CalculateScreenSize();
                        Gui.ResetGui();
                        CreateGUIComponents();
                        OpenPauseMenu();
                    },
                    World = World
                };

                GameStateManager.PushState(state);
            });

            MakeMenuItem(PausePanel, "Help", "", (sender, args) =>
            {
                GameStateManager.PushState(new TutorialViewState(Game, World));
            });

            MakeMenuItem(PausePanel, "Save", "",
                (sender, args) =>
                {
                    World.Save(
                        (success, exception) =>
                        {
                            Gui.ShowModalPopup(new Gui.Widgets.Popup
                            {
                                Text = success ? "File saved." : "Save failed - " + exception.Message,
                                OnClose = (s2) => OpenPauseMenu()
                            });
                        });
                });

            MakeMenuItem(PausePanel, "Quit", "", (sender, args) =>
            {
                Gui.ShowModalPopup(new Gui.Widgets.Confirm
                {
                    Text = "Are you sure you want to quit?",
                    OkayText = "Yes",
                    CancelText = "No",
                    Font = "Font16",
                    OnClose = (_sender) =>
                    {
                        var result = (_sender as Confirm).DialogResult;
                        if (result == Confirm.Result.OKAY)
                            QuitOnNextUpdate = true;
                    }
                });
            });

            PausePanel.Layout();

            Gui.ShowModalPopup(PausePanel);
        }

        public void Destroy()
        {
            Input.Destroy();
        }

        public void QuitGame()
        {
            QuitGame(new MainMenuState(Game));
        }

        public void QuitGame(GameState state)
        {
            World.Quit();
            GameStateManager.ClearState();
            Destroy();

            GameStateManager.PushState(state);
        }

        public void AutoSave(Action<bool> callback = null)
        {
            bool paused = World.Paused;

            World.Save(
                    (success, exception) =>
                    {
                        World.MakeAnnouncement(success ? "File autosaved." : "Autosave failed - " + exception.Message);
                        World.Paused = paused;
                        callback?.Invoke(success);
                    });
        }
    }
}
