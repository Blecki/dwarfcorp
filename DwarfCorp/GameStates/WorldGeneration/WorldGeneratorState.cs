using System;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Linq;

namespace DwarfCorp.GameStates
{
    public class WorldGeneratorState : GameState
    {
        private Gui.Root GuiRoot;
        private Gui.Widgets.ProgressBar GenerationProgress;
        public WorldGeneratorPreview Preview;
        private OverworldGenerator Generator;
        private Overworld Settings;
        private Widget MainPanel;
        private bool SuppressEnter = false;

        private Gui.Widget StartButton;

        private Gui.Widgets.EditableTextField NameField;
        private Gui.Widgets.EditableTextField MottoField;
        private Gui.Widgets.CompanyLogo CompanyLogoDisplay;
        private Gui.Widget NameEditBox;

        public static Point[] Sizes = new Point[]
        {
            new Point(4,4),
            new Point(8,8),
            new Point(16,16),
            new Point(24,24),
            new Point(32,32)
        };

        public static string[] LevelStrings = new string[]
{
            "Very Low",
            "Low",
            "Medium",
            "High",
            "Very High"
};


        public Widget CreateCombo<T>(Gui.Root Root, String Name, String Tooltip, T[] Values, Action<T> Setter, Func<T> Getter, String Font = "font10")
        {
            global::System.Diagnostics.Debug.Assert(Values.Length == LevelStrings.Length);

            var r = Root.ConstructWidget(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                Tooltip = Tooltip,
                Font = Font
            });

