// Button.cs
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

        public byte Transparency { get; set; }

        public Button(DwarfGUI gui, GUIComponent parent, string text, SpriteFont textFont, ButtonMode mode, ImageFrame image) :
            base(gui, parent)
        {
            DrawFrame = mode == ButtonMode.PushButton || mode == ButtonMode.ToolButton;
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
            Transparency = 255;
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

        public override void Update(DwarfTime time)
        {
            Rectangle bounds = GetImageBounds();
            LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, Math.Max(bounds.Width, LocalBounds.Width), Math.Max(bounds.Height, LocalBounds.Height));
            base.Update(time);
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

        public override void Render(DwarfTime time, SpriteBatch batch)
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

            imageColor.A = Transparency;

            Rectangle imageBounds = GetImageBounds();
            switch(Mode)
            {
                case ButtonMode.ImageButton:
                    if(DrawFrame)
                    {
                        GUI.Skin.RenderButtonFrame(imageBounds, batch);
                    }
                    Rectangle bounds = imageBounds;
                    if(Image != null && Image.Image != null)
                    {
                        batch.Draw(Image.Image, bounds, Image.SourceRect, imageColor);
                    }

                    Drawer2D.DrawAlignedText(batch, Text, TextFont, textColor, Drawer2D.Alignment.Under | Drawer2D.Alignment.Center, new Rectangle(bounds.X + 2, bounds.Y + 4, bounds.Width, bounds.Height), true);
                    if (IsToggled)
                    {
                        Drawer2D.DrawRect(batch, GetImageBounds(), Color.White, 2);
                    }
                    break;
                case ButtonMode.PushButton:
                    if (DrawFrame)
                        GUI.Skin.RenderButton(GlobalBounds, batch);
                    Drawer2D.DrawAlignedStrokedText(batch, Text,
                        TextFont,
                        textColor, strokeColor, Drawer2D.Alignment.Center, GlobalBounds, true);
                    break;
                case ButtonMode.ToolButton:
                    if (DrawFrame)
                        GUI.Skin.RenderButton(GlobalBounds, batch);
                    if (Image != null && Image.Image != null)
                    {
                        Rectangle imageRect = GetImageBounds();
                        Rectangle alignedRect = Drawer2D.Align(GlobalBounds, imageRect.Width, imageRect.Height, Drawer2D.Alignment.Left);
                        alignedRect.X += 5;
                        batch.Draw(Image.Image, alignedRect, Image.SourceRect, imageColor);
                    }
                    Drawer2D.DrawAlignedStrokedText(batch, Text, TextFont, textColor, strokeColor, Drawer2D.Alignment.Center, GlobalBounds, true);

                    if(IsToggled)
                    {
                        Drawer2D.DrawRect(batch, GetImageBounds(), Color.White, 2);
                    }
                    
                    break;
                case ButtonMode.TabButton:
                    GUI.Skin.RenderTab(GlobalBounds, batch, IsToggled ? Color.White : Color.LightGray);
                    Drawer2D.DrawAlignedStrokedText(batch, Text,
                        TextFont,
                        textColor, strokeColor, Drawer2D.Alignment.Top, new Rectangle(GlobalBounds.X, GlobalBounds.Y + 2, GlobalBounds.Width, GlobalBounds.Height), true);
                    break;
            }

            base.Render(time, batch);
        }
    }

}