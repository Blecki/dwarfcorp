// ScrollView.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    /// <summary>
    /// This GUI component holds other components, and allows different parts
    /// of its viewing area to be accessed by scroll bars.
    /// </summary>
    public class ScrollView : GUIComponent
    {
        protected int sx = 0;
        protected int sy = 0;
        protected int lastSx = 0;
        protected int lastSy = 0;

        public int ScrollX
        {
            get { return sx; }
            set
            {
                sx = value;
                UpdateScrollArea();
            }
        }

        public int ScrollY
        {
            get { return sy; }
            set
            {
                sy = value;
                UpdateScrollArea();
            }
        }

        protected Rectangle childRect;

        public Rectangle ChildRect
        {
            get { return childRect; }
            set { childRect = value; }
        }

        public Slider HorizontalSlider { get; set; }
        public Slider VerticalSlider { get; set; }
        public bool DrawBorder { get; set; }

        public ScrollView(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            ScrollX = 0;
            ScrollY = 0;
            HorizontalSlider = new Slider(gui, parent, "", 0.0f, 0.0f, 1.0f, Slider.SliderMode.Float)
            {
                DrawLabel = false
            };
            HorizontalSlider.OnValueModified += HorizontalSlider_OnValueModified;

            VerticalSlider = new Slider(gui, parent, "", 0.0f, 0.0f, 1.0f, Slider.SliderMode.Float) {DrawLabel = false};
            VerticalSlider.OnValueModified += VerticalSlider_OnValueModified;
            VerticalSlider.Orient = Slider.Orientation.Vertical;
            OnScrolled += ScrollView_OnScrolled;
        }

        private void ScrollView_OnScrolled(int amount)
        {
            if (IsVisible && IsMouseOver && ParentVisibleRecursive())
            {
                ScrollY = Math.Max(ScrollY + amount, 0);
                VerticalSlider.SliderValue =
                    Math.Min(
                        Math.Max(
                            ((float) ScrollY + (float) ChildRect.Y)/((float) ChildRect.Height + GetViewRect().Height/2),
                            0), 1);
            }
        }


        private void VerticalSlider_OnValueModified(float arg)
        {
            ScrollY = (int) (arg * (ChildRect.Height + GetViewRect().Height / 2) - ChildRect.Y);
        }

        private void HorizontalSlider_OnValueModified(float arg)
        {
            ScrollX = (int) (arg * (ChildRect.Width + GetViewRect().Width / 2) - ChildRect.X);
        }

        private void CalculateChildRect()
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = -int.MaxValue;
            int maxY = -int.MaxValue;
            foreach(GUIComponent child in Children)
            {
                minX = Math.Min(child.LocalBounds.X + sx, minX);
                minY = Math.Min(child.LocalBounds.Y + sy, minY);

                maxX = Math.Max(child.LocalBounds.Right + sx, maxX);
                maxY = Math.Max(child.LocalBounds.Bottom + sy, maxY);
            }

            childRect = new Rectangle(minX, minY, maxX - minX, maxY - minY);

            foreach (GUIComponent child in Children)
            {
                child.ClipRecursive(GlobalBounds);
            }
        }

        public void ResetScroll()
        {
            sx = 0;
            sy = 0;
            lastSx = sx;
            lastSy = sy;
            VerticalSlider.SliderValue = 0;
            HorizontalSlider.SliderValue = 0;
        }


        public void UpdateScrollArea()
        {
            int dx = sx - lastSx;
            int dy = sy - lastSy;
            foreach(GUIComponent child in Children)
            {
                child.LocalBounds = new Rectangle(child.LocalBounds.X - dx, child.LocalBounds.Y - dy, child.LocalBounds.Width, child.LocalBounds.Height);
            }

            UpdateTransformsRecursive();
            lastSx = sx;
            lastSy = sy;
            CalculateChildRect();
        }

        public override bool IsMouseOverRecursive()
        {
            Rectangle screenRect = GetViewRect();
            MouseState mouseState = Mouse.GetState();
            if (screenRect.Contains(mouseState.X, mouseState.Y))
            {
                return base.IsMouseOverRecursive();
            }

            return false;
        }

        public void UpdateSliders()
        {
            HorizontalSlider.LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Bottom - 32, LocalBounds.Width, 32);
            VerticalSlider.LocalBounds = new Rectangle(LocalBounds.Right - 32, LocalBounds.Top, 32, LocalBounds.Height);

            HorizontalSlider.IsVisible = ChildRect.Width > LocalBounds.Width;

            VerticalSlider.IsVisible = ChildRect.Height > LocalBounds.Height;
        }

        public Rectangle GetViewRect()
        {
            return new Rectangle(GlobalBounds.X, GlobalBounds.Y, GlobalBounds.Width - 32, GlobalBounds.Height - 32);
        }

        public Rectangle StartClip(SpriteBatch batch)
        {
            Rectangle originalRect = batch.GraphicsDevice.ScissorRectangle;
            Rectangle screenRect = ClipToScreen(GetViewRect(), batch.GraphicsDevice);
            batch.GraphicsDevice.ScissorRectangle = screenRect;
            return originalRect;
        }

        public void EndClip(Rectangle originalRect, SpriteBatch batch)
        {
            batch.GraphicsDevice.ScissorRectangle = originalRect;
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            CalculateChildRect();
            UpdateSliders();
            if(IsVisible)
            {
                Rectangle originalRect = StartClip(batch);
                Rectangle screenRect = ClipToScreen(GetViewRect(), batch.GraphicsDevice);
                foreach(GUIComponent child in Children)
                {
                    child.Render(time, batch);
                }
                EndClip(originalRect, batch);

                if (DrawBorder)
                {
                    Drawer2D.DrawRect(batch, screenRect, Color.Black, 1);
                }
            }

        }
    }

}