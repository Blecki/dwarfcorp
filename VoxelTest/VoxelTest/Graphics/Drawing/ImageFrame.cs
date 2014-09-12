using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Represents a sub-rectangle inside a 2D texture.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class ImageFrame
    {
        public Texture2D Image { get; set; }
        public Rectangle SourceRect { get; set; }

        public ImageFrame()
        {
            
        }

        public ImageFrame(Texture2D image)
        {
            Image = image;
            if(image != null)
                SourceRect = image.Bounds;
        }

        public ImageFrame(Texture2D image, int frameSize, int x, int y)
        {
            Image = image;
            SourceRect = new Rectangle(x * frameSize, y * frameSize, frameSize, frameSize);
        }

        public ImageFrame(Texture2D image, Rectangle sourceRect)
        {
            Image = image;
            SourceRect = sourceRect;
        }
    }

}