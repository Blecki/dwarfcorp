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
            ToolButton,
            TabButton
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
        public bool DrawFrame { get; set; }

        public bool DontMakeBigger { get; set; }
        public bool DontMakeSmaller { get; set; }

        public Button(DwarfGUI gui, GUIComponent parent, string text, SpriteFont textFont, ButtonMode mode, ImageFrame image) :
            base(gui, parent)
        {
            DrawFrame = false;
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
            DontMakeBigger = mode == ButtonMode.ToolButton;
            Mode = mode;
            DontMakeSmaller = false;
        }


        public void Clicked()
        {
            if(CanToggle)
            {
                IsToggled = !IsToggled;
            }
        }

        public override bool IsMouseOverRecursive()
        {
            MouseState state = Mouse.GetState();

            return base.IsMouseOverRecursive() || (Image != null && GetImageBounds().Contains(state.X, state.Y));
        }

        public Rectangle GetImageBounds()
        {
            Rectangle toDraw = GlobalBounds;

            if(Image == null)
            {
                return toDraw;
            }

            if(DontMakeBigger)
            {
                toDraw.Width = Math.Min(toDraw.Width, Image.SourceRect.Width);
                toDraw.Height = Math.Min(toDraw.Height, Image.SourceRect.Height);
            }

            if(DontMakeSmaller)
            {
                toDraw.Width = Math.Max(toDraw.Width, Image.SourceRect.Width);
                toDraw.Height = Math.Max(toDraw.Height, Image.SourceRect.Height);
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
            if(!IsVisible)
            {
                return;
            }

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

            Rectangle imageBounds = GetImageBounds();
            switch(Mode)
            {
                case ButtonMode.ImageButton:
                    if(DrawFrame)
                    {
                        GUI.Skin.RenderButtonFrame(imageBounds, batch);
                    }
                    if(Image != null && Image.Image != null)
                    {
                        batch.Draw(Image.Image, !KeepAspectRatio ? globalBounds : imageBounds, Image.SourceRect, imageColor);
                    }
                    
                    Drawer2D.SafeDraw(batch, Text, TextFont, textColor, new Vector2(imageBounds.X - 5, imageBounds.Y + imageBounds.Height + 1), Vector2.Zero);
                    if (IsToggled)
                    {
                        Drawer2D.DrawRect(batch, GetImageBounds(), Color.White, 2);
                    }
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

                    if(IsToggled)
                    {
                        Drawer2D.DrawRect(batch, GetImageBounds(), Color.White, 2);
                    }
                    
                    break;
                case ButtonMode.TabButton:
                    GUI.Skin.RenderTab(GlobalBounds, batch, IsToggled ? Color.White : Color.LightGray);
                    Drawer2D.DrawAlignedStrokedText(batch, Text,
                        TextFont,
                        textColor, strokeColor, Drawer2D.Alignment.Top, GlobalBounds);
                    break;
            }

            base.Render(time, batch);
        }
    }

}