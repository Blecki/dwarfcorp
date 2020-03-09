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
        private FlatToolTray.RootTray BottomToolBar;
        private FlatToolTray.Tray MainMenu;
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
        private Dictionary<uint, WorldPopup> LastWorldPopup = new Dictionary<uint, WorldPopup>();
        private List<Widget> TogglePanels = new List<Widget>();

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
        }

        private void HideTogglePanels()
        {
            foreach (var panel in TogglePanels)
                panel.Hidden = true;
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
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(208, 204),
                OnLayout = (sender) => sender.Rect.Y += 4
            }) as Gui.Widgets.Minimap.MinimapFrame;

            SelectedEmployeeInfo = Gui.RootItem.AddChild(new Play.EmployeeInfo.OverviewPanel
            {
                Hidden = true,
                Border = "border-fancy",
                Employee = null,
                EnablePosession = true,
                Tag = "selected-employee-info",
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(450, 500 - (50 * (GameSettings.Current.GuiScale - 1))),
            }) as Play.EmployeeInfo.OverviewPanel;

            var markerFilter = Gui.RootItem.AddChild(new DesignationFilter
            {
                DesignationSet = World.PersistentData.Designations,
                World = World,
                Hidden = true,
                Border = "border-fancy",
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

            TogglePanels = new List<Widget>
            {
                MinimapFrame,
                SelectedEmployeeInfo,
                markerFilter,
                taskList,
                roomList,
            };

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
                        HideTogglePanels();
                        MinimapFrame.Hidden = false;
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
                    if (SelectedEmployeeInfo.Hidden)
                    {
                        HideTogglePanels();
                        SelectedEmployeeInfo.Hidden = false;
                    }
                    else
                        SelectedEmployeeInfo.Hidden = true;
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
                        HideTogglePanels();
                        markerFilter.Hidden = false;
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
                        HideTogglePanels();
                        taskList.Hidden = false;
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
                        HideTogglePanels();
                        roomList.Hidden = false;
                    }
                    else
                        roomList.Hidden = true;
                }
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
                            ZonesIcon
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

            EconomyIcon = new Gui.Widgets.FramedIcon
            {
                Tag = "economy",
                Icon = new Gui.TileReference("tool-icons", 10),
                OnClick = (sender, args) => GameStateManager.PushState(new EconomyState(Game, World)),
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
                AlwaysPerfectSize = true,
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
                }).Concat(Library.EnumerateZoneTypes()
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
                        OnClick = (sender, args) => ChangeTool("BuildZone", data),
                        Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
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
                    ChangeTool("DeconstructObject");
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
                ItemSource = null,
                OnRefresh = (sender) =>
                {
                    (sender as IconTray).ItemSource = (new Widget[] { icon_menu_WallTypes_Return }).Concat(
                        Library.EnumerateVoxelTypes()
                        .Where(voxel => voxel.IsBuildable)
                        .Where(voxel => World.CanBuildVoxel(voxel))/*
                        {
                            var resourceCount = World.ListResourcesInStockpilesPlusMinions().Where(r => voxel.CanBuildWith(Library.GetResourceType(r.Key))).Sum(r => r.Value.First.Count + r.Value.Second.Count);

                            int newNum = Math.Max(resourceCount -
                                World.PersistentData.Designations.EnumerateDesignations(DesignationType.Put).Count(d =>
                                BuildRequirementsEqual(Library.GetVoxelType(d.Tag.ToString()), voxel)), 0);

                            return newNum > 0;
                        })//*/
                        .Select(data => new FlatToolTray.Icon // Todo: Sort blocks we actually have the materials for to the front when menu is shown?
                        {
                            Tooltip = "Build " + data.Name,
                            Icon = new Gui.TileReference("voxels", data.ID),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            //Text = data.Name,
                            EnabledTextColor = Color.White.ToVector4(),
                            Font = "font10-outline-numsonly",
                            PopupChild = new BuildWallInfo
                            {
                                Data = data,
                                Rect = new Rectangle(0, 0, 256, 128),
                                World = World
                            },
                            OnClick = (_sender, args) =>
                            {
                                ChangeTool("BuildWall", new BuildWallTool.BuildWallToolArguments
                                {
                                    VoxelType = (byte)data.ID,
                                    Floor = false
                                });
                            },
                            //OnUpdate = (_sender, args) => UpdateBlockWidget(_sender, data),
                            Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                            OnShown = (_sender) => World.Tutorial("build blocks"),
                            Hidden = false
                        }));

                    (sender as IconTray).ResetItemsFromSource();
                }
            };

            var menu_Floortypes = new FlatToolTray.Tray
            {
                Tag = "build floor",
                ItemSource = null,
                OnRefresh = (sender) =>
                {
                    (sender as IconTray).ItemSource = (new Widget[] { icon_menu_WallTypes_Return }).Concat(
                        Library.EnumerateVoxelTypes()
                        .Where(voxel => voxel.IsBuildable)
                        .Where(voxel => World.CanBuildVoxel(voxel))/*
                        {
                            var resourceCount = World.ListResourcesInStockpilesPlusMinions().Where(r => voxel.CanBuildWith(Library.GetResourceType(r.Key))).Sum(r => r.Value.First.Count + r.Value.Second.Count);

                            int newNum = Math.Max(resourceCount -
                                World.PersistentData.Designations.EnumerateDesignations(DesignationType.Put).Count(d =>
                                BuildRequirementsEqual(Library.GetVoxelType(d.Tag.ToString()), voxel)), 0);

                            return newNum > 0;
                        })//*/
                        .Select(data => new FlatToolTray.Icon // Todo: Sort blocks we actually have the materials for to the front when menu is shown?
                        {
                            Tooltip = "Build " + data.Name,
                            Icon = new Gui.TileReference("voxels", data.ID),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            //Text = data.Name,
                            EnabledTextColor = Color.White.ToVector4(),
                            Font = "font10-outline-numsonly",
                            PopupChild = new BuildWallInfo
                            {
                                Data = data,
                                Rect = new Rectangle(0, 0, 256, 128),
                                World = World
                            },
                            OnClick = (_sender, args) => ChangeTool("BuildWall", new BuildWallTool.BuildWallToolArguments
                                {
                                    VoxelType = (byte)data.ID,
                                    Floor = true
                                }),
                            //OnUpdate = (_sender, args) => UpdateBlockWidget(_sender, data),
                            Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                            OnShown = (_sender) => World.Tutorial("build blocks"),
                            Hidden = false
                        }));

                    (sender as IconTray).ResetItemsFromSource();
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

#region icon_Craft

            // TODO: Translation
            Func<string, string> objectNameToLabel = (string name) =>
            {
                var replacement = name.Replace("Potion", "").Replace("of", "");
                return replacement;
            };

            var menu_CraftTypes = CategoryMenuBuilder.CreateCategoryMenu(
                Library.EnumerateCraftables().ToList(),
                (data) => true,
                (data) => new FlatToolTray.Icon
                {
                    Icon = data.Icon,
                    NewStyleIcon = data.NewStyleIcon != null ? data.NewStyleIcon : (Library.GetResourceType((data as CraftItem).ResourceCreated).HasValue(out var res) ? res.Gui_Graphic : null),
                    Tooltip = Library.GetString("craft", data.DisplayName),
                    KeepChildVisible = true, // So the player can interact with the popup.
                    ExpandChildWhenDisabled = true,
                    Behavior = FlatToolTray.IconBehavior.ShowClickPopup,
                    Text = objectNameToLabel(data.DisplayName),
                    TextVerticalAlign = VerticalAlign.Below,
                    TextColor = Color.White.ToVector4(),
                    OnShown = (sender) => World.Tutorial("build crafts"),
                    PopupChild = new BuildCraftInfo
                    {
                        Data = data as CraftItem,
                        Rect = new Rectangle(0, 0, 450, 200),
                        World = World,
                        OnShown = (sender) => World.Tutorial((data as CraftItem).Name),
                        BuildAction = (sender, args) =>
                        {
                            var buildInfo = (sender as Gui.Widgets.BuildCraftInfo);
                            if (buildInfo == null)
                                return;
                            //sender.Hidden = true;

                            // Todo: Break out into task composition function.
                            var numRepeats = buildInfo.GetNumRepeats();
                            if (numRepeats > 1)
                            {
                                var subTasks = new List<Task>();
                                var compositeTask = new CompoundTask(String.Format("Craft {0} {1}", numRepeats, (data as CraftItem).PluralDisplayName), TaskCategory.CraftItem, TaskPriority.Medium);
                                for (var i = 0; i < numRepeats; ++i)
                                    subTasks.Add(new CraftResourceTask((data as CraftItem), i + 1, numRepeats, buildInfo.GetSelectedResources()) { Hidden = true });
                                World.TaskManager.AddTasks(subTasks);
                                compositeTask.AddSubTasks(subTasks);
                                World.TaskManager.AddTask(compositeTask);
                            }
                            else
                                World.TaskManager.AddTask(new CraftResourceTask((data as CraftItem), 1, 1, buildInfo.GetSelectedResources()));

                            ShowToolPopup((data as CraftItem).Verb.PresentTense + " " + numRepeats.ToString() + " " + (numRepeats == 1 ? data.DisplayName : (data as CraftItem).PluralDisplayName));
                            
                        }
                    }
                },
                (widget, args) => ChangeTool("SelectUnits"));

            var icon_Craft = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 39),
                Text = "Craft",
                EnabledTextColor = Vector4.One,
                Tooltip = "Craft objects and furniture.",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                KeepChildVisible = true,
                MinimumSize = new Point(128, 32),
                ReplacementMenu = menu_CraftTypes.Menu,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

#endregion

#region icon_PlaceObject

            var menu_PlaceTypes = CategoryMenuBuilder.CreateCategoryMenu(
                Library.EnumerateResourceTypes().Where(r => r.Placement_Placeable),
                (data) =>
                {
                    return World.ListResources().Any(r => r.Key == (data as ResourceType).TypeName);
                },
                (data) => new FlatToolTray.Icon
                {
                    Icon = data.Icon,
                    NewStyleIcon = data.NewStyleIcon,
                    Tooltip = Library.GetString("craft", data.DisplayName),
                    ExpandChildWhenDisabled = true,
                    Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                    Text = objectNameToLabel(data.DisplayName),
                    TextVerticalAlign = VerticalAlign.Below,
                    TextColor = Color.White.ToVector4(),
                    PopupChild = new PlaceCraftInfo
                    {
                        Data = data as ResourceType,
                        Rect = new Rectangle(0, 0, 256, 164),
                        World = World,
                    },
                    OnClick = (sender, args) => ChangeTool("PlaceObject", data)
                },
                (widget, args) => ChangeTool("SelectUnits"));

            var icon_PlaceObject = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 39),
                Text = "Objects",
                EnabledTextColor = Vector4.One,
                Tooltip = "Craft objects and furniture.",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Below,
                KeepChildVisible = true,
                MinimumSize = new Point(128, 32),
                ReplacementMenu = menu_PlaceTypes.Menu,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu
            };

