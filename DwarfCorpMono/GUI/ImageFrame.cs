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
    public class ImageFrame
    {
        public Texture2D Image { get; set; }
        public Rectangle SourceRect { get; set; }

        public ImageFrame(Texture2D image)
        {
            Image = image;
            SourceRect = image.Bounds;
        }

        public ImageFrame(Texture2D image, Rectangle sourceRect)
        {
            Image = image;
            SourceRect = sourceRect;
        }
    }

}
