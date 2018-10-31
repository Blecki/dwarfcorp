// ImageFrame.cs
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
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
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
        protected bool Equals(ImageFrame other)
        {
            return Equals(Image, other.Image) && SourceRect.Equals(other.SourceRect);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Image != null ? Image.GetHashCode() : 0)*397) ^ SourceRect.GetHashCode();
            }
        }

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

        public override bool Equals(object obj)
        {
            return obj is ImageFrame && Equals((ImageFrame) obj);
        }

    }

    [JsonObject(IsReference = true)]
    public class LayeredImage
    {
        public List<NamedImageFrame> Images { get; set; }
        public List<Color> Tints { get; set; } 

        public LayeredImage()
        {
            Images = new List<NamedImageFrame>();
        }

        public void Render(Rectangle location)
        {
            for (int i = 0; i < Images.Count; i++)
            {
                DwarfGame.SpriteBatch.Draw(Images[i].SafeGetImage(), location, Images[i].SourceRect, Tints[i]);
            }
        }
    }

    public class SpriteSheetReference
    {
        public string Asset;
        public int TileWidth;
        public int TileHeight;
        public int Tile;

        public NamedImageFrame ToImageFrame()
        {
            Microsoft.Xna.Framework.Graphics.Texture2D realTexture = AssetManager.GetContentTexture(Asset);
            int numTilesX = realTexture.Width / TileWidth;
            int y = Tile / numTilesX;
            int x = Tile % numTilesX;
            return new NamedImageFrame(Asset, new Rectangle(x, y, TileWidth, TileHeight));
        }

        public KeyValuePair<SpriteSheet, Point> ToSpriteSheet()
        {
            Microsoft.Xna.Framework.Graphics.Texture2D realTexture = AssetManager.GetContentTexture(Asset);
            int numTilesX = realTexture.Width / TileWidth;
            int y = Tile / numTilesX;
            int x = Tile % numTilesX;
            return new KeyValuePair<SpriteSheet, Point>(new SpriteSheet(Asset, TileWidth, TileHeight), new Point(x, y));
        }
    }
    


    [JsonObject(IsReference = true)]
    public class NamedImageFrame : ImageFrame
    {
        public string AssetName { get; set; }

        public Texture2D SafeGetImage()
        {
            if (Image == null || Image.IsDisposed || Image.GraphicsDevice.IsDisposed)
            {
                Image = AssetManager.GetContentTexture(AssetName);
            }
            return Image;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Image = AssetManager.GetContentTexture(AssetName);
        }

        public NamedImageFrame()
        {
            
        }

        public NamedImageFrame(string name)
        {
            AssetName = name;
            Image = AssetManager.GetContentTexture(name);
            if (Image != null)
            {
                SourceRect = Image.Bounds;
            }
        }

        public NamedImageFrame(Texture2D tex, int frameSize, int x, int y) :
            base(tex, frameSize, x, y)
        {
            AssetName = ContentPaths.Error;
        }

        public NamedImageFrame(Texture2D tex, Rectangle sourceRect) :
                base(tex, sourceRect)
        {
            AssetName = ContentPaths.Error;
        }


        public NamedImageFrame(string name, int frameSize, int x, int y)
        {
            AssetName = name;
            Image = AssetManager.GetContentTexture(name);
            SourceRect = new Rectangle(x * frameSize, y * frameSize, frameSize, frameSize);
        }

        public NamedImageFrame(string name, Rectangle sourceRect)
        {
            AssetName = name;
            Image = AssetManager.GetContentTexture(name);
            SourceRect = sourceRect;
        }
    }



}