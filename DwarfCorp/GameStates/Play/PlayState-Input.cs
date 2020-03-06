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
    public partial class PlayState : GameState
    {
        public static InputManager Input;

        public VoxelSelector VoxSelector;
        public BodySelector BodySelector;
        private List<GameComponent> SelectedObjects = new List<GameComponent>();

        private List<ContextCommands.ContextCommand> ContextCommands = new List<ContextCommands.ContextCommand>();

        private bool sliceDownheld = false;
        private bool sliceUpheld = false;
        private Timer sliceDownTimer = new Timer(0.5f, true, Timer.TimerMode.Real);
        private Timer sliceUpTimer = new Timer(0.5f, true, Timer.TimerMode.Real);
        private int rememberedViewValue = 0;


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

            Debugger.SetConsoleCommandContext(World);
            InfoTray.ClearTopMessage();

            // Hide tutorial while menu is up
            if (PausePanel == null || PausePanel.Hidden)
                World.TutorialManager.ShowTutorial();
            else
                World.TutorialManager.HideTutorial();

#if !DEBUG
            try
            {
#endif
            if (IsMouseOverGui)
                ShowInfo(InfoTray.TopEntry, "MOUSE OVER GUI");
            else
                BottomToolBar.RefreshVisibleTray();

            #region Input for GUI

            DwarfGame.GumInput.FireActions(Gui, (@event, args) =>
            {
                // Let old input handle mouse interaction for now. Will eventually need to be replaced.

                if (DwarfGame.IsConsoleVisible)
                    return;

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
                        var now = (float)gameTime.TotalRealTime.TotalSeconds;
                        if (now - timeOnLastClick < doubleClickThreshold)
                        {
                            World.Renderer.Camera.ZoomTo(World.Renderer.CursorLightPos);
                            World.Renderer.Camera.ZoomTo(World.Renderer.CursorLightPos);
                        }
                        timeOnLastClick = now;
                    }

                    if (args.MouseButton == 1) // Right mouse click.
                    {
                        var bodiesClicked = World.ComponentManager.FindRootBodiesInsideScreenRectangle(
                            new Rectangle(args.X, args.Y, 1, 1), World.Renderer.Camera);

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
                    if (args.KeyValue >= '0' && args.KeyValue <= '9' && (args.Control || args.Shift))
                    {
                        var savedPositionSlot = args.KeyValue - '0';

                        if (args.Control)
                        {
                            if (World.Renderer.PersistentSettings.SavedCameraPositions.ContainsKey(savedPositionSlot))
                            {
                                var saved = World.Renderer.PersistentSettings.SavedCameraPositions[savedPositionSlot];
                                World.Renderer.Camera.Target = saved.Target;
                                World.Renderer.Camera.ViewMatrix = saved.ViewMatrix;
                                World.Renderer.Camera.Position = saved.Position;
                                World.Renderer.SetMaxViewingLevel(saved.SliceLevel);
                            }
                        }
                        else if (args.Shift)
                        {
                            World.Renderer.PersistentSettings.SavedCameraPositions[savedPositionSlot] = new CameraPositiionSnapshot
                            {
                                Position = World.Renderer.Camera.Position,
                                Target = World.Renderer.Camera.Target,
                                ViewMatrix = World.Renderer.Camera.ViewMatrix,
                                SliceLevel = World.Renderer.PersistentSettings.MaxViewingLevel
                            };
                        }
                    }
                    else if (FlatToolTray.Tray.Hotkeys.Contains((Keys)args.KeyValue))
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                            (BottomToolBar.Children.First(w => w.Hidden == false) as FlatToolTray.Tray).Hotkey((Keys)args.KeyValue);
                    }
                    else if ((Keys)args.KeyValue == Keys.Escape)
                    {
                        BrushTray.Select(0);
                        CameraTray.Select(0);

                        if (World.TutorialManager.HasCurrentTutorial())
                            World.TutorialManager.DismissCurrentTutorial();
                        else if (TogglePanels.Any(p => p.Hidden == false))
                            HideTogglePanels();
                        else if (MainMenu.Hidden && PausePanel == null)
                            (BottomToolBar.Children.First(w => w.Hidden == false) as FlatToolTray.Tray).Hotkey(FlatToolTray.Tray.Hotkeys[0]);
                        else if (CurrentToolMode != "SelectUnits" && PausePanel == null)
                            ChangeTool("SelectUnits");
                        else if (PausePanel != null)
                            PausePanel.Close();
                        else
                            OpenPauseMenu();
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.SelectAllDwarves && (PausePanel == null || PausePanel.Hidden))
                    {
                        World.PersistentData.SelectedMinions.AddRange(World.PlayerFaction.Minions);
                        World.Tutorial("dwarf selected");
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.SelectNextEmployee && (PausePanel == null || PausePanel.Hidden))
                    {
                        if (World.PlayerFaction.Minions.Count > 0)
                        {
                            if (World.PersistentData.SelectedMinions.Count == 0)
                            {
                                World.PersistentData.SelectedMinions.Clear();
                                World.PersistentData.SelectedMinions.Add(World.PlayerFaction.Minions[0]);
                            }
                            else
                            {
                                var index = World.PlayerFaction.Minions.IndexOf(World.PersistentData.SelectedMinions[0]);
                                index += 1;
                                if (index >= World.PlayerFaction.Minions.Count)
                                    index = 0;
                                World.PersistentData.SelectedMinions.Clear();
                                World.PersistentData.SelectedMinions.Add(World.PlayerFaction.Minions[index]);
                            }

                            World.Tutorial("dwarf selected");
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.SelectPreviousEmployee && (PausePanel == null || PausePanel.Hidden))
                    {
                        if (World.PlayerFaction.Minions.Count > 0)
                        {
                            if (World.PersistentData.SelectedMinions.Count == 0)
                            {
                                World.PersistentData.SelectedMinions.Clear();
                                World.PersistentData.SelectedMinions.Add(World.PlayerFaction.Minions[0]);
                            }
                            else
                            {
                                var index = World.PlayerFaction.Minions.IndexOf(World.PersistentData.SelectedMinions[0]);
                                index -= 1;
                                if (index < 0)
                                    index = World.PlayerFaction.Minions.Count - 1;
                                World.PersistentData.SelectedMinions.Clear();
                                World.PersistentData.SelectedMinions.Add(World.PlayerFaction.Minions[index]);
                            }
                            World.Tutorial("dwarf selected");
                        }
                    }
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Pause)
                    {
                        if (PausePanel == null || PausePanel.Hidden)
                        {
                            World.Paused = !World.Paused;
                            if (World.Paused) GameSpeedControls.Pause();
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
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Map && (PausePanel == null || PausePanel.Hidden))
                        Gui.SafeCall(MinimapIcon.OnClick, MinimapIcon, new InputEventArgs());
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Employees && (PausePanel == null || PausePanel.Hidden))
                        Gui.SafeCall(EmployeesIcon.OnClick, EmployeesIcon, new InputEventArgs());
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Tasks && (PausePanel == null || PausePanel.Hidden))
                        Gui.SafeCall(TasksIcon.OnClick, TasksIcon, new InputEventArgs());
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Zones && (PausePanel == null || PausePanel.Hidden))
                        Gui.SafeCall(ZonesIcon.OnClick, ZonesIcon, new InputEventArgs());
                    else if ((Keys)args.KeyValue == ControlSettings.Mappings.Marks && (PausePanel == null || PausePanel.Hidden))
                        Gui.SafeCall(MarksIcon.OnClick, MarksIcon, new InputEventArgs());
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

            World.Renderer.Update(gameTime);
            Input.Update();
            CurrentTool.Update(Game, gameTime);

            #region World Popups

            if (LastWorldPopup != null)
            {
                var removals = new List<uint>();
                foreach (var popup in LastWorldPopup)
                {
                    popup.Value.Update(gameTime, World.Renderer.Camera, Game.GraphicsDevice.Viewport);
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

            #region Update money, stock, and supervisor labels
            var pulse = 0.25f * (float)Math.Sin(gameTime.TotalRealTime.TotalSeconds * 4) + 0.25f;
            MoneyLabel.Text = World.PlayerFaction.Economy.Funds.ToString();
            MoneyLabel.TextColor = World.PlayerFaction.Economy.Funds > 1.0m ? Color.White.ToVector4() : new Vector4(1.0f, pulse, pulse, 1.0f);
            MoneyLabel.Invalidate();
            int availableSpace = World.ComputeRemainingStockpileSpace();
            int totalSpace = World.ComputeTotalStockpileSpace();
            StocksLabel.Text = String.Format("    Stocks: {0}/{1}", totalSpace - availableSpace, totalSpace);
            StocksLabel.TextColor = availableSpace > 0 ? Color.White.ToVector4() : new Vector4(1.0f, pulse, pulse, 1.0f);
            StocksLabel.Invalidate();
            LevelLabel.Text = String.Format("{0}/{1}", World.Renderer.PersistentSettings.MaxViewingLevel, World.WorldSizeInVoxels.Y);
            LevelLabel.Invalidate();
            SupervisionLabel.Text = String.Format("{0}/{1}", World.CalculateSupervisedEmployees(), World.CalculateSupervisionCap());
            SupervisionLabel.Invalidate();
            #endregion

            BottomBar.Layout();

            if (GameSpeedControls.CurrentSpeed != (int)DwarfTime.LastTime.Speed)
                World.Tutorial("time");

            GameSpeedControls.CurrentSpeed = (int)DwarfTime.LastTime.Speed;

            if (PausedWidget.Hidden == World.Paused)
            {
                PausedWidget.Hidden = !World.Paused;
                PausedWidget.Invalidate();
            }

            // Really just handles mouse pointer animation.
            Gui.Update(gameTime.ToRealTime());

            AutoSaveTimer.Update(gameTime);

            if (GameSettings.Current.AutoSave && AutoSaveTimer.HasTriggered)
                AutoSave();

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
                World.DisplaySpeciesCountsInMetrics();
                // Todo: Employee AI debug display

                var scheduleDisplay = DwarfGame.GetConsoleTile("FORECAST");
                scheduleDisplay.TextSize = 1;
                scheduleDisplay.Lines.Clear();
                scheduleDisplay.Lines.Add(String.Format("Diff:{0:+00;-00;+00} Forecast:{1:+00;-00;+00}", World.EventScheduler.CurrentDifficulty, World.EventScheduler.ForecastDifficulty(World.Time.CurrentDate)));
                foreach (var scheduledEvent in World.EventScheduler.Forecast)
                    scheduleDisplay.Lines.Add(String.Format("{2:+00;-00;+00} {1} {0}", scheduledEvent.Event.Name, (scheduledEvent.Date - World.Time.CurrentDate).ToString(@"hh\:mm"), scheduledEvent.Event.Difficulty));
                scheduleDisplay.Invalidate();
            }
            #endregion

#if !DEBUG
        }
            catch (Exception e)
            {
                Program.CaptureException(e);
                if (Program.ShowErrorDialog(e.Message))
                    throw new HandledException(e);
            }
#endif 
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
    }
}
