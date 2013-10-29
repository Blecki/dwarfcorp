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

        public ImageFrame Image { get; set; }
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
        public bool KeepAspectRatio { get; set; }
        public ButtonMode Mode { get; set; }

        public Button(SillyGUI gui, SillyGUIComponent parent, string text, SpriteFont textFont, ButtonMode mode, ImageFrame image) :
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
            KeepAspectRatio = false;
            Mode = mode;
        }

        public void Clicked()
        {
            if (CanToggle)
            {
                IsToggled = !IsToggled;
            }
        }

        public Rectangle GetImageBounds()
        {
            Rectangle toDraw = GlobalBounds;
            toDraw.Height -= 60;
            if (KeepAspectRatio)
            {
                if (toDraw.Width < toDraw.Height)
                {
                    float wPh = (float)toDraw.Width / (float)toDraw.Height;
                    toDraw = new Rectangle(toDraw.X, toDraw.Y, toDraw.Width, (int)(toDraw.Height * wPh));
                }
                else
                {
                    float wPh = (float)toDraw.Height / (float)toDraw.Width;
                    toDraw = new Rectangle(toDraw.X, toDraw.Y, (int)(toDraw.Width * wPh), toDraw.Height);
                }
            }
            return toDraw;
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            Rectangle globalBounds = GlobalBounds;
            Color imageColor = Color.White;
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
                if (Image != null && Image.Image != null)
                {
                    if (!KeepAspectRatio)
                    {
                        batch.Draw(Image.Image, globalBounds, Image.SourceRect, imageColor);
                    }
                    else
                    {
                        batch.Draw(Image.Image, GetImageBounds(), Image.SourceRect, imageColor);
                    }
                }
                Drawer2D.SafeDraw(batch, Text, TextFont, textColor, new Vector2(globalBounds.X, globalBounds.Y + globalBounds.Height - 60), Vector2.Zero);
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