#endregion

#region icon_Rail

            var icon_menu_Rail_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) => ChangeTool("SelectUnits")
            };

            var icon_menu_Rail_Paint = new FlatToolTray.Icon
            {
                Icon = new TileReference("rail", 0),
                Tooltip = "Paint",
                Text = "paint",
                TextVerticalAlign = VerticalAlign.Below,
                TextColor = Color.White.ToVector4(),
                Behavior = FlatToolTray.IconBehavior.LeafIcon,
                OnClick = (widget, args) => ChangeTool("PaintRail", Rail.PaintRailTool.Mode.Normal)
            };

            var menu_Rail = new FlatToolTray.Tray
            {
                Tag = "build rail",
                ItemSource = (new Widget[] { icon_menu_Rail_Return, icon_menu_Rail_Paint }).Concat(
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
                                OnClick = (sender, args) => ChangeTool("BuildRail", new Rail.BuildRailTool.Arguments
                                {
                                    Pattern = data,
                                    Mode = Rail.BuildRailTool.Mode.Normal
                                }),
                                Hidden = false
                            }))
                
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
                OnClick = (widget, args) => ChangeTool("SelectUnits")
            };

            var menu_BuildTools = new FlatToolTray.Tray
            {
                ItemSource = new FlatToolTray.Icon[]
                    {
                        icon_menu_BuildTools_Return,
                        icon_destroyObjects,
                        icon_BuildRoom,
                        icon_BuildWall,
                        icon_BuildFloor,
                        icon_PlaceObject,
                        icon_RailTool,
                    }
            };

            icon_menu_RoomTypes_Return.ReplacementMenu = menu_BuildTools;
            icon_menu_WallTypes_Return.ReplacementMenu = menu_BuildTools;
            icon_menu_Rail_Return.ReplacementMenu = menu_BuildTools;            
            menu_PlaceTypes.ReturnIcon.ReplacementMenu = menu_BuildTools;

            var icon_BuildTool = new FlatToolTray.Icon
            {
                Tag = "build",
                Text = "Place",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 2),
                KeepChildVisible = true,
                Tooltip = "Place voxels and object's you've built.",
                ReplacementMenu = menu_BuildTools,
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
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

