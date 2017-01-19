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

        public TextGenerator TextGenerator { get; set; }
        public static Color DefaultColor = Color.DarkRed;
        public static string DefaultName = "Greybeard & Sons";
        public static string DefaultMotto = "My beard is in the work!";
        public static NamedImageFrame DefaultLogo = new NamedImageFrame(ContentPaths.Logos.grebeardlogo);
        public static string CompanyName { get; set; }
        public static string CompanyMotto { get; set; }
        public static NamedImageFrame CompanyLogo { get; set; }
        public static Color CompanyColor { get; set; }

        public CompanyMakerState(DwarfGame game, GameStateManager stateManager) :
            base(game, "CompanyMakerState", stateManager)
        {
            CompanyName = DefaultName;
            CompanyMotto = DefaultMotto;
            CompanyLogo = DefaultLogo;
            CompanyColor = DefaultColor;
            TextGenerator = new TextGenerator();
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInput.GetInputQueue();

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

            mainPanel.AddChild(new Widget
            {
                Text = "CREATE!",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    
                    StateManager.PopState();
                },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            // Todo: Main menu needs to be fixed so going back actually goes back to the first menu.
            mainPanel.AddChild(new Widget
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

            nameRow.AddChild(new Widget
            {
                MinimumSize = new Point(64, 0),
                Text = "Name",
                AutoLayout = AutoLayout.DockLeft
            });

            nameRow.AddChild(new Widget
            {
                Text = "Randomize",
                AutoLayout = AutoLayout.DockRight,
                Border = "border-button",
                OnClick = (sender, args) =>
                    {
                        var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.company_exploration);
                        CompanyName = TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
                        NameField.Text = CompanyName;
                        // Todo: Doesn't automatically invalidate when text changed??
                        NameField.Invalidate();
                    }
            });

            NameField = nameRow.AddChild(new EditableTextField
                {
                    Text = DefaultName,
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
                AutoLayout = AutoLayout.DockLeft
            });

            mottoRow.AddChild(new Widget
            {
                Text = "Randomize",
                AutoLayout = AutoLayout.DockRight,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.mottos);
                    CompanyMotto = TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
                    MottoField.Text = CompanyName;
                    // Todo: Doesn't automatically invalidate when text changed??
                    MottoField.Invalidate();
                }
            });

            MottoField = mottoRow.AddChild(new EditableTextField
            {
                Text = DefaultMotto,
                AutoLayout = AutoLayout.DockFill
            }) as EditableTextField;
            #endregion

            GuiRoot.RootItem.Layout();

            // Must be true or Render will not be called.
            IsInitialized = true;

            base.OnEnter();
            return;

        /*    
           

            Label companyLogoLabel = new Label(GUI, Layout, "Logo", GUI.DefaultFont);
            Layout.SetComponentPosition(companyLogoLabel, 0, 3, 1, 1);

            CompanyLogoPanel = new ImagePanel(GUI, Layout, CompanyLogo)
            {
                KeepAspectRatio = true,
                AssetName = CompanyLogo.AssetName
            };
            Layout.SetComponentPosition(CompanyLogoPanel, 1, 3, 1, 1);


            Button selectorButton = new Button(GUI, Layout, "Select", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Load a custom company logo"
            };
            Layout.SetComponentPosition(selectorButton, 2, 3, 1, 1);
            selectorButton.OnClicked += selectorButton_OnClicked;

            Label companyColorLabel = new Label(GUI, Layout, "Color", GUI.DefaultFont);
            Layout.SetComponentPosition(companyColorLabel, 0, 4, 1, 1);

            CompanyColorPanel = new ColorPanel(GUI, Layout) {CurrentColor = DefaultColor};
            Layout.SetComponentPosition(CompanyColorPanel, 1, 4, 1, 1);
            CompanyColorPanel.OnClicked += CompanyColorPanel_OnClicked;


            Button apply = new Button(GUI, Layout, "Continue", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Check));
            Layout.SetComponentPosition(apply, 2, 9, 1, 1);

            apply.OnClicked += apply_OnClicked;

            Button back = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow));
            Layout.SetComponentPosition(back, 1, 9, 1, 1);

            back.OnClicked += back_onClicked;

            base.OnEnter();
         * */
        }

        public List<Color> GenerateDefaultColors()
        {
            List<Color> toReturn = new List<Color>();
            for (int h = 0; h < 255; h += 16)
            {
                for (int v = 64; v < 255; v += 64)
                {
                    for (int s = 128; s < 255; s += 32)
                    {
                        toReturn.Add(new HSLColor((float) h, (float) s, (float) v));
                    }

                }
            }
        
            return toReturn;
        }

        
        private void apply_OnClicked()
        {
        //    CompanyName = CompanyNameEdit.Text;
        //    CompanyMotto = CompanyMottoEdit.Text;
        //    CompanyLogo = new NamedImageFrame(CompanyLogoPanel.AssetName, CompanyLogoPanel.Image.SourceRect);
            StateManager.PopState();
        }

        private void back_onClicked()
        {
            StateManager.PopState();
        }

        private void companyNameEdit_OnTextModified(string arg)
        {
            CompanyName = arg;
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInput.GetInputQueue())
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