            r.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 0),
                Text = Name,
                Font = Font,
                TextVerticalAlign = VerticalAlign.Center
            });

            var combo = r.AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = AutoLayout.DockFill,
                Items = new List<String>(LevelStrings),
                Font = Font,
                OnSelectedIndexChanged = (sender) =>
                {
                    var box = sender as Gui.Widgets.ComboBox;
                    if (box.SelectedIndex >= 0 && box.SelectedIndex < Values.Length)
                        Setter(Values[box.SelectedIndex]);
                    RestartGeneration();
                }
            }) as Gui.Widgets.ComboBox;

            var index = (new List<T>(Values)).IndexOf(Getter());
            if (index == -1)
                combo.SilentSetSelectedIndex(2);
            else
                combo.SilentSetSelectedIndex(index);

            return r;
        }


        public enum WorldType
        {
            NewWorld,
            SavedWorld
        }

        private WorldType _WorldType = WorldType.NewWorld;
        
        public WorldGeneratorState(DwarfGame Game, Overworld Settings, WorldType _WorldType) :
            base(Game)
        {
            this._WorldType = _WorldType;
            this.Settings = Settings;
        }       

        private void RestartGeneration()
        {
            if (Generator != null)
                Generator.Abort();

            if (Settings.Natives != null)
                Settings.Natives.Clear();

            Generator = new OverworldGenerator(Settings, true);
            if (Preview != null) Preview.SetGenerator(Generator, Settings);

            GuiRoot.RootItem.GetChild(0).Text = Settings.Name;
            GuiRoot.RootItem.GetChild(0).Invalidate();
            GuiRoot.MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23);
            Preview.Hidden = true;

            Generator.Generate();
        }

        public override void OnEnter()
        {
            if (SuppressEnter)
            {
                SuppressEnter = false;
                return;
            }

            DwarfGame.GumInputMapper.GetInputQueue();

            #region Setup GUI
            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23);

            MainPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = GuiRoot.RenderData.VirtualScreen,
                Border = "border-fancy",
                Text = Settings.Name,
                Font = "font16",
                TextColor = new Vector4(0, 0, 0, 1),
                Padding = new Margin(4, 4, 4, 4),
                InteriorMargin = new Margin(24, 0, 0, 0)
            });

            #region Bottom Bar

            var bottomBar = MainPanel.AddChild(new Widget
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                MinimumSize = new Point(0, 36),
                Padding = new Margin(0, 0, 4, 4)
            });

            StartButton = bottomBar.AddChild(new Gui.Widget
            {
                Text = "Launch",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    if (_WorldType == WorldType.SavedWorld)
                    {
                        var saveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Name;
                        var saveGame = SaveGame.LoadMetaFromDirectory(saveName);
                        if (saveGame != null)
                        {
                            DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is loading a saved game.");
                            Settings.InstanceSettings.LoadType = LoadType.LoadFromFile;

                            GameStateManager.ClearState();
                            GameStateManager.PushState(new LoadState(Game, Settings, LoadTypes.UseExistingOverworld));
                        }
                    }
                    else
                    {
                        Settings.Company.Name = NameField.Text;
                        Settings.Company.Motto = MottoField.Text;
                        Settings.PlayerCorporationFunds = Settings.Difficulty.StartingFunds;
                        Settings.Natives.FirstOrDefault(n => n.Name == "Player").PrimaryColor = new Color(Settings.Company.LogoBackgroundColor);

                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", string.Format("User is starting a game with a {0} x {1} world.", Settings.Width, Settings.Height));
                        Settings.InstanceSettings.LoadType = LoadType.CreateNew;

                        var message = "";
                        var valid = InstanceSettings.ValidateEmbarkment(Settings, out message);
                        if (valid == InstanceSettings.ValidationResult.Pass)
                            LaunchNewGame();
                        else if (valid == InstanceSettings.ValidationResult.Query)
                        {
                            var popup = new Gui.Widgets.Confirm()
                            {
                                Text = message,
                                OnClose = (_sender) =>
                                {
                                    if ((_sender as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                                        LaunchNewGame();
                                }
                            };
                            GuiRoot.ShowModalPopup(popup);
                        }
                        else if (valid == InstanceSettings.ValidationResult.Reject)
                        {
                            var popup = new Gui.Widgets.Confirm()
                            {
                                Text = message,
                                CancelText = ""
                            };
                            GuiRoot.ShowModalPopup(popup);
                        }
                    }
                }
            });

            bottomBar.AddChild(new Gui.Widget
            {
                Text = "Back",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockLeft,
                OnClick = (sender, args) =>
                {
                    GameStateManager.ClearState();
                    GameStateManager.PushState(new MainMenuState(Game));
                }
            });

            GenerationProgress = bottomBar.AddChild(new Gui.Widgets.ProgressBar
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Font = "font10",
                TextColor = new Vector4(1, 1, 1, 1)
            }) as Gui.Widgets.ProgressBar;

            #endregion

            #region Right Panel

            var rightPanel = MainPanel.AddChild(new Widget
            {
                MinimumSize = new Point(256, 0),
                Padding = new Margin(2, 2, 2, 2),
                AutoLayout = AutoLayout.DockRight
            });

            if (_WorldType == WorldType.NewWorld)
            {
                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Randomize",
                    Border = "border-button",
                    ChangeColorOnHover = true,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font10",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    OnClick = (sender, args) =>
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is regenerating the world.");
                        Settings.Seed = MathFunctions.RandInt(Int32.MinValue, Int32.MaxValue);
                        Settings.Name = Overworld.GetRandomWorldName();
                        RestartGeneration();
                    }
                });
            }

            if (_WorldType == WorldType.NewWorld)
            {
                rightPanel.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(64, 0),
                    Text = "Name",
                    Font = "font10"
                });

                var topRow = rightPanel.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(0, 30)
                });

                topRow.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockRight,
                    Border = "border-button",
                    Text = "Random",
                    Font = "font8",
                    OnClick = (sender, args) =>
                    {
                        Settings.Name = TextGenerator.GenerateRandom(TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds));
                        NameEditBox.Text = Settings.Name;
                    }
                });

                NameEditBox = topRow.AddChild(new Gui.Widgets.EditableTextField
                {
                    AutoLayout = AutoLayout.DockFill,
                    Text = Settings.Name,
                    Font = "font10",
                    OnTextChange = (sender) =>
                    {
                        Settings.Name = sender.Text;
                    }
                });

                var srow = rightPanel.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(0, 30),
                    Tooltip = "Set the world seed"
                });

                srow.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockLeft,
                    MinimumSize = new Point(64, 0),
                    Text = "Seed",
                    Font = "font10"
                });

                srow.AddChild(new Gui.Widgets.EditableTextField()
                {
                    AutoLayout = AutoLayout.DockFill,
                    Text = Settings.Seed.ToString(),
                    BeforeTextChange = (sender, args) =>
                    {
                        if (Int32.TryParse(args.NewText, out int s))
                            Settings.Seed = s;
                        else
                            args.Cancelled = true;
                    },
                    OnTextChange = (sender) =>
                    {
                        RestartGeneration();
                    }
                });
            }
            else
                rightPanel.AddChild(new Widget
                {
                    Text = "Seed: " + Settings.Seed,
                    Font = "font10",
                    AutoLayout = AutoLayout.DockTop
                });

            if (_WorldType == WorldType.NewWorld)
            { 
                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Advanced",
                    Border = "border-button",
                    ChangeColorOnHover = true,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font10",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    OnClick = (sender, args) =>
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is modifying advanced settings.");
                        var advancedSettingsEditor = GuiRoot.ConstructWidget(new Gui.Widgets.WorldGenerationSettingsDialog
                        {
                            Settings = Settings,
                            OnClose = (s) =>
                            {
                                if ((s as Gui.Widgets.WorldGenerationSettingsDialog).Result == Gui.Widgets.WorldGenerationSettingsDialog.DialogResult.Okay)
                                    RestartGeneration();
                            }
                        });

                        GuiRoot.ShowModalPopup(advancedSettingsEditor);
                    }
                });

                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Choose Biomes",
                    Border = "border-button",
                    ChangeColorOnHover = true,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font10",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    OnClick = (sender, args) =>
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is selecting biomes.");
                        var advancedSettingsEditor = GuiRoot.ConstructWidget(new BiomeChooser(Settings)
                        {
                            OnClose = (s) =>
                            {
                                RestartGeneration();
                            }
                        });

                        GuiRoot.ShowModalPopup(advancedSettingsEditor);
                    }
                });

                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Employees",
                    Border = "border-button",
                    ChangeColorOnHover = true,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font10",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    MinimumSize = new Point(0, 32),
                    OnClick = (sender, args) =>
                    {
                        DwarfGame.LogSentryBreadcrumb("Game Launcher", "User is modifying embarkment.");
                        var embarkmentEditor = GuiRoot.ConstructWidget(new EmbarkmentEditor(Settings)
                        {
                        });

                        GuiRoot.ShowModalPopup(embarkmentEditor);
                    }
                });
            }

            if (_WorldType == WorldType.NewWorld)
            {
                var sizeSelector = rightPanel.AddChild(CreateCombo<Point>(GuiRoot, "Size", "Size of the world in chunks", Sizes, (p) => Settings.SizeInChunks = p, () => Settings.SizeInChunks, "font8"));
            }

            rightPanel.AddChild(new Widget
            {
                Text = "Factions",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font10",
                AutoLayout = AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    SuppressEnter = true;
                    GameStateManager.PushState(new FactionViewState(GameState.Game, Settings));
                }
            });

            if (_WorldType == WorldType.NewWorld)
            {
                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Difficulty",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    Font = "font10",
                    TextColor = new Vector4(0, 0, 0, 1)
                });

                var difficultySelectorCombo = rightPanel.AddChild(new Gui.Widgets.ComboBox
                {
                    AutoLayout = Gui.AutoLayout.DockTop,
                    Items = Library.EnumerateDifficulties().Select(e => e.Name).ToList(),
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font10",
                    OnSelectedIndexChanged = (sender) =>
                    {
                        Settings.Difficulty = Library.GetDifficulty((sender as Gui.Widgets.ComboBox).SelectedItem);
                    }
                }) as Gui.Widgets.ComboBox;

                difficultySelectorCombo.SelectedIndex = difficultySelectorCombo.Items.IndexOf("Normal");
            }
            else
                rightPanel.AddChild(new Widget
                {
                    Text = "Difficulty: " + Settings.Difficulty.Name,
                    Font = "font10",
                    AutoLayout = AutoLayout.DockTop
                });


            if (_WorldType == WorldType.NewWorld)
            {
                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Caves",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    Font = "font10",
                    TextColor = new Vector4(0, 0, 0, 1),
                });

                var caveOptions = new String[] { "Barely any", "Few", "Normal", "Lots", "Way too many" };
                var caveValues = new int[] { 2, 3, 4, 6, 9 };

                var caveSetting = rightPanel.AddChild(new Gui.Widgets.ComboBox
                {
                    AutoLayout = AutoLayout.DockTop,
                    Items = new List<string>(caveOptions),
                    Font = "font10",
                    TextColor = new Vector4(0, 0, 0, 1),
                    OnSelectedIndexChanged = (sender) =>
                    {
                        Settings.NumCaveLayers = caveValues[(sender as Gui.Widgets.ComboBox).SelectedIndex];
                    }
                }) as Gui.Widgets.ComboBox;

                caveSetting.SelectedIndex = caveSetting.Items.IndexOf("Normal");
            }
            else
                rightPanel.AddChild(new Widget
                {
                    Text = "Caves: " + Settings.Difficulty.Name,
                    Font = "font10",
                    AutoLayout = AutoLayout.DockTop
                });

            if (_WorldType == WorldType.NewWorld)
            {
                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Slices",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    Font = "font10",
                    TextColor = new Vector4(0, 0, 0, 1),
                });

                var zLevelSetting = rightPanel.AddChild(new Gui.Widgets.ComboBox
                {
                    AutoLayout = AutoLayout.DockTop,
                    Items = new List<string>(new string[] { "16", "64", "128" }),
                    Font = "font10",
                    TextColor = new Vector4(0, 0, 0, 1),
                    OnSelectedIndexChanged = (sender) =>
                    {
                        switch ((sender as Gui.Widgets.ComboBox).SelectedItem)
                        {
                            case "16": Settings.zLevels = 1; break;
                            case "64": Settings.zLevels = 4; break;
                            case "128": Settings.zLevels = 8; break;
                        }
                    }
                }) as Gui.Widgets.ComboBox;

                zLevelSetting.SelectedIndex = 1;
            }
            else
                rightPanel.AddChild(new Widget
                {
                    Text = "Z Levels: " + Settings.zLevels * VoxelConstants.ChunkSizeY,
                    Font = "font10",
                    AutoLayout = AutoLayout.DockTop
                });


            #region Name 

            if (_WorldType == WorldType.NewWorld)
            {
                rightPanel.AddChild(new Gui.Widget
                {
                    MinimumSize = new Point(64, 0),
                    Text = "Company Name",
                    Font = "font8",
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Center
                });

                var nameRow = rightPanel.AddChild(new Widget
                {
                    MinimumSize = new Point(0, 32),
                    AutoLayout = AutoLayout.DockTop,
                    Padding = new Margin(0, 0, 2, 2)
                });

                nameRow.AddChild(new Gui.Widgets.Button
                {
                    Text = "?",
                    AutoLayout = AutoLayout.DockRight,
                    Border = "border-button",
                    OnClick = (sender, args) =>
                    {
                        var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.company);
                        NameField.Text = TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
                    }
                });

                NameField = nameRow.AddChild(new Gui.Widgets.EditableTextField
                {
                    Text = Settings.Company.Name,
                    AutoLayout = AutoLayout.DockFill
                }) as Gui.Widgets.EditableTextField;
            }
            else
                rightPanel.AddChild(new Gui.Widget
                {
                    MinimumSize = new Point(64, 0),
                    Text = "Company Name: " + Settings.Company.Name,
                    Font = "font10",
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Center
                });

            #endregion

            #region Motto

            if (_WorldType == WorldType.NewWorld)
            {
                rightPanel.AddChild(new Widget
                {
                    MinimumSize = new Point(64, 0),
                    Text = "Company Motto",
                    Font = "font8",
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Center
                });

                var mottoRow = rightPanel.AddChild(new Widget
                {
                    MinimumSize = new Point(0, 32),
                    AutoLayout = AutoLayout.DockTop,
                    Padding = new Margin(0, 0, 2, 2)
                });

                mottoRow.AddChild(new Gui.Widgets.Button
                {
                    Text = "?",
                    AutoLayout = AutoLayout.DockRight,
                    Border = "border-button",
                    OnClick = (sender, args) =>
                    {
                        var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.mottos);
                        MottoField.Text = TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
                        // Todo: Doesn't automatically invalidate when text changed??
                        MottoField.Invalidate();
                    }
                });

                MottoField = mottoRow.AddChild(new Gui.Widgets.EditableTextField
                {
                    Text = Settings.Company.Motto,
                    AutoLayout = AutoLayout.DockFill
                }) as Gui.Widgets.EditableTextField;

            }
            else
                rightPanel.AddChild(new Widget
                {
                    MinimumSize = new Point(64, 0),
                    Text = "Company Motto: " + Settings.Company.Motto,
                    Font = "font10",
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Center
                });


            #endregion


            #region Logo

            if (_WorldType == WorldType.NewWorld)
            {
                rightPanel.AddChild(new Widget
                {
                    MinimumSize = new Point(64, 0),
                    Text = "Company Logo",
                    Font = "font10",
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Left,
                    TextVerticalAlign = VerticalAlign.Center
                });

                var logoRow = rightPanel.AddChild(new Widget
                {
                    MinimumSize = new Point(0, 64),
                    AutoLayout = AutoLayout.DockTop,
                    Padding = new Margin(0, 0, 2, 2)
                });

                CompanyLogoDisplay = logoRow.AddChild(new Gui.Widgets.CompanyLogo
                {
                    AutoLayout = AutoLayout.DockLeft,
                    MinimumSize = new Point(64, 64),
                    MaximumSize = new Point(64, 64),
                    CompanyInformation = Settings.Company
                }) as Gui.Widgets.CompanyLogo;

                var rightBox = logoRow.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockFill
                });

                var bgBox = rightBox.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(32, 32),
                });

                bgBox.AddChild(new Widget
                {
                    Text = "BG:",
                    AutoLayout = AutoLayout.DockLeft
                });

                bgBox.AddChild(new Widget
                {
                    Background = Settings.Company.LogoBackground,
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    AutoLayout = AutoLayout.DockLeft,
                    OnClick = (sender, args) =>
                    {
                        var source = GuiRoot.GetTileSheet("company-logo-background") as Gui.TileSheet;
                        var chooser = new Gui.Widgets.GridChooser
                        {
                            ItemSource = Enumerable.Range(0, source.Columns * source.Rows)
                                .Select(i => new Widget
                                {
                                    Background = new TileReference("company-logo-background", i)
                                }),
                            OnClose = (s2) =>
                            {
                                var gc = s2 as Gui.Widgets.GridChooser;
                                if (gc.DialogResult == Gui.Widgets.GridChooser.Result.OKAY &&
                                    gc.SelectedItem != null)
                                {
                                    sender.Background = gc.SelectedItem.Background;
                                    sender.Invalidate();
                                    Settings.Company.LogoBackground = gc.SelectedItem.Background;
                                    CompanyLogoDisplay.Invalidate();
                                }
                            },
                            PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                        };
                        GuiRoot.ShowModalPopup(chooser);
                    }
                });

                bgBox.AddChild(new Widget
                {
                    Background = new TileReference("basic", 1),
                    BackgroundColor = Settings.Company.LogoBackgroundColor,
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    AutoLayout = AutoLayout.DockLeft,
                    OnClick = (sender, args) =>
                    {
                        var chooser = new Gui.Widgets.GridChooser
                        {
                            ItemSize = new Point(16, 16),
                            ItemSpacing = new Point(4, 4),
                            ItemSource = EnumerateDefaultColors()
                                .Select(c => new Widget
                                {
                                    Background = new TileReference("basic", 1),
                                    BackgroundColor = new Vector4(c.ToVector3(), 1),
                                }),
                            OnClose = (s2) =>
                            {
                                var gc = s2 as Gui.Widgets.GridChooser;
                                if (gc.DialogResult == Gui.Widgets.GridChooser.Result.OKAY &&
                                    gc.SelectedItem != null)
                                {
                                    sender.BackgroundColor = gc.SelectedItem.BackgroundColor;
                                    sender.Invalidate();
                                    Settings.Company.LogoBackgroundColor = gc.SelectedItem.BackgroundColor;
                                    CompanyLogoDisplay.Invalidate();
                                }
                            },
                            PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                        };
                        GuiRoot.ShowModalPopup(chooser);
                    }
                });

                var fgBox = rightBox.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockFill,
                    MinimumSize = new Point(32, 32),
                });

                fgBox.AddChild(new Widget
                {
                    Text = "FG:",
                    AutoLayout = AutoLayout.DockLeft
                });

                fgBox.AddChild(new Widget
                {
                    Background = Settings.Company.LogoSymbol,
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    AutoLayout = AutoLayout.DockLeft,
                    OnClick = (sender, args) =>
                    {
                        var source = GuiRoot.GetTileSheet("company-logo-symbol") as Gui.TileSheet;
                        var chooser = new Gui.Widgets.GridChooser
                        {
                            ItemSource = Enumerable.Range(0, source.Columns * source.Rows)
                                .Select(i => new Widget
                                {
                                    Background = new TileReference("company-logo-symbol", i)
                                }),
                            OnClose = (s2) =>
                            {
                                var gc = s2 as Gui.Widgets.GridChooser;
                                if (gc.DialogResult == Gui.Widgets.GridChooser.Result.OKAY &&
                                    gc.SelectedItem != null)
                                {
                                    sender.Background = gc.SelectedItem.Background;
                                    sender.Invalidate();
                                    Settings.Company.LogoSymbol = gc.SelectedItem.Background;
                                    CompanyLogoDisplay.Invalidate();
                                }
                            },
                            PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                        };
                        GuiRoot.ShowModalPopup(chooser);
                    }
                });

                fgBox.AddChild(new Widget
                {
                    Background = new TileReference("basic", 1),
                    BackgroundColor = Settings.Company.LogoSymbolColor,
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    AutoLayout = AutoLayout.DockLeft,
                    OnClick = (sender, args) =>
                    {
                        var chooser = new Gui.Widgets.GridChooser
                        {
                            ItemSize = new Point(16, 16),
                            ItemSpacing = new Point(4, 4),
                            ItemSource = EnumerateDefaultColors()
                                .Select(c => new Widget
                                {
                                    Background = new TileReference("basic", 1),
                                    BackgroundColor = new Vector4(c.ToVector3(), 1),
                                }),
                            OnClose = (s2) =>
                            {
                                var gc = s2 as Gui.Widgets.GridChooser;
                                if (gc.DialogResult == Gui.Widgets.GridChooser.Result.OKAY &&
                                    gc.SelectedItem != null)
                                {
                                    sender.BackgroundColor = gc.SelectedItem.BackgroundColor;
                                    sender.Invalidate();
                                    Settings.Company.LogoSymbolColor = gc.SelectedItem.BackgroundColor;
                                    CompanyLogoDisplay.Invalidate();
                                }
                            },
                            PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                        };
                        GuiRoot.ShowModalPopup(chooser);
                    }
                });
            }
            else
                CompanyLogoDisplay = rightPanel.AddChild(new Gui.Widgets.CompanyLogo
                {
                    AutoLayout = AutoLayout.FloatTop,
                    MinimumSize = new Point(64, 64),
                    MaximumSize = new Point(64, 64),
                    CompanyInformation = Settings.Company
                }) as Gui.Widgets.CompanyLogo;

            #endregion

            if (_WorldType == WorldType.SavedWorld)
            {
                var saveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Name;
                var saveGame = SaveGame.LoadMetaFromDirectory(saveName);
                if (saveGame != null)
                {

                    rightPanel.AddChild(new Gui.Widget
                    {
                        AutoLayout = AutoLayout.DockFill,
                        Font = "font10",
                        Text = saveGame.Metadata.DescriptionString
                    });
                }
            }
        
            Preview = MainPanel.AddChild(new WorldGeneratorPreview(Game.GraphicsDevice)
            {
                Border = "border-thin",
                AutoLayout = Gui.AutoLayout.DockFill,
                Overworld = Settings,
                Hidden = true,
                OnLayout = (sender) =>
                {
                    //sender.Rect = new Rectangle(sender.Rect.X, sender.Rect.Y, sender.Rect.Width, GenerationProgress.Rect.Bottom - sender.Rect.Y);
                }
            }) as WorldGeneratorPreview;

            GuiRoot.RootItem.Layout();
            #endregion

            #endregion

            switch (_WorldType)
            {
                case WorldType.NewWorld:
                    RestartGeneration();
                    break;

                case WorldType.SavedWorld:
                    // Setup a dummy generator.
                    Generator = new OverworldGenerator(Settings, false);
                    Generator.LoadDummy();
                    Preview.SetGenerator(Generator, Settings);
                    break;
            }

            IsInitialized = true;
            base.OnEnter();
        }

        private void LaunchNewGame()
        {
            if (Settings.InstanceSettings == null || Settings.Natives == null)
                return; // Someone crashed here.

            GameStateManager.ClearState();
            GameStateManager.PushState(new LoadState(Game, Settings, LoadTypes.UseExistingOverworld));
        }


        private IEnumerable<Color> EnumerateDefaultColors()
        {
            for (int h = 0; h < 255; h += 16)
                for (int v = 0; v < 255; v += 16)
                    for (int s = 0; s < 255; s += 16)
                        yield return new HSLColor((float)h, (float)s, (float)v);
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
                GuiRoot.HandleInput(@event.Message, @event.Args);

            GenerationProgress.Text = Generator.LoadingMessage;
            GenerationProgress.Percentage = Generator.Progress * 100.0f;

            // Enable or disable start button based on Generator state.

            GuiRoot.Update(gameTime.ToRealTime());

            if (Generator.CurrentState == OverworldGenerator.GenerationState.Finished)
            {
                Preview.Hidden = false;
                Preview.Update(gameTime);
                Preview.RenderPreview(Game.GraphicsDevice);
            }

            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

            if (Generator.CurrentState == OverworldGenerator.GenerationState.Finished)
            {
                Preview.DrawPreview();
                GuiRoot.MousePointer = new MousePointer("mouse", 1, 0);
            }

            GuiRoot.RedrawPopups();
            GuiRoot.Postdraw();
            GuiRoot.DrawMouse();

            base.Render(gameTime);
        }

        public override void OnPopped()
        {
            Preview.Close();
            base.OnPopped();
        }
    }
}