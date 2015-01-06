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



        public ImagePanel(DwarfGUI gui, GUIComponent parent, Texture2D image) :
            base(gui, parent)
        {
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
                //toDraw.Y += (toDraw.Height - Image.SourceRect.Height);
                toDraw.Height = Math.Min(Image.SourceRect.Height, toDraw.Height);

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
                    batch.Draw(imageFrame.Image, toDraw, imageFrame.SourceRect, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
                }
                else
                {
                    if(IsMouseOver)
                    {
                        batch.Draw(imageFrame.Image, toDraw, imageFrame.SourceRect, Color.Orange, 0, Vector2.Zero, SpriteEffects.None, 0);
                    }
                    else
                    {
                        batch.Draw(imageFrame.Image, toDraw, imageFrame.SourceRect, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
                    }
                }
                
            }
            base.Render(time, batch);
        }
    }

}