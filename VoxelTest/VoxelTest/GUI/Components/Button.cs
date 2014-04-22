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

    /// <summary>
    /// This is a basic GUI element which can be clicked. There are three kinds:
    /// Image buttons, which are merely sprites, push buttons, which are just rectangles,
    /// and tool buttons, which are push buttons that have icons on them.
    /// </summary>
    public class Button : GUIComponent
    {
        public enum ButtonMode
        {
            ImageButton,
            PushButton,
            ToolButton
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

        public bool ConstrainSize { get; set; }

        public Button(DwarfGUI gui, GUIComponent parent, string text, SpriteFont textFont, ButtonMode mode, ImageFrame image) :
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
            KeepAspectRatio = mode == ButtonMode.ToolButton;
            ConstrainSize = mode == ButtonMode.ToolButton;
            Mode = mode;
        }

        public void Clicked()
        {
            if(CanToggle)
            {
                IsToggled = !IsToggled;
            }
        }

        public Rectangle GetImageBounds()
        {
            Rectangle toDraw = GlobalBounds;


            if(ConstrainSize)
            {
                toDraw.Width = Math.Min(toDraw.Width, Image.SourceRect.Width);
                toDraw.Height = Math.Min(toDraw.Height, Image.SourceRect.Height);
            }

            if (!KeepAspectRatio)
            {
                return toDraw;
            }

            toDraw = DwarfGUI.AspectRatioFit(Image.SourceRect, toDraw);

            return toDraw;
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            Rectangle globalBounds = GlobalBounds;
            Color imageColor = Color.White;
            Color textColor = TextColor;
            Color strokeColor = GUI.DefaultStrokeColor;

            if(IsLeftPressed)
            {
                imageColor = PressedTint;
                textColor = PressedTextColor;
            }
            else if(IsMouseOver)
            {
                imageColor = HoverTint;
                textColor = HoverTextColor;
            }

            if(CanToggle && IsToggled)
            {
                imageColor = ToggleTint;
            }

            switch(Mode)
            {
                case ButtonMode.ImageButton:
                    if(Image != null && Image.Image != null)
                    {
                        batch.Draw(Image.Image, !KeepAspectRatio ? globalBounds : GetImageBounds(), Image.SourceRect, imageColor);
                    }
                    Drawer2D.SafeDraw(batch, Text, TextFont, textColor, new Vector2(globalBounds.X, globalBounds.Y + globalBounds.Height), Vector2.Zero);
                    break;
                case ButtonMode.PushButton:
                    GUI.Skin.RenderButton(GlobalBounds, batch);
                    Drawer2D.DrawAlignedStrokedText(batch, Text,
                        TextFont,
                        textColor, strokeColor, Drawer2D.Alignment.Center, GlobalBounds);
                    break;
                case ButtonMode.ToolButton:
                    GUI.Skin.RenderButton(GlobalBounds, batch);
                    if (Image != null && Image.Image != null)
                    {
                        Rectangle imageRect = GetImageBounds();
                        Rectangle alignedRect = Drawer2D.Align(GlobalBounds, imageRect.Width, imageRect.Height, Drawer2D.Alignment.Left);
                        alignedRect.X += 5;
                        batch.Draw(Image.Image, alignedRect, Image.SourceRect, imageColor);
                    }
                    Drawer2D.DrawAlignedStrokedText(batch, Text, TextFont, textColor, strokeColor, Drawer2D.Alignment.Center, GlobalBounds);
                    break;
            }

            base.Render(time, batch);
        }
    }

}