using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class SpriteSheet
    {
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public string AssetName { get; set; }

        public SpriteSheet()
        {
            FrameWidth = -1;
            FrameHeight = -1;
            AssetName = "";
        }

        public SpriteSheet(string name, int frameSize)
        {
            FrameWidth = frameSize;
            FrameHeight = frameSize;
            AssetName = name;
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
    }
}
