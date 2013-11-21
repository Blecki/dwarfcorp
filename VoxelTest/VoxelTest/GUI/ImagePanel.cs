using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

namespace DwarfCorp
{

    public class ImagePanel : SillyGUIComponent
    {
        public ImageFrame Image
        {
            get { return m_texture; }
            set
            {
                Lock.WaitOne();
                m_texture = value;
                Lock.ReleaseMutex();
            }
        }

        private ImageFrame m_texture = null;
        public Mutex Lock { get; set; }
        public bool KeepAspectRatio { get; set; }
        public bool Highlight { get; set; }

        public ImagePanel(SillyGUI gui, SillyGUIComponent parent, Texture2D image) :
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


        public ImagePanel(SillyGUI gui, SillyGUIComponent parent, ImageFrame image) :
            base(gui, parent)
        {
            Lock = new Mutex();
            Image = image;
            KeepAspectRatio = true;
        }

        public Rectangle GetImageBounds()
        {
            Rectangle toDraw = GlobalBounds;
            if(KeepAspectRatio)
            {
                if(toDraw.Width < toDraw.Height)
                {
                    float wPh = (float) toDraw.Width / (float) toDraw.Height;
                    toDraw = new Rectangle(toDraw.X, toDraw.Y, toDraw.Width, (int) (toDraw.Height * wPh));
                }
                else
                {
                    float wPh = (float) toDraw.Height / (float) toDraw.Width;
                    toDraw = new Rectangle(toDraw.X, toDraw.Y, (int) (toDraw.Width * wPh), toDraw.Height);
                }
            }
            return toDraw;
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            if(Image != null)
            {
                Lock.WaitOne();
                Rectangle toDraw = GetImageBounds();

                if(!Highlight)
                {
                    batch.Draw(m_texture.Image, toDraw, m_texture.SourceRect, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
                }
                else
                {
                    if(IsMouseOver)
                    {
                        batch.Draw(m_texture.Image, toDraw, m_texture.SourceRect, Color.Orange, 0, Vector2.Zero, SpriteEffects.None, 0);
                    }
                    else
                    {
                        batch.Draw(m_texture.Image, toDraw, m_texture.SourceRect, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
                    }
                }
                Lock.ReleaseMutex();
            }
            base.Render(time, batch);
        }
    }

}