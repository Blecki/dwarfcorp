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

        public Action OnVerified;

        private Gui.Widgets.EditableTextField NameField;
        private Gui.Widgets.EditableTextField MottoField;
        private Gui.Widgets.CompanyLogo CompanyLogoDisplay;
        
        public enum PanelStates
        {
            Generate,
            Launch
        }

        private PanelStates PanelState = PanelStates.Generate;
        
        public WorldGeneratorState(DwarfGame Game, Overworld Settings, PanelStates StartingState) :
            base(Game)
        {
            this.PanelState = StartingState;
            this.Settings = Settings;
        }       

        private void RestartGeneration()
        {
            if (Generator != null)
                Generator.Abort();

            if (Settings.Natives != null)
                Settings.Natives.Clear();

            Settings.Size = Settings.GenerationSettings.SizeInChunks;
            Generator = new OverworldGenerator(Settings, true);
            if (Preview != null) Preview.SetGenerator(Generator, Settings);

            GuiRoot.RootItem.GetChild(0).Text = Settings.Name;
            GuiRoot.RootItem.GetChild(0).Invalidate();
            GuiRoot.MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23);
            Preview.Hidden = true;
            GenerationProgress.Hidden = false;

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

            var rightPanel = MainPanel.AddChild(new Widget
            {
                MinimumSize = new Point(256, 0),
                Padding = new Margin(2, 2, 2, 2),
                AutoLayout = AutoLayout.DockRight
            });

            rightPanel.AddChild(new Gui.Widget
            {
                Text = "Back",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    GameStateManager.ClearState();
                    GameStateManager.PushState(new MainMenuState(Game));
                }
            });

            rightPanel.AddChild(new Widget
            {
                Text = "Factions",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    SuppressEnter = true;
                    GameStateManager.PushState(new FactionViewState(GameState.Game, Settings));
                }
            });


            if (PanelState == PanelStates.Generate)
            {
                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Randomize",
                    Border = "border-button",
                    ChangeColorOnHover = true,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font16",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    OnClick = (sender, args) =>
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is regenerating the world.");
                        Settings.Seed = MathFunctions.RandInt(Int32.MinValue, Int32.MaxValue);
                        Settings.Name = Overworld.GetRandomWorldName();
                        RestartGeneration();
                    }
                });


                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Save World",
                    Border = "border-button",
                    ChangeColorOnHover = true,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font16",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    OnClick = (sender, args) =>
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is saving the world.");
                        if (Generator.CurrentState != OverworldGenerator.GenerationState.Finished)
                            GuiRoot.ShowTooltip(GuiRoot.MousePosition, "Generator is not finished.");
                        else
                        {
                            global::System.IO.DirectoryInfo worldDirectory = global::System.IO.Directory.CreateDirectory(DwarfGame.GetWorldDirectory() + global::System.IO.Path.DirectorySeparatorChar + Settings.Name);
                            var file = new NewOverworldFile(Game.GraphicsDevice, Settings);
                            file.WriteFile(worldDirectory.FullName);
                            GuiRoot.ShowModalPopup(GuiRoot.ConstructWidget(new Gui.Widgets.Popup
                            {
                                Text = "File saved."
                            }));
                        }
                    }
                });

                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Advanced",
                    Border = "border-button",
                    ChangeColorOnHover = true,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font16",
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
                    Text = "Edit Embarkment",
                    Border = "border-button",
                    ChangeColorOnHover = true,
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font16",
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

            StartButton = rightPanel.AddChild(new Gui.Widget
            {
                Text = "Launch",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockBottom,
                OnClick = (sender, args) =>
                {
                    Settings.Company.Name = NameField.Text;
                    Settings.Company.Motto = MottoField.Text;
                    Settings.InstanceSettings.InitalEmbarkment = new Embarkment(Settings);
                    Settings.PlayerCorporationFunds = Settings.Difficulty.StartingFunds;
                    Settings.Natives.FirstOrDefault(n => n.Name == "Player").PrimaryColor = new Color(Settings.Company.LogoBackgroundColor);

                    var saveName = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Settings.Name;
                    var saveGame = SaveGame.LoadMetaFromDirectory(saveName);
                    if (saveGame != null)
                    {
                        DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is loading a saved game.");
                        Settings.InstanceSettings.LoadType = LoadType.LoadFromFile;

                        GameStateManager.ClearState();
                        GameStateManager.PushState(new LoadState(Game, Settings, LoadTypes.UseExistingOverworld));
                    }
                    else
                    {
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

            if (PanelState == PanelStates.Generate)
            {
                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Difficulty",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    Font = "font8",
                    TextColor = new Vector4(0, 0, 0, 1)
                });

                var difficultySelectorCombo = rightPanel.AddChild(new Gui.Widgets.ComboBox
                {
                    AutoLayout = Gui.AutoLayout.DockTop,
                    Items = Library.EnumerateDifficulties().Select(e => e.Name).ToList(),
                    TextColor = new Vector4(0, 0, 0, 1),
                    Font = "font8",
                    OnSelectedIndexChanged = (sender) =>
                    {
                        Settings.Difficulty = Library.GetDifficulty((sender as Gui.Widgets.ComboBox).SelectedItem);
                    }
                }) as Gui.Widgets.ComboBox;

                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Caves",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    Font = "font8",
                    TextColor = new Vector4(0, 0, 0, 1),
                });

                var layerSetting = rightPanel.AddChild(new Gui.Widgets.ComboBox
                {
                    AutoLayout = AutoLayout.DockTop,
                    Items = new List<string>(new string[] { "Barely any", "Few", "Normal", "Lots", "Way too many" }),
                    Font = "font8",
                    TextColor = new Vector4(0, 0, 0, 1),
                    OnSelectedIndexChanged = (sender) =>
                    {
                        switch ((sender as Gui.Widgets.ComboBox).SelectedItem)
                        {
                            case "Barely any": Settings.NumCaveLayers = 2; break;
                            case "Few": Settings.NumCaveLayers = 3; break;
                            case "Normal": Settings.NumCaveLayers = 4; break;
                            case "Lots": Settings.NumCaveLayers = 6; break;
                            case "Way too many": Settings.NumCaveLayers = 9; break;
                        }
                    }
                }) as Gui.Widgets.ComboBox;

                rightPanel.AddChild(new Gui.Widget
                {
                    Text = "Slices",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    Font = "font8",
                    TextColor = new Vector4(0, 0, 0, 1),
                });

                var zLevelSetting = rightPanel.AddChild(new Gui.Widgets.ComboBox
                {
                    AutoLayout = AutoLayout.DockTop,
                    Items = new List<string>(new string[] { "16", "64", "128" }),
                    Font = "font8",
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
                difficultySelectorCombo.SelectedIndex = difficultySelectorCombo.Items.IndexOf("Normal");
                layerSetting.SelectedIndex = layerSetting.Items.IndexOf("Normal");

                #region Name 

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
                #endregion

                #region Motto

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
                #endregion

                #region Logo

                rightPanel.AddChild(new Widget
                {
                    MinimumSize = new Point(64, 0),
                    Text = "Company Logo",
                    Font = "font8",
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


                #endregion
            }



            GenerationProgress = MainPanel.AddChild(new Gui.Widgets.ProgressBar
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Font = "font10",
                TextColor = new Vector4(1,1,1,1)                
            }) as Gui.Widgets.ProgressBar;


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

            switch (PanelState)
            {
                case PanelStates.Generate:
                    RestartGeneration();
                    break;

                case PanelStates.Launch:
                    // Setup a dummy generator.
                    Generator = new OverworldGenerator(Settings, false);
                    Generator.LoadDummy();
                    Preview.SetGenerator(Generator, Settings);
                    break;
            }

            Settings.InstanceSettings.InitalEmbarkment = new Embarkment(Settings);

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
                for (int v = 64; v < 255; v += 64)
                    for (int s = 128; s < 255; s += 32)
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
                GenerationProgress.Hidden = true;
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