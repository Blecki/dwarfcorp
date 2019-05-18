using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System.Linq;

namespace DwarfCorp.GameStates
{
    // Todo: Merge settings with next screen??
    public class CompanyMakerState : GameState
    {
        private Gui.Root GuiRoot;
        private Gui.Widgets.EditableTextField NameField;
        private Gui.Widgets.EditableTextField MottoField;
        private Gui.Widgets.CompanyLogo CompanyLogoDisplay;

        private CompanyInformation CompanyInformation;

        public CompanyMakerState(DwarfGame game) :
            base(game)
        {
            CompanyInformation = new CompanyInformation();
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);

            Rectangle rect = GuiRoot.RenderData.VirtualScreen;
            rect.Inflate(-rect.Width / 3, -rect.Height / 3);
            var mainPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = rect,
                MinimumSize = new Point(512, 256),
                AutoLayout = AutoLayout.FloatCenter,
                Border = "border-fancy",
                Padding = new Margin(4, 4, 4, 4),
                InteriorMargin = new Margin(2,0,0,0),
                TextSize = 1,
                Font = "font10"
            });

            mainPanel.AddChild(new Gui.Widgets.Button
            {
                Text = "Create!",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    CompanyInformation.Name = NameField.Text;
                    CompanyInformation.Motto = MottoField.Text;

                    var settings = new OverworldGenerationSettings()
                    {
                        Company = CompanyInformation,
                    };

                    GameStateManager.PushState(new WorldGeneratorState(Game, settings, WorldGeneratorState.PanelStates.Generate));
                },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            mainPanel.AddChild(new Gui.Widgets.Button
            {
                Text = "< Back",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                Font = "font16",
                OnClick = (sender, args) =>
                {
                    GameStateManager.PopState();
                },
                AutoLayout = AutoLayout.FloatBottomLeft,
            });

            #region Name 

            mainPanel.AddChild(new Widget()
            {
                Text = "Create a Company",
                Font = "font16",
                AutoLayout = AutoLayout.DockTop,
            });

            var nameRow = mainPanel.AddChild(new Widget
            {
                MinimumSize = new Point(0, 24),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0,0,2,2)
            });

            nameRow.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(64, 0),
                Text = "Name",
                AutoLayout = AutoLayout.DockLeft,
                TextHorizontalAlign = HorizontalAlign.Right,
                TextVerticalAlign = VerticalAlign.Center
            });

            nameRow.AddChild(new Gui.Widgets.Button
            {
                Text = "Randomize",
                AutoLayout = AutoLayout.DockRight,
                Border = "border-button",
                OnClick = (sender, args) =>
                    {
                        var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.company);
                        NameField.Text = TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
                    }
            });

            NameField = nameRow.AddChild(new EditableTextField
                {
                    Text = CompanyInformation.Name,
                    AutoLayout = AutoLayout.DockFill
                }) as EditableTextField;
            #endregion

            #region Motto
            var mottoRow = mainPanel.AddChild(new Widget
            {
                MinimumSize = new Point(0, 24),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0, 0, 2, 2)
            });

            mottoRow.AddChild(new Widget
            {
                MinimumSize = new Point(64, 0),
                Text = "Motto",
                AutoLayout = AutoLayout.DockLeft,
                TextHorizontalAlign = HorizontalAlign.Right,
                TextVerticalAlign = VerticalAlign.Center
            });

            mottoRow.AddChild(new Button
            {
                Text = "Randomize",
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

            MottoField = mottoRow.AddChild(new EditableTextField
            {
                Text = CompanyInformation.Motto,
                AutoLayout = AutoLayout.DockFill
            }) as EditableTextField;
            #endregion

            #region Logo

            var logoRow = mainPanel.AddChild(new Widget
            {
                MinimumSize = new Point(0, 64),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0, 0, 2, 2)
            });

            CompanyLogoDisplay = logoRow.AddChild(new Gui.Widgets.CompanyLogo
                {
                    AutoLayout = AutoLayout.DockLeft,
                    MinimumSize = new Point(64,64),
                    MaximumSize = new Point(64,64),
                    CompanyInformation = CompanyInformation
                }) as Gui.Widgets.CompanyLogo;

            logoRow.AddChild(new Widget
            {
                Text = "BG:",
                AutoLayout = AutoLayout.DockLeft
            });

            logoRow.AddChild(new Widget
                {
                    Background = CompanyInformation.LogoBackground,
                    MinimumSize = new Point(32,32),
                    MaximumSize = new Point(32,32),
                    AutoLayout = AutoLayout.DockLeft,
                    OnClick = (sender, args) =>
                        {
                            var source = GuiRoot.GetTileSheet("company-logo-background") as Gui.TileSheet;
                            var chooser = new Gui.Widgets.GridChooser
                            {
                                ItemSource = Enumerable.Range(0, source.Columns * source.Rows)
                                    .Select(i => new Widget {
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
                                            CompanyInformation.LogoBackground = gc.SelectedItem.Background;
                                            CompanyLogoDisplay.Invalidate();
                                        }
                                    },
                                PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                            };
                            GuiRoot.ShowModalPopup(chooser);
                        }
                });

            logoRow.AddChild(new Widget
            {
                Background = new TileReference("basic", 1),
                BackgroundColor = CompanyInformation.LogoBackgroundColor,
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = AutoLayout.DockLeft,
                OnClick = (sender, args) =>
                {
                    var chooser = new Gui.Widgets.GridChooser
                    {
                        ItemSize = new Point(16,16),
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
                                CompanyInformation.LogoBackgroundColor = gc.SelectedItem.BackgroundColor;
                                CompanyLogoDisplay.Invalidate();
                            }
                        },
                        PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                    };
                    GuiRoot.ShowModalPopup(chooser);
                }
            });

            logoRow.AddChild(new Widget
            {
                Text = "FG:",
                AutoLayout = AutoLayout.DockLeft
            });

            logoRow.AddChild(new Widget
            {
                Background = CompanyInformation.LogoSymbol,
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
                                CompanyInformation.LogoSymbol = gc.SelectedItem.Background;
                                CompanyLogoDisplay.Invalidate();
                            }
                        },
                        PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                    };
                    GuiRoot.ShowModalPopup(chooser);
                }
            });

            logoRow.AddChild(new Widget
            {
                Background = new TileReference("basic", 1),
                BackgroundColor = CompanyInformation.LogoSymbolColor,
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
                                CompanyInformation.LogoSymbolColor = gc.SelectedItem.BackgroundColor;
                                CompanyLogoDisplay.Invalidate();
                            }
                        },
                        PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                    };
                    GuiRoot.ShowModalPopup(chooser);
                }
            });


            #endregion

            GuiRoot.RootItem.Layout();

            IsInitialized = true;

            base.OnEnter();
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
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToRealTime());
            base.Update(gameTime);
        }
        
        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}