using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    public class Button : SillyGUIComponent
    {
        public enum ButtonMode
        {
            ImageButton,
            PushButton
        }

        public Texture2D Image { get; set; }
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public Color PressedTextColor { get; set; }
        public Color HoverTextColor { get; set; }
        public Color HoverTint { get; set; }
        public Color PressedTint { get; set; }
        public Color ToggleTint { get; set; }
        public SpriteFont TextFont { get; set; }
        public bool CanToggle { get; set; }
        public bool IsToggled { get; set; }
        public ButtonMode Mode { get; set; }

        public Button(SillyGUI gui, SillyGUIComponent parent, string text, SpriteFont textFont, ButtonMode mode, Texture2D image) :
            base(gui, parent)
        {
            Text = text;
            Image = image;
            TextColor = gui.DefaultTextColor;
            HoverTextColor = Color.DarkRed;
            HoverTint = new Color(200, 200, 180);
            PressedTextColor = Color.Red;
            PressedTint = new Color(100, 100, 180);
            ToggleTint = Color.White;
            TextFont = textFont;
            CanToggle = false;
            IsToggled = false;
            OnClicked += Clicked;
            Mode = mode;
        }

        public void Clicked()
        {
            if (CanToggle)
            {
                IsToggled = !IsToggled;
            }
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            Rectangle globalBounds = GlobalBounds;
            Color imageColor = new Color(150, 150, 150, 0);
            Color textColor = TextColor;
            Color strokeColor = GUI.DefaultStrokeColor;

            if (IsLeftPressed)
            {
                imageColor = PressedTint;
                textColor = PressedTextColor;
            }
            else if (IsMouseOver)
            {
                imageColor = HoverTint;
                textColor = HoverTextColor;
            }

            if (CanToggle && IsToggled)
            {
                imageColor = ToggleTint;
            }

            if (Mode == ButtonMode.ImageButton)
            {
                    batch.Draw(Image, globalBounds, imageColor);
                    Drawer2D.SafeDraw(batch, Text, TextFont, textColor, new Vector2(globalBounds.X, globalBounds.Y + globalBounds.Height + 5), Vector2.Zero);
            }
            else if (Mode == ButtonMode.PushButton)
            {
                
                GUI.Skin.RenderButton(GlobalBounds, batch);

                Drawer2D.DrawAlignedStrokedText(batch, Text,
                                         TextFont,
                                         textColor, strokeColor, Drawer2D.Alignment.Center, GlobalBounds);
            }

            base.Render(time, batch);
        }
    }
}
