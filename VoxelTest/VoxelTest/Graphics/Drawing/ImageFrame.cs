using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

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

    [JsonObject(IsReference = true)]
    public class NamedImageFrame : ImageFrame
    {
        public string AssetName { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Image = TextureManager.GetTexture(AssetName);
        }

        public NamedImageFrame()
        {
            
        }

        public NamedImageFrame(string name)
        {
            AssetName = name;
            Image = TextureManager.GetTexture(name);

            if (Image != null)
            {
                SourceRect = Image.Bounds;
            }
        }

        public NamedImageFrame(string name, int frameSize, int x, int y)
        {
            AssetName = name;
            Image = TextureManager.GetTexture(name);
            SourceRect = new Rectangle(x * frameSize, y * frameSize, frameSize, frameSize);
        }

        public NamedImageFrame(string name, Rectangle sourceRect)
        {
            AssetName = name;
            Image = TextureManager.GetTexture(name);
            SourceRect = sourceRect;
        }
    }

}