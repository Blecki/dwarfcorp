using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class SpriteSheet
    {
        protected bool Equals(SpriteSheet other)
        {
            return FrameWidth == other.FrameWidth && FrameHeight == other.FrameHeight && string.Equals(AssetName, other.AssetName);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = FrameWidth;
                hashCode = (hashCode*397) ^ FrameHeight;
                hashCode = (hashCode*397) ^ (AssetName != null ? AssetName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public string AssetName { get; set; }

        public SpriteSheet()
        {
            FrameWidth = -1;
            FrameHeight = -1;
            AssetName = "";
        }

        public SpriteSheet(string name)
        {
            AssetName = name;
            Texture2D tex = TextureManager.GetTexture(name);
            FrameWidth = tex.Width;
            FrameHeight = tex.Height;
        }

        public SpriteSheet(string name, int frameWidth, int frameHeight)
        {
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            AssetName = name;
        }

        public SpriteSheet(string name, int frameSize)
        {
            FrameWidth = frameSize;
            FrameHeight = frameSize;
            AssetName = name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SpriteSheet) obj);
        }

        public List<NamedImageFrame> GenerateFrames()
        {
            List<NamedImageFrame> toReturn = new List<NamedImageFrame>();
            Texture2D texture = TextureManager.GetTexture(AssetName);

            if (texture == null) return null;

            for (int r = 0; r < texture.Height/FrameHeight; r++)
            {
                for (int c = 0; c < texture.Height/FrameHeight; c++)
                {
                    toReturn.Add(new NamedImageFrame(AssetName, FrameWidth, c, r));
                }
            }

            return toReturn;
        }

        public NamedImageFrame GenerateFrame(Point position)
        {
            return new NamedImageFrame(AssetName, new Rectangle(position.X * FrameWidth, position.Y * FrameHeight, FrameWidth, FrameHeight));
        }
    }
}