#endregion

#region icon_GatherTool

            var icon_GatherTool = new FlatToolTray.Icon
            {
                Tag = "gather",
                Text = "Gather",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 6),
                Tooltip = "Tell dwarves to pick things up.",
                OnClick = (sender, args) => ChangeTool("Gather"),
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

#endregion

#region icon_ChopTool

            var icon_ChopTool = new FlatToolTray.Icon
            {
                Tag = "chop",
                Text = "Harvest",
                TextVerticalAlign = VerticalAlign.Below,
                Icon = new TileReference("tool-icons", 1),
                Tooltip = "Chop trees and harvest plants.",
                OnClick = (sender, args) => ChangeTool("Chop"),
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
                OnClick = (sender, args) => ChangeTool("Attack"),
                Behavior = FlatToolTray.IconBehavior.LeafIcon
            };

#endregion

#region icon_FarmTool

            var icon_menu_Farm_Return = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = (widget, args) => ChangeTool("SelectUnits")
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
                OnRefresh = (widget) =>
                {
                    widget.Clear();

                    (widget as FlatToolTray.Tray).ItemSource =
                        (new Widget[] { icon_menu_Plant_Return }).Concat(
                            ResourceSet.GroupByRealType(World.GetResourcesWithTag("Plantable"))     
                        .Select(group => new FlatToolTray.Icon
                        {
                            // Todo: Should support apparent type grouping.
                            Icon = (group.Prototype.HasValue(out var res) && res.GuiLayers != null) ? res.GuiLayers[0] : null, // Menu icons need to support new dynamic resource gui icons.
                            NewStyleIcon = (group.Prototype.HasValue(out var _res) ? _res.Gui_Graphic : null),
                            Tooltip = "Plant " + group.ApparentType,
                            Behavior = FlatToolTray.IconBehavior.ShowHoverPopup,
                            Text = group.ApparentType,
                            TextVerticalAlign = VerticalAlign.Below,
                            OnClick = (sender, args) => ChangeTool("Plant", group.ApparentType),
                            PopupChild = new PlantInfo()
                            {
                                Type = group.ApparentType,
                                Rect = new Rectangle(0, 0, 256, 128),
                                TextColor = Color.Black.ToVector4()
                            },
                        }
                       ));

                    (widget as IconTray).ResetItemsFromSource();

                    widget.Hidden = false;
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
                OnClick = (sender, args) => ChangeTool("Wrangle"),
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
                    icon_Craft,
                    icon_BuildTool,
                    icon_DigTool,
                    icon_GatherTool,
                    icon_ChopTool,
                    icon_AttackTool,
                    icon_Plant,
                    icon_Wrangle,
                    icon_CancelTasks,
                },
                OnShown = (sender) => ChangeTool("SelectUnits"),
                Tag = "tools"
            };

            icon_menu_BuildTools_Return.ReplacementMenu = MainMenu;
            menu_CraftTypes.ReturnIcon.ReplacementMenu = MainMenu;
            icon_menu_Farm_Return.ReplacementMenu = MainMenu;
            //icon_menu_Magic_Return.ReplacementMenu = MainMenu;
            icon_menu_Plant_Return.ReplacementMenu = MainMenu;

            BottomToolBar = secondBar.AddChild(new FlatToolTray.RootTray
            {
                AutoLayout = AutoLayout.DockFill,
                ItemSource = new Widget[] { },
            }) as FlatToolTray.RootTray;

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

            // Now that it's laid out, bring the second bar to the front so commands draw over other shit.
            secondBar.BringToFront();
            BottomToolBar.SwitchTray(MainMenu);
            GodMenu.BringToFront();

            BodySelector.LeftReleased += BodySelector_LeftReleased;
            (Tools["SelectUnits"] as DwarfSelectorTool).DrawSelectionRect = b => ContextCommands.Any(c => c.CanBeAppliedTo(b, World));
        }

    }
}
