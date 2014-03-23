using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

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
        public bool Highlight { get; set; }

        public ImagePanel(DwarfGUI gui, GUIComponent parent, Texture2D image) :
            base(gui, parent)
        {
            Highlight = false;
            Lock = new Mutex();
            if(image != null)
            {
                Image = new ImageFrame(image, new Rectangle(0, 0, image.Width, image.Height));
            }
            KeepAspectRatio = true;
        }


        public ImagePanel(DwarfGUI gui, GUIComponent parent, ImageFrame image) :
            base(gui, parent)
        {
            Lock = new Mutex();
            Image = image;
            KeepAspectRatio = true;
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
            return toDraw;
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            if(Image != null && IsVisible)
            {
                Lock.WaitOne();
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
                Lock.ReleaseMutex();
            }
            base.Render(time, batch);
        }
    }

}