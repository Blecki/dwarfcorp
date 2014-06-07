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

        public ScrollView(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            ScrollX = 0;
            ScrollY = 0;
            HorizontalSlider = new Slider(gui, parent, "", 0.0f, 0.0f, 1.0f, Slider.SliderMode.Float);
            HorizontalSlider.DrawLabel = false;
            HorizontalSlider.OnValueModified += HorizontalSlider_OnValueModified;

            VerticalSlider = new Slider(gui, parent, "", 0.0f, 0.0f, 1.0f, Slider.SliderMode.Float);
            VerticalSlider.DrawLabel = false;
            VerticalSlider.OnValueModified += VerticalSlider_OnValueModified;
            VerticalSlider.Orient = Slider.Orientation.Vertical;
            OnScrolled += ScrollView_OnScrolled;
        }

        private void ScrollView_OnScrolled(int amount)
        {
            ScrollY = Math.Max(ScrollY + amount, 0);
            VerticalSlider.SliderValue = Math.Min(Math.Max(((float) ScrollY + (float) ChildRect.Y) / ((float) ChildRect.Height + GetViewRect().Height / 2), 0), 1);
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


        public void UpdateSliders()
        {
            HorizontalSlider.LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Bottom - 32, LocalBounds.Width, 32);
            VerticalSlider.LocalBounds = new Rectangle(LocalBounds.Right - 32, LocalBounds.Top, 32, LocalBounds.Height);

            if(ChildRect.Width <= LocalBounds.Width)
            {
                HorizontalSlider.IsVisible = false;
            }
            else
            {
                HorizontalSlider.IsVisible = true;
            }

            if(ChildRect.Height <= LocalBounds.Height)
            {
                VerticalSlider.IsVisible = false;
            }
            else
            {
                VerticalSlider.IsVisible = true;
            }
        }

        public Rectangle GetViewRect()
        {
            return new Rectangle(GlobalBounds.X, GlobalBounds.Y, GlobalBounds.Width - 32, GlobalBounds.Height - 32);
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            CalculateChildRect();
            UpdateSliders();
            if(IsVisible)
            {
                Rectangle originalRect = batch.GraphicsDevice.ScissorRectangle;
                Rectangle screenRect = ClipToScreen(GetViewRect(), batch.GraphicsDevice);

                batch.GraphicsDevice.ScissorRectangle = screenRect;
                foreach(GUIComponent child in Children)
                {
                    child.Render(time, batch);
                }
                batch.GraphicsDevice.ScissorRectangle = originalRect;

                Drawer2D.DrawRect(batch, GetViewRect(), Color.Black, 1);
            }
        }
    }

}