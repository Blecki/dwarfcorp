// GUISkin.cs
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
using System.Security.Permissions;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Specifies how a GUI should be drawn. Has a bunch of primitive drawing functions
    /// which draw different elements of the GUI.
    /// </summary>
    public class GUISkin
    {
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int PointerWidth { get; set; }
        public int PointerHeight { get; set; }
        public Texture2D Texture { get; set; }
        public Texture2D PointerTexture { get; set; }
        public Dictionary<Tile, Point> Frames { get; set; }
        public Dictionary<MousePointer, Point> MouseFrames { get; set; }

        public enum MousePointer
        {
            Pointer,
            Dig,
            Gather,
            Build,
            Chop,
            Guard,
            Attack,
            Magic,
            Wait
        }

        public enum Tile
        {
            PanelUpperLeft,
            PanelUpperRight,
            PanelUpper,
            PanelLowerLeft,
            PanelLowerRight,
            PanelLeft,
            PanelRight,
            PanelLower,
            PanelCenter,

            WindowUpperLeft,
            WindowUpperRight,
            WindowUpperRightNoEx,
            WindowUpper,
            WindowLowerLeft,
            WindowLowerRight,
            WindowLeft,
            WindowRight,
            WindowLower,
            WindowCenter,

            CheckboxUnchecked,
            CheckboxChecked,
            
            Radiobutton,
            RadiobuttonPushed,
            
            ButtonUpperLeft,
            ButtonUpperRight,
            ButtonLowerLeft,
            ButtonLowerRight,
            ButtonLeft,
            ButtonRight,
            ButtonUpper,
            ButtonLower,
            ButtonCenter,

            ToolTipUpperLeft,
            ToolTipUpperRight,
            ToolTipLowerLeft,
            ToolTipLowerRight,
            ToolTipLeft,
            ToolTipRight,
            ToolTipUpper,
            ToolTipLower,
            ToolTipCenter,


            Track,
            TrackVert,
            SliderTex,
            SliderVertical,

            FieldLeft,
            FieldRight,
            FieldCenter,


            GroupUpperLeft,
            GroupUpper,
            GroupUpperRight,
            GroupLeft,
            GroupRight,
            GroupLower ,
            GroupLowerRight ,
            GroupLowerLeft,

            ProgressLeft,
            ProgressFilled,
            ProgressEmpty,
            ProgressCap ,
            ProgressRight ,

            Check ,
            Ex ,
            RightArrow ,
            LeftArrow ,
            DownArrow,
            Save ,

            SmallArrowRight,
            SmallArrowLeft,
            SmallArrowUp,
            SmallArrowDown,
            SmallEx,

            CloseButton,

            ZoomIn,
            ZoomOut,
            ZoomHome,

            ButtonFrame,
            TabLeft,
            TabCenter,
            TabRight
        }

        public Timer MouseTimer { get; set; }
        public int WaitIndex { get; set; }

        public GUISkin(Texture2D texture, int tileWidth, int tileHeight, Texture2D pointerTexture, int pointerWidth, int pointerHeight)
        {
            Texture = texture;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            PointerWidth = pointerWidth;
            PointerHeight = pointerHeight;
            PointerTexture = pointerTexture;
            MouseFrames = new Dictionary<MousePointer, Point>();
            Frames = new Dictionary<Tile, Point>();
            MouseTimer = new Timer(0.1f, false, Timer.TimerMode.Real);
            WaitIndex = 0;
        }

        public Rectangle GetRect(Point p, int w, int h)
        {
            return new Rectangle(p.X * w, p.Y * h, w, h);
        }

        public ImageFrame GetMouseFrame(Point p)
        {
            return new ImageFrame(PointerTexture, GetRect(p, PointerWidth, PointerHeight));
        }

        public Rectangle GetSourceRect(Point p)
        {
            return GetRect(p, TileWidth, TileHeight);
        }

        public ImageFrame GetSpecialFrame(MousePointer key)
        {
            MouseTimer.Update(DwarfTime.LastTime);

            if (key == MousePointer.Wait)
            {
                Point frame = MouseFrames[key];

                if (MouseTimer.HasTriggered)
                {
                    WaitIndex = (WaitIndex + 1)%6;
                }

                frame.X += WaitIndex;
                return GetMouseFrame(frame);
            }
            else
            {

                return GetMouseFrame(MouseFrames[key]);
            }
        }

        public ImageFrame GetSpecialFrame(Tile key)
        {
            return new ImageFrame(Texture, GetSourceRect(Frames[key]));
        }

        public Rectangle GetSourceRect(Tile s)
        {
            return GetSourceRect(Frames[s]);
        }

        public void SetDefaults()
        {
            Frames[Tile.PanelUpperLeft] = new Point(0, 0);
            Frames[Tile.PanelUpper] = new Point(1, 0);
            Frames[Tile.PanelUpperRight] = new Point(2, 0);
            Frames[Tile.PanelLeft] = new Point(0, 1);
            Frames[Tile.PanelCenter] = new Point(1, 1);
            Frames[Tile.PanelRight] = new Point(2, 1);
            Frames[Tile.PanelLowerLeft] = new Point(0, 2);
            Frames[Tile.PanelLower] = new Point(1, 2);
            Frames[Tile.PanelLowerRight] = new Point(2, 2);

            Frames[Tile.WindowUpperLeft] = new Point(0, 10);
            Frames[Tile.WindowUpper] = new Point(1, 10);
            Frames[Tile.WindowUpperRight] = new Point(2, 10);
            Frames[Tile.WindowUpperRightNoEx] = new Point(2, 3);
            Frames[Tile.CloseButton] = new Point(3, 10);
            Frames[Tile.WindowLeft] = new Point(0, 11);
            Frames[Tile.WindowCenter] = new Point(1, 11);
            Frames[Tile.WindowRight] = new Point(2, 11);
            Frames[Tile.WindowLowerLeft] = new Point(0, 12);
            Frames[Tile.WindowLower] = new Point(1, 12);
            Frames[Tile.WindowLowerRight] = new Point(2, 12);

            Frames[Tile.CheckboxUnchecked] = new Point(3, 0);
            Frames[Tile.CheckboxChecked] = new Point(3, 1);

            Frames[Tile.Radiobutton] = new Point(4, 0);
            Frames[Tile.RadiobuttonPushed] = new Point(4, 1);

            Frames[Tile.ButtonUpperLeft] = new Point(5, 0);
            Frames[Tile.ButtonUpper] = new Point(6, 0);
            Frames[Tile.ButtonUpperRight] = new Point(7, 0);
            Frames[Tile.ButtonLeft] = new Point(5, 1);
            Frames[Tile.ButtonCenter] = new Point(6, 1);
            Frames[Tile.ButtonRight] = new Point(7, 1);
            Frames[Tile.ButtonLowerLeft] = new Point(5, 2);
            Frames[Tile.ButtonLower] = new Point(6, 2);
            Frames[Tile.ButtonLowerRight] = new Point(7, 2);

            Frames[Tile.ToolTipUpperLeft] = new Point(8, 0);
            Frames[Tile.ToolTipUpper] = new Point(9, 0);
            Frames[Tile.ToolTipUpperRight] = new Point(10, 0);
            Frames[Tile.ToolTipLeft] = new Point(8, 1);
            Frames[Tile.ToolTipCenter] = new Point(9, 1);
            Frames[Tile.ToolTipRight] = new Point(10, 1);
            Frames[Tile.ToolTipLowerLeft] = new Point(8, 2);
            Frames[Tile.ToolTipLower] = new Point(9, 2);
            Frames[Tile.ToolTipLowerRight] = new Point(10, 2);

            Frames[Tile.FieldLeft] = new Point(3, 3);
            Frames[Tile.FieldCenter] = new Point(4, 3);
            Frames[Tile.FieldRight] = new Point(5, 3);

            Frames[Tile.TabLeft] = new Point(0, 9);
            Frames[Tile.TabCenter] = new Point(1, 9);
            Frames[Tile.TabRight] = new Point(2, 9);

            Frames[Tile.DownArrow] = new Point(6, 3);

            Frames[Tile.Track] = new Point(3, 4);
            Frames[Tile.SliderTex] = new Point(4, 4);
            Frames[Tile.TrackVert] = new Point(5, 4);
            Frames[Tile.SliderVertical] = new Point(6, 4);

            Frames[Tile.GroupUpperLeft] = new Point(0, 6);
            Frames[Tile.GroupUpper] = new Point(1, 6);
            Frames[Tile.GroupUpperRight] = new Point(2, 6);
            Frames[Tile.GroupLeft] = new Point(0, 7);
            Frames[Tile.GroupRight] = new Point(2, 7);
            Frames[Tile.GroupLowerLeft] = new Point(0, 8);
            Frames[Tile.GroupLower] = new Point(1, 8);
            Frames[Tile.GroupLowerRight] = new Point(2, 8);

            Frames[Tile.ProgressLeft] = new Point(3, 6);
            Frames[Tile.ProgressFilled] = new Point(7, 6);
            Frames[Tile.ProgressCap] = new Point(6, 6);
            Frames[Tile.ProgressEmpty] = new Point(5, 6);
            Frames[Tile.ProgressRight] = new Point(4, 6);

            Frames[Tile.Check] = new Point(3, 7);
            Frames[Tile.Ex] = new Point(4, 7);
            Frames[Tile.Save] = new Point(3, 8);
            Frames[Tile.LeftArrow] = new Point(4, 8);
            Frames[Tile.RightArrow] = new Point(5, 8);

            Frames[Tile.ZoomIn] = new Point(11,0);
            Frames[Tile.ZoomOut] = new Point(11, 1);
            Frames[Tile.ZoomHome] = new Point(12, 0);

            Frames[Tile.SmallArrowLeft] = new Point(14, 1);
            Frames[Tile.SmallArrowRight] = new Point(13, 0);
            Frames[Tile.SmallArrowUp] = new Point(12, 1);
            Frames[Tile.SmallArrowDown] = new Point(13, 1);
            Frames[Tile.SmallEx] = new Point(14, 0);

            Frames[Tile.ButtonFrame] = new Point(9, 4);

            MouseFrames[MousePointer.Pointer] = new Point(0, 0);
            MouseFrames[MousePointer.Dig] = new Point(1, 0);
            MouseFrames[MousePointer.Build] = new Point(4, 0);
            MouseFrames[MousePointer.Gather] = new Point(6, 0);
            MouseFrames[MousePointer.Chop] = new Point(5, 0);
            MouseFrames[MousePointer.Attack] = new Point(2, 0);
            MouseFrames[MousePointer.Guard] = new Point(3, 0);
            MouseFrames[MousePointer.Magic] = new Point(0, 1);
            MouseFrames[MousePointer.Wait] = new Point(0, 2);

         }

        public void RenderButtonFrame(Rectangle buttonRect, SpriteBatch batch)
        {
            Rectangle destRect = new Rectangle(buttonRect.X - TileWidth, buttonRect.Y - TileHeight, buttonRect.Width + TileWidth * 2, buttonRect.Height + TileHeight * 2);
            ImageFrame frame = GetSpecialFrame(Tile.ButtonFrame);
            Rectangle sourceRect = new Rectangle(frame.SourceRect.X - TileWidth, frame.SourceRect.Y - TileHeight, frame.SourceRect.Width + TileWidth * 2, frame.SourceRect.Height + TileHeight * 2);

            batch.Draw(frame.Image, destRect, sourceRect, Color.White);
        }

        public void RenderMouse(int x, int y, int scale, MousePointer mode, SpriteBatch spriteBatch, Color tint)
        {
            spriteBatch.Draw(PointerTexture, new Rectangle(x, y, PointerWidth * scale, PointerHeight * scale), GetSpecialFrame(mode).SourceRect, tint);
        }

        public void RenderTile(Rectangle screenRect, Tile tile, SpriteBatch batch, Color tint)
        {
            batch.Draw(Texture, screenRect, GetSpecialFrame(tile).SourceRect, tint);
        }

        public void RenderPanel(Rectangle rectbounds, SpriteBatch spriteBatch)
        {
            Rectangle rect = new Rectangle((int) (rectbounds.X + TileWidth / 4), (int) (rectbounds.Y + TileHeight / 4), rectbounds.Width - TileWidth / 2, rectbounds.Height - TileHeight / 2);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.PanelUpperLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.PanelLowerLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.PanelUpperRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.PanelLowerRight), Color.White);

            int maxX = rect.X + rect.Width;
            int diffX = rect.Width % TileWidth;
            int maxY = rect.Y + rect.Height;
            int diffY = rect.Height % TileHeight;
            int right = maxX - diffX - TileWidth;
            int bottom = maxY - diffY - TileHeight;
            int left = rect.X;
            int top = rect.Y;

            for(int x = left; x <= right; x += TileWidth)
            {
                spriteBatch.Draw(Texture, new Rectangle(x, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.PanelUpper), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, rect.Y - TileHeight, diffX, TileHeight), GetSourceRect(Tile.PanelUpper), Color.White);

            for(int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, y, TileWidth, TileHeight), GetSourceRect(Tile.PanelLeft), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, maxY - diffY, TileWidth, diffY), GetSourceRect(Tile.PanelLeft), Color.White);

            for(int x = left; x <= right; x += TileWidth)
            {
                spriteBatch.Draw(Texture, new Rectangle(x, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.PanelLower), Color.White);
            }


            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, rect.Y + rect.Height, diffX, TileHeight), GetSourceRect(Tile.PanelLower), Color.White);

            for(int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, y, TileWidth, TileHeight), GetSourceRect(Tile.PanelRight), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, maxY - diffY, TileWidth, diffY), GetSourceRect(Tile.PanelRight), Color.White);

            for(int x = left; x <= right; x += TileWidth)
            {
                for(int y = top; y <= bottom; y += TileHeight)
                {
                    spriteBatch.Draw(Texture, new Rectangle(x, y, TileWidth, TileHeight), GetSourceRect(Tile.PanelCenter), Color.White);
                }
                spriteBatch.Draw(Texture, new Rectangle(x, maxY - diffY, TileWidth, diffY), GetSourceRect(Tile.PanelCenter), Color.White);
            }

            for(int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, y, diffX, TileHeight), GetSourceRect(Tile.PanelCenter), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, maxY - diffY, diffX, diffY), GetSourceRect(Tile.PanelCenter), Color.White);
        }

        public void RenderWindow(Rectangle rectbounds, SpriteBatch spriteBatch, bool ex)
        {
            Tile upperTile = ex ?  Tile.WindowUpperRight : Tile.WindowUpperRightNoEx;
    
            Rectangle rect = new Rectangle((int)(rectbounds.X + TileWidth / 4), (int)(rectbounds.Y + TileHeight / 4), rectbounds.Width - TileWidth / 2, rectbounds.Height - TileHeight / 2);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.WindowUpperLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.WindowLowerLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(upperTile), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.WindowLowerRight), Color.White);

            int maxX = rect.X + rect.Width;
            int diffX = rect.Width % TileWidth;
            int maxY = rect.Y + rect.Height;
            int diffY = rect.Height % TileHeight;
            int right = maxX - diffX - TileWidth;
            int bottom = maxY - diffY - TileHeight;
            int left = rect.X;
            int top = rect.Y;

            for (int x = left; x <= right; x += TileWidth)
            {
                spriteBatch.Draw(Texture, new Rectangle(x, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.WindowUpper), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, rect.Y - TileHeight, diffX, TileHeight), GetSourceRect(Tile.WindowUpper), Color.White);

            for (int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, y, TileWidth, TileHeight), GetSourceRect(Tile.WindowLeft), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, maxY - diffY, TileWidth, diffY), GetSourceRect(Tile.WindowLeft), Color.White);

            for (int x = left; x <= right; x += TileWidth)
            {
                spriteBatch.Draw(Texture, new Rectangle(x, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.WindowLower), Color.White);
            }


            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, rect.Y + rect.Height, diffX, TileHeight), GetSourceRect(Tile.WindowLower), Color.White);

            for (int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, y, TileWidth, TileHeight), GetSourceRect(Tile.WindowRight), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, maxY - diffY, TileWidth, diffY), GetSourceRect(Tile.WindowRight), Color.White);

            for (int x = left; x <= right; x += TileWidth)
            {
                for (int y = top; y <= bottom; y += TileHeight)
                {
                    spriteBatch.Draw(Texture, new Rectangle(x, y, TileWidth, TileHeight), GetSourceRect(Tile.WindowCenter), Color.White);
                }
                spriteBatch.Draw(Texture, new Rectangle(x, maxY - diffY, TileWidth, diffY), GetSourceRect(Tile.WindowCenter), Color.White);
            }

            for (int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, y, diffX, TileHeight), GetSourceRect(Tile.WindowCenter), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, maxY - diffY, diffX, diffY), GetSourceRect(Tile.WindowCenter), Color.White);
        }


        public void RenderButton(Rectangle rectbounds, SpriteBatch spriteBatch)
        {
            int w = Math.Max(rectbounds.Width - TileWidth / 4, TileWidth / 4);
            int h = Math.Max(rectbounds.Height - TileHeight / 4, TileHeight / 4);
            Rectangle rect = new Rectangle((int) (rectbounds.X + TileWidth / 8),
                (int) (rectbounds.Y + TileHeight / 8),
                w,
                h);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.ButtonUpperLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.ButtonLowerLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.ButtonUpperRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.ButtonLowerRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y - TileHeight, rect.Width, TileHeight), GetSourceRect(Tile.ButtonUpper), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y, TileWidth, rect.Height), GetSourceRect(Tile.ButtonLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y + rect.Height, rect.Width, TileHeight), GetSourceRect(Tile.ButtonLower), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y, TileWidth, rect.Height), GetSourceRect(Tile.ButtonRight), Color.White);
            spriteBatch.Draw(Texture, rect, GetSourceRect(Tile.ButtonCenter), Color.White);
        }

        public void RenderToolTip(Rectangle rectbounds, SpriteBatch spriteBatch, Color tint)
        {
            int w = Math.Max(rectbounds.Width - TileWidth / 4, TileWidth / 4);
            int h = Math.Max(rectbounds.Height - TileHeight / 4, TileHeight / 4);
            Rectangle rect = new Rectangle((int)(rectbounds.X + TileWidth / 8),
                (int)(rectbounds.Y + TileHeight / 8),
                w,
                h);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.ToolTipUpperLeft), tint);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.ToolTipLowerLeft), tint);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.ToolTipUpperRight), tint);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.ToolTipLowerRight), tint);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y - TileHeight, rect.Width, TileHeight), GetSourceRect(Tile.ToolTipUpper), tint);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y, TileWidth, rect.Height), GetSourceRect(Tile.ToolTipLeft), tint);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y + rect.Height, rect.Width, TileHeight), GetSourceRect(Tile.ToolTipLower), tint);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y, TileWidth, rect.Height), GetSourceRect(Tile.ToolTipRight), tint);
            spriteBatch.Draw(Texture, rect, GetSourceRect(Tile.ToolTipCenter), tint);
        }

        public void RenderCheckbox(Rectangle rect, bool checkstate, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, rect, checkstate ? GetSourceRect(Tile.CheckboxChecked) : GetSourceRect(Tile.CheckboxUnchecked), Color.White);
        }

        public void RenderDownArrow(Rectangle rect, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, rect, GetSourceRect(Tile.DownArrow), Color.White);
        }

        public void RenderRadioButton(Rectangle rect, bool checkstate, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, rect, checkstate ? GetSourceRect(Tile.RadiobuttonPushed) : GetSourceRect(Tile.Radiobutton), Color.White);
        }

        public void RenderField(Rectangle rect, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y, TileWidth, TileHeight), GetSourceRect(Tile.FieldLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + TileWidth, rect.Y, rect.Width - TileWidth * 2, TileHeight), GetSourceRect(Tile.FieldCenter), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.Right - TileWidth, rect.Top, TileWidth, TileHeight), GetSourceRect(Tile.FieldRight), Color.White);
        }

        public void RenderProgressBar(Rectangle rectBounds, float progress, Color tint, SpriteBatch spriteBatch)
        {
            float n = (float) Math.Max(Math.Min(progress, 1.0), 0.0);

            if(n > 0)
            {
                Rectangle drawFillRect = new Rectangle(rectBounds.X + TileWidth / 2 - 8, rectBounds.Y, (int) ((rectBounds.Width - TileWidth / 2 - 4) * n) - 8, rectBounds.Height);
                Rectangle filledRect = GetSourceRect(Tile.ProgressFilled);
                filledRect.Width = 1;
                spriteBatch.Draw(Texture, drawFillRect, filledRect, tint);

                Rectangle progressRect = GetSourceRect(Tile.ProgressCap);
                progressRect.Width = 8;

                Rectangle capRect = new Rectangle((int) ((rectBounds.Width - TileWidth / 2 - 4) * n) + rectBounds.X, rectBounds.Y, 8, rectBounds.Height);
                spriteBatch.Draw(Texture, capRect, progressRect, tint);
            }

            spriteBatch.Draw(Texture, new Rectangle(rectBounds.X, rectBounds.Y, TileWidth, rectBounds.Height), GetSourceRect(Tile.ProgressLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rectBounds.X + rectBounds.Width - TileWidth, rectBounds.Y, TileWidth, rectBounds.Height), GetSourceRect(Tile.ProgressRight), Color.White);

            int steps = (rectBounds.Width - TileWidth) / TileWidth;

            for(int i = 0; i < steps; i++)
            {
                spriteBatch.Draw(Texture, new Rectangle(rectBounds.X + TileWidth / 2 + i * TileWidth, rectBounds.Y, TileWidth, rectBounds.Height), GetSourceRect(Tile.ProgressEmpty), Color.White);
            }

            int remainder = (rectBounds.Width - TileWidth) - steps * TileWidth;

            if(remainder > 0)
            {
                spriteBatch.Draw(Texture, new Rectangle(rectBounds.X + TileWidth / 2 + steps * TileWidth, rectBounds.Y, remainder, rectBounds.Height), GetSourceRect(Tile.ProgressEmpty), Color.White);
            }
        }

        public void RenderGroup(Rectangle rectbounds, SpriteBatch spriteBatch)
        {
            Rectangle rect = new Rectangle((int) (rectbounds.X + TileWidth / 4), (int) (rectbounds.Y + TileHeight / 4), rectbounds.Width - TileWidth / 2, rectbounds.Height - TileHeight / 2);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.GroupUpperLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.GroupLowerLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(Tile.GroupUpperRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(Tile.GroupLowerRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y - TileHeight, rect.Width, TileHeight), GetSourceRect(Tile.GroupUpper), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y, TileWidth, rect.Height), GetSourceRect(Tile.GroupLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y + rect.Height, rect.Width, TileHeight), GetSourceRect(Tile.GroupLower), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y, TileWidth, rect.Height), GetSourceRect(Tile.GroupRight), Color.White);
        }

        public void RenderSliderVertical(SpriteFont font, Rectangle boundingRect, float value, float minvalue, float maxValue, Slider.SliderMode mode,  bool drawLabel, bool invert, SpriteBatch spriteBatch)
        {
            const int padding = 5;

            if(invert)
            {
                value = maxValue - value;
            }


            int fieldSize = Math.Max(Math.Min((int) (0.2f * boundingRect.Width), 150), 64);
            Rectangle rect = new Rectangle(boundingRect.X + boundingRect.Width / 2 - TileWidth / 2, boundingRect.Y + padding, boundingRect.Width, boundingRect.Height - TileHeight - padding * 2);
            Rectangle fieldRect = new Rectangle(boundingRect.Right - fieldSize, boundingRect.Y + boundingRect.Height - TileHeight / 2, fieldSize, TileHeight);

            int maxY = rect.Y + rect.Height;
            int diffY = rect.Height % TileHeight;
            int bottom = maxY;
            int left = rect.X;
            int top = rect.Y;


            for(int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(rect.X, y, TileWidth, TileHeight), GetSourceRect(Tile.TrackVert), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(rect.X, maxY - diffY, TileWidth, diffY), GetSourceRect(Tile.TrackVert), Color.White);

            float d = (value - minvalue) / (maxValue - minvalue);

            int sliderY = (int) ((d) * rect.Height + rect.Y);

            spriteBatch.Draw(Texture, new Rectangle(rect.X, sliderY - TileHeight / 2, TileWidth, TileHeight), GetSourceRect(Tile.SliderVertical), Color.White);

            if(!drawLabel)
            {
                return;
            }

            RenderField(fieldRect, spriteBatch);

            if(invert)
            {
                value = -(value - maxValue);
            }

            float v = 0.0f;
            if(mode == Slider.SliderMode.Float)
            {
                v = (float) Math.Round(value, 2);
            }
            else
            {
                v = (int) value;
            }

            string toDraw = "" + v;

            Vector2 origin = Datastructures.SafeMeasure(font, toDraw) * 0.5f;

            Drawer2D.SafeDraw(spriteBatch, toDraw, font, Color.Black, new Vector2(fieldRect.X + fieldRect.Width / 2, fieldRect.Y + 16), origin);
        }

        public void RenderSliderHorizontal(SpriteFont font, Rectangle boundingRect, float value, float minvalue, float maxValue, Slider.SliderMode mode,  bool drawLabel, bool invertValue, SpriteBatch spriteBatch)
        {
            const int padding = 5;

            if(invertValue)
            {
                value = maxValue - value;
            }

            int fieldSize = Math.Max(Math.Min((int) (0.2f * boundingRect.Width), 150), 64);
            Rectangle rect = new Rectangle(boundingRect.X + padding, boundingRect.Y + boundingRect.Height / 2 - TileHeight / 2, boundingRect.Width - fieldSize - padding * 2, boundingRect.Height / 2);
            Rectangle fieldRect = new Rectangle(boundingRect.Right - fieldSize, boundingRect.Y + boundingRect.Height / 2 - TileHeight / 2, fieldSize, boundingRect.Height / 2);
            int maxX = rect.X + rect.Width;
            int diffX = rect.Width % TileWidth;
            int right = maxX;
            int left = rect.X;
            int top = rect.Y;


            for(int x = left; x <= right; x += TileWidth)
            {
                spriteBatch.Draw(Texture, new Rectangle(x, rect.Y, TileWidth, TileHeight), GetSourceRect(Tile.Track), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, rect.Y, diffX, TileHeight), GetSourceRect(Tile.Track), Color.White);

            int sliderX = (int) ((value - minvalue) / (maxValue - minvalue) * rect.Width + rect.X);

            spriteBatch.Draw(Texture, new Rectangle(sliderX - TileWidth / 2, rect.Y, TileWidth, TileHeight), GetSourceRect(Tile.SliderTex), Color.White);

            if(!drawLabel)
            {
                return;
            }
            RenderField(fieldRect, spriteBatch);

            float v = 0.0f;
            if(invertValue)
            {
                value = value - maxValue;
            }
            if(mode == Slider.SliderMode.Float)
            {
                v = (float) Math.Round(value, 2);
            }
            else
            {
                v = (int) value;
            }

            string toDraw = "" + v;

            Vector2 origin = Datastructures.SafeMeasure(font, toDraw) * 0.5f;

            Drawer2D.SafeDraw(spriteBatch, toDraw, font, Color.Black, new Vector2(fieldRect.X + fieldRect.Width / 2, fieldRect.Y + 16), origin);
        }

        public void RenderTab(Rectangle rect, SpriteBatch spriteBatch, Color tint)
        {
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y - TileHeight/4 - 2, TileWidth, TileHeight), GetSourceRect(Tile.TabLeft), tint);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + TileWidth, rect.Y - TileHeight/4 - 2, rect.Width - TileWidth * 2, TileHeight), GetSourceRect(Tile.TabCenter), tint);
            spriteBatch.Draw(Texture, new Rectangle(rect.Right - TileWidth, rect.Top - TileHeight / 4 - 2, TileWidth, TileHeight), GetSourceRect(Tile.TabRight), tint);
        }
    }

}