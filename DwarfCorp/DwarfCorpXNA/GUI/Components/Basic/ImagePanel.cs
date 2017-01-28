// ImagePanel.cs
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
using System.Threading;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// This is a GUI component which merely draws an image.
    /// </summary>
    public class ImagePanel : GUIComponent
    {
        public ImageFrame Image
        {
            get { return imageFrame; }
            set
            {
                Lock.WaitOne();
                imageFrame = value;
                Lock.ReleaseMutex();
            }
        }

        private ImageFrame imageFrame = null;
        public Mutex Lock { get; set; }
        public bool KeepAspectRatio { get; set; }
        public bool ConstrainSize { get; set; }
        public bool Highlight { get; set; }
        public string AssetName { get; set; }
        public Color Tint { get; set; }


        public ImagePanel(DwarfGUI gui, GUIComponent parent, Texture2D image) :
            base(gui, parent)
        {
            Tint = Color.White;
            AssetName = "";
            Highlight = false;
            Lock = new Mutex();
            ConstrainSize = false;
            if(image != null)
            {
                Image = new ImageFrame(image, new Rectangle(0, 0, image.Width, image.Height));
            }
            KeepAspectRatio = true;
        }


        public ImagePanel(DwarfGUI gui, GUIComponent parent, ImageFrame image) :
            base(gui, parent)
        {
            Tint = Color.White;
            AssetName = "";
            Lock = new Mutex();
            Image = image;
            KeepAspectRatio = true;
        }

        public override bool IsMouseOverRecursive()
        {

                if(!IsVisible)
            {
                return false;
            }

            MouseState mouse = Mouse.GetState();


            bool mouseOver =  (IsMouseOver && this != GUI.RootComponent) || Children.Any(child => child.IsMouseOverRecursive());

            return GetImageBounds().Contains(mouse.X, mouse.Y)  || Children.Any(child => child.IsMouseOverRecursive());
        }



        public Rectangle GetImageBounds()
        {
            Rectangle toDraw = GlobalBounds;

            if (!KeepAspectRatio)
            {
                return toDraw;
            }

            if(Image == null)
            {
                return toDraw;
            }

            toDraw = DwarfGUI.AspectRatioFit(Image.SourceRect, toDraw);

            if(ConstrainSize)
            {
                toDraw.Width = Math.Min(Image.SourceRect.Width, toDraw.Width);
                toDraw.Height = Math.Min(Image.SourceRect.Height, toDraw.Height);
                toDraw.Width = Math.Max(Image.SourceRect.Width, toDraw.Width);
                toDraw.Height = Math.Max(Image.SourceRect.Height, toDraw.Height);
            }

            return toDraw;
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if(Image != null && Image.Image != null && IsVisible)
            {
                Rectangle toDraw = GetImageBounds();

                if(!Highlight)
                {
                    batch.Draw(imageFrame.Image, toDraw, imageFrame.SourceRect, Tint, 0, Vector2.Zero, SpriteEffects.None, 0);
                }
                else
                {
                    if(IsMouseOver)
                    {
                        batch.Draw(imageFrame.Image, toDraw, imageFrame.SourceRect, Color.Orange, 0, Vector2.Zero, SpriteEffects.None, 0);
                    }
                    else
                    {
                        batch.Draw(imageFrame.Image, toDraw, imageFrame.SourceRect, Tint, 0, Vector2.Zero, SpriteEffects.None, 0);
                    }
                }
                
            }
            base.Render(time, batch);
        }
    }

    /// <summary>
    /// This is a GUI component which merely draws an image.
    /// </summary>
    public class RenderPanel : GUIComponent
    {
        public RenderTarget2D Image
        {
            get { return imageFrame; }
            set
            {
                Lock.WaitOne();
                imageFrame = value;
                Lock.ReleaseMutex();
            }
        }

        private RenderTarget2D imageFrame = null;
        public Mutex Lock { get; set; }
        public bool KeepAspectRatio { get; set; }
        public bool ConstrainSize { get; set; }
        public bool Highlight { get; set; }
        public string AssetName { get; set; }
        public Color Tint { get; set; }


        public RenderPanel(DwarfGUI gui, GUIComponent parent, RenderTarget2D image) :
            base(gui, parent)
        {
            Tint = Color.White;
            AssetName = "";
            Highlight = false;
            Lock = new Mutex();
            ConstrainSize = false;
            Image = image;
            KeepAspectRatio = true;
        }

        public override bool IsMouseOverRecursive()
        {

            if (!IsVisible)
            {
                return false;
            }

            MouseState mouse = Mouse.GetState();


            bool mouseOver = (IsMouseOver && this != GUI.RootComponent) || Children.Any(child => child.IsMouseOverRecursive());

            return GetImageBounds().Contains(mouse.X, mouse.Y) || Children.Any(child => child.IsMouseOverRecursive());
        }



        public Rectangle GetImageBounds()
        {
            Rectangle toDraw = GlobalBounds;

            if (!KeepAspectRatio)
            {
                return toDraw;
            }

            if (Image == null)
            {
                return toDraw;
            }

            toDraw = DwarfGUI.AspectRatioFit(Image.Bounds, toDraw);

            if (ConstrainSize)
            {
                toDraw.Width = Math.Min(Image.Bounds.Width, toDraw.Width);
                toDraw.Height = Math.Min(Image.Bounds.Height, toDraw.Height);
                toDraw.Width = Math.Max(Image.Width, toDraw.Width);
                toDraw.Height = Math.Max(Image.Bounds.Height, toDraw.Height);
            }

            return toDraw;
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if (Image != null && Image != null && IsVisible)
            {
                Rectangle toDraw = GetImageBounds();

                if (!Highlight)
                {
                    batch.Draw(imageFrame, toDraw, imageFrame.Bounds, Tint, 0, Vector2.Zero, SpriteEffects.None, 0);
                }
                else
                {
                    if (IsMouseOver)
                    {
                        batch.Draw(imageFrame, toDraw, imageFrame.Bounds, Color.Orange, 0, Vector2.Zero, SpriteEffects.None, 0);
                    }
                    else
                    {
                        batch.Draw(imageFrame, toDraw, imageFrame.Bounds, Tint, 0, Vector2.Zero, SpriteEffects.None, 0);
                    }
                }

            }
            base.Render(time, batch);
        }
    }

}