using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    public partial class PlayState : GameState
    {
        public Gui.Root Gui;

        private Widget MoneyLabel;
        private Widget LevelLabel;
        private Widget SupervisionLabel;
        private Widget StocksLabel;
        private Widget TimeLabel;
        private Widget PausePanel;
        private Gui.Widgets.Minimap.MinimapFrame MinimapFrame;
        private Gui.Widgets.Minimap.MinimapRenderer MinimapRenderer;
        private GameSpeedControls GameSpeedControls;
        private Widget PausedWidget;
        private InfoTray InfoTray;
        private ToggleTray BrushTray;
        private ToggleTray CameraTray;
        private CheckBox Xray;
        private Play.GodMenu GodMenu;
        private AnnouncementPopup Announcer;
        private FramedIcon EconomyIcon;
        private Timer AutoSaveTimer;
        private Play.EmployeeInfo.OverviewPanel SelectedEmployeeInfo;
        private Widget ContextMenu;
        private Widget MultiContextMenu;
        private Widget BottomBar;
        private Widget BottomBackground;
        private Widget MinimapIcon;
        private Widget EmployeesIcon;
        private Widget ZonesIcon;
        private Widget TasksIcon;
        private Widget MarksIcon;
        private Widget CommandsIcon;
        private Dictionary<uint, WorldPopup> LastWorldPopup = new Dictionary<uint, WorldPopup>();
        private Play.CommandTray CommandTray;



        public Gui.MousePointer MousePointer = new Gui.MousePointer("mouse", 1, 0);

        public void ShowTooltip(String Text)
        {
            Gui.ShowTooltip(Gui.MousePosition, Text);
        }

        public void ShowInfo(UInt32 EntityID, String Text)
        {
            InfoTray.AddMessage(EntityID, Text);
        }

        public void ShowToolPopup(String Text)
        {
            if (String.IsNullOrEmpty(Text))
            {
                if (Gui.TooltipItem != null)
                    Gui.DestroyWidget(Gui.TooltipItem);
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
                return Gui.HoverItem != null || Gui.Dragging;
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

        public void ShowEmployeeDialog(CreatureAI Employee, Rectangle ParentRect)
        {
            SelectedEmployeeInfo.Employee = Employee;
            var rect = new Rectangle(ParentRect.Right, ParentRect.Y, SelectedEmployeeInfo.Rect.Width, SelectedEmployeeInfo.Rect.Height);
            if (rect.Right > Gui.RenderData.VirtualScreen.Right) rect.X = ParentRect.X - SelectedEmployeeInfo.Rect.Width;
            SelectedEmployeeInfo.Rect = rect;
            SelectedEmployeeInfo.Hidden = false;
            SelectedEmployeeInfo.Layout();
            SelectedEmployeeInfo.BringToFront();
        }

        private void UpdateGui(DwarfTime gameTime)
        {
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

            if (GameSpeedControls.CurrentSpeed != (int)DwarfTime.LastTimeX.Speed)
                World.Tutorial("time");

            GameSpeedControls.CurrentSpeed = (int)DwarfTime.LastTimeX.Speed;

            if (PausedWidget.Hidden == World.Paused)
            {
                PausedWidget.Hidden = !World.Paused;
                PausedWidget.Invalidate();
            }

            // Really just handles mouse pointer animation.
            Gui.Update(gameTime.ToRealTime());
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
                HoverTextColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4(),
                ChangeColorOnHover = true
            });
        }

        public void OpenPauseMenu()
        {
            if (PausePanel != null)
                return;

            var pausedRightNow = World.Paused;
            GameSpeedControls.Pause();

            PausePanel = new Gui.Widget
            {
                Rect = new Rectangle(Gui.RenderData.VirtualScreen.Center.X - 128, Gui.RenderData.VirtualScreen.Center.Y - 150, 256, 230),
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
                    World.Paused = pausedRightNow;
                },
                Font = "font16"
            };

            Gui.ConstructWidget(PausePanel);

            MakeMenuItem(PausePanel, "Continue", "", (sender, args) =>
            {
                PausePanel.Close();
                if (!pausedRightNow)
                    GameSpeedControls.Resume();
                else
                    GameSpeedControls.Pause();
                World.Paused = pausedRightNow;
                PausedWidget.Hidden = !World.Paused;
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

            MakeMenuItem(PausePanel, "Save", "", (sender, args) =>
            {
                World.Save((success, exception) =>
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

        public void CreateGUIComponents()
        {
            BottomBackground = Gui.RootItem.AddChild(new TrayBackground
            {
                Corners = Scale9Corners.Top,
                MinimumSize = new Point(0, 118),
                AutoLayout = AutoLayout.DockBottom
            });

            BottomBar = BottomBackground.AddChild(new Gui.Widget
            {
                Transparent = true,
                //Background = new TileReference("basic", 0),
                //BackgroundColor = new Vector4(0, 0, 0, 0.5f),
                Padding = new Margin(0, 0, 2, 2),
                MinimumSize = new Point(0, 42),
                AutoLayout = AutoLayout.DockBottom
            });

            var secondBar = BottomBackground.AddChild(new Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 70),
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
                    World.Renderer.SetMaxViewingLevel(World.Renderer.PersistentSettings.MaxViewingLevel - 1);
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
                    World.Renderer.SetMaxViewingLevel(World.Renderer.PersistentSettings.MaxViewingLevel + 1);
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
                AutoLayout = AutoLayout.FloatBottomLeft
            }) as Gui.Widgets.Minimap.MinimapFrame;

            SelectedEmployeeInfo = Gui.RootItem.AddChild(new Play.EmployeeInfo.OverviewPanel
            {
                Hidden = true,
                Employee = null,
                EnablePosession = true,
                Tag = "selected-employee-info",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(450, 500 - (50 * (GameSettings.Current.GuiScale - 1))),
            }) as Play.EmployeeInfo.OverviewPanel;

            var employeeListView = Gui.RootItem.AddChild(new Gui.Widgets.EmployeePanel
            {
                Hidden = true,
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(450, Gui.RenderData.VirtualScreen.Height - 120),
                World = World
            });

            var markerFilter = Gui.RootItem.AddChild(new DesignationFilter
            {
                DesignationSet = World.PersistentData.Designations,
                World = World,
                Hidden = true,
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(300, 200)
            });

            var taskList = Gui.RootItem.AddChild(new Play.TaskListPanel
            {
                Border = "border-thin",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(600, 300),
                Hidden = true,
                World = this.World
            });

            var roomList = Gui.RootItem.AddChild(new Play.ZoneListPanel
            {
                Border = "border-fancy",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(600, 300),
                Hidden = true,
                World = this.World
            });

            var commandPanel = Gui.RootItem.AddChild(new Play.CommandPanel
            {
                Border = "border-fancy",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(400, Math.Min(600, Gui.RenderData.VirtualScreen.Height - 100)),
                Hidden = true,
                World = this.World
            });

            var eventPanel = Gui.RootItem.AddChild(new EventLogViewer()
            {
                Border = "border-fancy",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(400, Math.Min(600, Gui.RenderData.VirtualScreen.Height - 100)),
                Log = World.EventLog,
                Now = World.Time.CurrentDate,
                Hidden = true
            });

            var economyPanel = Gui.RootItem.AddChild(new EconomyPanel()
            {
                Border = "border-fancy",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(600, Math.Min(600, Gui.RenderData.VirtualScreen.Height - 100)),
                World = World,
                Hidden = true
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
                        MinimapFrame.BringToFront();
                    }
                    else
                        MinimapFrame.Hidden = true;
                }
            };

            EmployeesIcon = new FramedIcon
            {
                Icon = new Gui.TileReference("tool-icons", 34),
                Text = "@play-employee-icon-label",
                Tooltip = "@play-employee-icon-tooltip",
                EnabledTextColor = Vector4.One,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                OnClick = (sender, args) =>
                {
                    if (employeeListView.Hidden)
                    {
                        employeeListView.Hidden = false;
                        employeeListView.BringToFront();
                    }
                    else
                        employeeListView.Hidden = true;
                }
            };

            MarksIcon = new FramedIcon
            {
                Icon = new Gui.TileReference("tool-icons", 17),
                Text = "@play-marks-icon-label",
                Tooltip = "@play-marks-icon-tooltip",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                EnabledTextColor = Vector4.One,
                OnClick = (sender, args) =>
                {
                    if (markerFilter.Hidden)
                    {
                        markerFilter.Hidden = false;
                        markerFilter.BringToFront();
                    }
                    else
                        markerFilter.Hidden = true;
                }
            };

            TasksIcon = new FramedIcon
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
                        taskList.Hidden = false;
                        taskList.BringToFront();
                    }
                    else
                        taskList.Hidden = true;
                }
            };

            ZonesIcon = new FramedIcon
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
                        roomList.Hidden = false;
                        roomList.BringToFront();
                    }
                    else
                        roomList.Hidden = true;
                }
            };

            CommandsIcon = new FramedIcon
            {
                Icon = new Gui.TileReference("tool-icons", 15),
                Text = "Commands",
                Tooltip = "Search all possible commands.",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                EnabledTextColor = Vector4.One,
                OnClick = (sender, args) =>
                {
                    if (commandPanel.Hidden)
                    {
                        commandPanel.Hidden = false;
                        commandPanel.BringToFront();
                    }
                    else
                        commandPanel.Hidden = true;
                }
            };

            var eventsIcon = new FramedIcon
            {
                Icon = new Gui.TileReference("tool-icons", 21),
                OnClick = (sender, args) =>
                {
                    if (eventPanel.Hidden)
                    {
                        eventPanel.Hidden = false;
                        eventPanel.BringToFront();
                    }
                    else
                        eventPanel.Hidden = true;
                },
                Text = Library.GetString("events-label"),
                TextVerticalAlign = VerticalAlign.Below,
                Tooltip = Library.GetString("events-tooltip")
            };

            EconomyIcon = new Gui.Widgets.FramedIcon
            {
                Tag = "economy",
                Icon = new Gui.TileReference("tool-icons", 10),
                OnClick = (sender, args) =>
                {
                    if (economyPanel.Hidden)
                    {
                        economyPanel.Hidden = false;
                        economyPanel.BringToFront();
                        World.Tutorial("economy");
                    }
                    else
                        economyPanel.Hidden = true;
                },
                Tooltip = Library.GetString("economy-tooltip"),
                Text = Library.GetString("economy-label"),
                TextVerticalAlign = VerticalAlign.Below
            };

            var diplomacyIcon = new Gui.Widgets.FramedIcon()
            {
                Icon = new Gui.TileReference("tool-icons", 36),
                OnClick = (sender, args) =>
                {
                    GameStateManager.PushState(new PlayFactionViewState(GameState.Game, World));
                },
                Text = Library.GetString("diplomacy-label"),
                TextVerticalAlign = VerticalAlign.Below,
                Tooltip = Library.GetString("diplomacy-tooltip")
            };

            var bottomLeft = secondBar.AddChild(new Gui.Widgets.IconTray
            {
                Corners = 0,
                Transparent = true,
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockLeft,
                SizeToGrid = new Point(5, 1),
                AlwaysPerfectSize = true,
                ItemSource = new Gui.Widget[]
                        {
                            MinimapIcon,
                            EmployeesIcon,
                            MarksIcon,
                            TasksIcon,
                            ZonesIcon,
                            CommandsIcon,
                            eventsIcon,
                            EconomyIcon,
                            diplomacyIcon
                        },
            });

            secondBar.AddChild(new Widget // Spacer
            {
                Transparent = true,
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(8, 0)
            });

