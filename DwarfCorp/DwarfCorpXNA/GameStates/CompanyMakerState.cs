// CompanyMakerState.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum;
using Gum.Widgets;
using System.Linq;

namespace DwarfCorp
{
    public class CompanyInformation
    {
        public TileReference LogoBackground = new TileReference("company-logo-background", 0);
        public Vector4 LogoBackgroundColor = Vector4.One;
        public TileReference LogoSymbol = new TileReference("company-logo-symbol", 0);
        public Vector4 LogoSymbolColor = Vector4.One;
        public string Name = "Graybeard & Sons";
        public string Motto = "My beard is in the workd!";
    }
}

namespace DwarfCorp.GameStates
{
    /// <summary>
    /// This game state allows the player to design their own dwarf company.
    /// </summary>
    public class CompanyMakerState : GameState
    {
        private Gum.Root GuiRoot;
        private Gum.Widgets.EditableTextField NameField;
        private Gum.Widgets.EditableTextField MottoField;
        private NewGui.CompanyLogo CompanyLogoDisplay;

        // Todo: This should be passed to the next screen when create is clicked, and so on. Instead
        //  of being static... there should be a state object that is slowly built by each screen until
        //  it finally gets passed to the game.
        // Todo: Also need to save this when the game is saved.
        public static CompanyInformation CompanyInformation { get; set; }

        // Does not actually set these.
        //public static NamedImageFrame CompanyLogo { get; set; }
        //public static Color CompanyColor { get; set; }

        public CompanyMakerState(DwarfGame game, GameStateManager stateManager) :
            base(game, "CompanyMakerState", stateManager)
        {
            CompanyInformation = new CompanyInformation();
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gum.Root(new Point(640, 480), DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

            // CONSTRUCT GUI HERE...
            var mainPanel = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                Rect = GuiRoot.VirtualScreen,
                Border = "border-fancy",
                Text = "Create a Company",
                Padding = new Margin(4, 4, 4, 4),
                InteriorMargin = new Margin(24,0,0,0),
                TextSize = 2
            });

            mainPanel.AddChild(new Gum.Widgets.Button
            {
                Text = "CREATE!",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    // Grab string values from widgets!
                    CompanyInformation.Name = NameField.Text;
                    CompanyInformation.Motto = MottoField.Text;

                    // Why are they stored as statics on this class???
                    StateManager.PushState(new NewGameChooseWorldState(Game, Game.StateManager));
                },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            mainPanel.AddChild(new Gum.Widgets.Button
            {
                Text = "BACK",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    StateManager.PopState();
                },
                AutoLayout = AutoLayout.FloatBottomRight,
                OnLayout = s => s.Rect.X -= 128 // Hack to keep it from floating over the other button.
            });

            #region Name 
            var nameRow = mainPanel.AddChild(new Widget
            {
                MinimumSize = new Point(0, 24),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0,0,2,2)
            });

            nameRow.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(64, 0),
                Text = "Name",
                AutoLayout = AutoLayout.DockLeft,
                TextHorizontalAlign = HorizontalAlign.Right,
                TextVerticalAlign = VerticalAlign.Center
            });

            nameRow.AddChild(new Gum.Widgets.Button
            {
                Text = "Randomize",
                AutoLayout = AutoLayout.DockRight,
                Border = "border-button",
                OnClick = (sender, args) =>
                    {
                        var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.company_exploration);
                        NameField.Text = TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
                        // Todo: Doesn't automatically invalidate when text changed??
                        NameField.Invalidate();
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

            mottoRow.AddChild(new Widget
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

            CompanyLogoDisplay = logoRow.AddChild(new NewGui.CompanyLogo
                {
                    AutoLayout = AutoLayout.DockLeft,
                    MinimumSize = new Point(64,64),
                    MaximumSize = new Point(64,64),
                    CompanyInformation = CompanyInformation
                }) as NewGui.CompanyLogo;

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
                            var source = GuiRoot.GetTileSheet("company-logo-background") as Gum.TileSheet;
                            var chooser = new NewGui.GridChooser
                            {
                                ItemSource = Enumerable.Range(0, source.Columns * source.Rows)
                                    .Select(i => new Widget {
                                        Background = new TileReference("company-logo-background", i)
                                    }),
                                OnClose = (s2) =>
                                    {
                                        var gc = s2 as NewGui.GridChooser;
                                        if (gc.DialogResult == NewGui.GridChooser.Result.OKAY &&
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
                            GuiRoot.ShowPopup(chooser);
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
                    var chooser = new NewGui.GridChooser
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
                            var gc = s2 as NewGui.GridChooser;
                            if (gc.DialogResult == NewGui.GridChooser.Result.OKAY &&
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
                    GuiRoot.ShowPopup(chooser);
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
                    var source = GuiRoot.GetTileSheet("company-logo-symbol") as Gum.TileSheet;
                    var chooser = new NewGui.GridChooser
                    {
                        ItemSource = Enumerable.Range(0, source.Columns * source.Rows)
                            .Select(i => new Widget
                            {
                                Background = new TileReference("company-logo-symbol", i)
                            }),
                        OnClose = (s2) =>
                        {
                            var gc = s2 as NewGui.GridChooser;
                            if (gc.DialogResult == NewGui.GridChooser.Result.OKAY &&
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
                    GuiRoot.ShowPopup(chooser);
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
                    var chooser = new NewGui.GridChooser
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
                            var gc = s2 as NewGui.GridChooser;
                            if (gc.DialogResult == NewGui.GridChooser.Result.OKAY &&
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
                    GuiRoot.ShowPopup(chooser);
                }
            });


            #endregion

            GuiRoot.RootItem.Layout();

            // Must be true or Render will not be called.
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

            GuiRoot.Update(gameTime.ToGameTime());
            base.Update(gameTime);
        }
        
        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}