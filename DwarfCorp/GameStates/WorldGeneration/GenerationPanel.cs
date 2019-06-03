using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System;

namespace DwarfCorp.GameStates
{
    public class GenerationPanel : Widget
    {
        private Gui.Widget StartButton;
        private WorldGenerator Generator => GetGenerator();
        private OverworldGenerationSettings Settings;
        private DwarfGame Game;

        public Action RestartGeneration;
        public Func<WorldGenerator> GetGenerator;
        public Action OnVerified;

        private Gui.Widgets.EditableTextField NameField;
        private Gui.Widgets.EditableTextField MottoField;
        private Gui.Widgets.CompanyLogo CompanyLogoDisplay;

        public GenerationPanel(DwarfGame Game, OverworldGenerationSettings Settings)
        {
            this.Settings = Settings;
            this.Game = Game;
        }

        public override void Construct()
        {
            AddChild(new Gui.Widget
            {
                Text = "Randomize",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) => {
                    DwarfGame.LogSentryBreadcrumb("WorldGenerator", "User is regenerating the world.");
                    Settings.Seed = MathFunctions.RandInt(Int32.MinValue, Int32.MaxValue);
                    Settings.Name = OverworldGenerationSettings.GetRandomWorldName();
                    RestartGeneration();
                }
            });

            AddChild(new Gui.Widget
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
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        Root.ShowTooltip(Root.MousePosition, "Generator is not finished.");
                    else
                    {
                        global::System.IO.DirectoryInfo worldDirectory = global::System.IO.Directory.CreateDirectory(DwarfGame.GetWorldDirectory() + global::System.IO.Path.DirectorySeparatorChar + Settings.Name);
                        var file = new NewOverworldFile(Game.GraphicsDevice, Settings);
                        file.WriteFile(worldDirectory.FullName);
                        Root.ShowModalPopup(Root.ConstructWidget(new Gui.Widgets.Popup
                        {
                            Text = "File saved."
                        }));
                    }
                }
            });

            AddChild(new Gui.Widget
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
                    var advancedSettingsEditor = Root.ConstructWidget(new Gui.Widgets.WorldGenerationSettingsDialog
                    {
                        Settings = Settings,
                        OnClose = (s) =>
                        {
                            if ((s as Gui.Widgets.WorldGenerationSettingsDialog).Result == Gui.Widgets.WorldGenerationSettingsDialog.DialogResult.Okay)
                                RestartGeneration();
                        }
                    });

                    Root.ShowModalPopup(advancedSettingsEditor);
                }
            });

            var lastRow = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32)
            });

            lastRow.AddChild(new Gui.Widget
            {
                Text = "Back",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockLeft,
                OnClick = (sender, args) =>
                {
                    Generator.Abort();
                    GameStateManager.PopState();
                }
            });

            StartButton = lastRow.AddChild(new Gui.Widget
            {
                Text = "Launch",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockFill,
                OnClick = (sender, args) =>
                {
                    Settings.Company.Name = NameField.Text;
                    Settings.Company.Motto = MottoField.Text;

                    OnVerified?.Invoke();
                }
            });

            AddChild(new Gui.Widget
            {
                Text = "Difficulty",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0,0,0,1)
            });

            var difficultySelectorCombo = AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Items = Library.EnumerateEmbarkments().Select(e => e.Name).ToList(),
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font8",
                OnSelectedIndexChanged = (sender) =>
                {
                    Settings.InitalEmbarkment = Library.GetEmbarkment((sender as Gui.Widgets.ComboBox).SelectedItem);
                }
            }) as Gui.Widgets.ComboBox;

            AddChild(new Gui.Widget
            {
                Text = "Caves",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
            });

            var layerSetting = AddChild(new Gui.Widgets.ComboBox
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

            AddChild(new Gui.Widget
            {
                Text = "Z Levels",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
            });

            var zLevelSetting = AddChild(new Gui.Widgets.ComboBox
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

            AddChild(new Gui.Widget
            {
                MinimumSize = new Point(64, 0),
                Text = "Company Name",
                Font = "font8",
                AutoLayout = AutoLayout.DockTop,
                TextHorizontalAlign = HorizontalAlign.Left,
                TextVerticalAlign = VerticalAlign.Center
            });

            var nameRow = AddChild(new Widget
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

            AddChild(new Widget
            {
                MinimumSize = new Point(64, 0),
                Text = "Company Motto",
                Font = "font8",
                AutoLayout = AutoLayout.DockTop,
                TextHorizontalAlign = HorizontalAlign.Left,
                TextVerticalAlign = VerticalAlign.Center
            });

            var mottoRow = AddChild(new Widget
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

            AddChild(new Widget
            {
                MinimumSize = new Point(64, 0),
                Text = "Company Logo",
                Font = "font8",
                AutoLayout = AutoLayout.DockTop,
                TextHorizontalAlign = HorizontalAlign.Left,
                TextVerticalAlign = VerticalAlign.Center
            });

            var logoRow = AddChild(new Widget
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
                    var source = Root.GetTileSheet("company-logo-background") as Gui.TileSheet;
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
                    Root.ShowModalPopup(chooser);
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
                    Root.ShowModalPopup(chooser);
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
                    var source = Root.GetTileSheet("company-logo-symbol") as Gui.TileSheet;
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
                    Root.ShowModalPopup(chooser);
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
                    Root.ShowModalPopup(chooser);
                }
            });


            #endregion


            base.Construct();
        }

        private IEnumerable<Color> EnumerateDefaultColors()
        {
            for (int h = 0; h < 255; h += 16)
                for (int v = 64; v < 255; v += 64)
                    for (int s = 128; s < 255; s += 32)
                        yield return new HSLColor((float)h, (float)s, (float)v);
        }
    }
}