#endregion

#region Setup right tray

            var topRightTray = secondBar.AddChild(new Gui.Widgets.IconTray
            {
                Corners = 0,//Gui.Scale9Corners.Top,
                Transparent = true,
                AutoLayout = DwarfCorp.Gui.AutoLayout.DockRight,
                SizeToGrid = new Point(4, 1),
                AlwaysPerfectSize = true,
                ItemSource = new Gui.Widget[]
                        {
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
                    if ((int)DwarfTime.LastTimeX.Speed != speed)
                    {
                        World.Tutorial("time");
                        if ((int)DwarfTime.LastTimeX.Speed == 0)
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
                        DwarfTime.LastTimeX.Speed = (float)speed;
                        World.Paused = speed == 0;
                        PausedWidget.Hidden = !World.Paused;
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
                AlwaysPerfectSize = true,
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
                AlwaysPerfectSize = true,
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
                                    World.Renderer.ChangeCameraMode(OrbitCamera.ControlType.Overhead);
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
                                    World.Renderer.ChangeCameraMode(OrbitCamera.ControlType.Walk);
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
                    World.Renderer.TargetCaveView = isChecked ? 1.0f : 0.0f;
                    World.Tutorial("xray");
                },
                AutoLayout = AutoLayout.DockLeftCentered
            }) as CheckBox;

            if (World.Renderer.Camera.Control == OrbitCamera.ControlType.Overhead)
            {
                CameraTray.Select(0);
            }
            else
            {
                CameraTray.Select(1);
            }

            #endregion

            #region Setup tool tray

            CommandTray = secondBar.AddChild(new Play.CommandTray
            {
                World = this.World,
                AutoLayout = AutoLayout.DockFill
            }) as Play.CommandTray;

            ChangeTool("SelectUnits");

#endregion

#region GOD MODE

            GodMenu = Gui.RootItem.AddChild(new Play.GodMenu
            {
                World = World,
                AutoLayout = AutoLayout.FloatTopLeft
            }) as Play.GodMenu;

            GodMenu.Hidden = true;

            #endregion

            Gui.RootItem.Layout();

            // Tell the command tray where the tool popup can exist.
            CommandTray.ToolPopupZone = new Rectangle(MinimapFrame.Rect.Right + 4, 0, Gui.RenderData.VirtualScreen.Width - MinimapFrame.Rect.Right - 8, 0);

            // Now that it's laid out, bring the second bar to the front so commands draw over other shit.
            secondBar.BringToFront();
            CommandTray.BringToFront();
            GodMenu.BringToFront();

            BodySelector.LeftReleased += BodySelector_LeftReleased;
            (Tools["SelectUnits"] as DwarfSelectorTool).DrawSelectionRect = b => ContextCommands.Any(c => c.CanBeAppliedTo(b, World));

            CommandTray.RefreshItems();
        }

    }
}
