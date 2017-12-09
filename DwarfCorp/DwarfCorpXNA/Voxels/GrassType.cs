using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class GrassType
    {
        public class FringeTileUV
        {
            public Vector2 UV;
            public Vector4 Bounds;

            public FringeTileUV(int x, int y, int textureWidth, int textureHeight)
            {
                UV = new Microsoft.Xna.Framework.Vector2((float)x / (float)textureWidth,
                    (float)y / (float)textureHeight);
                Bounds = new Microsoft.Xna.Framework.Vector4((float)x / (float)textureWidth + 0.001f,
                    (float)y / (float)textureHeight + 0.001f, (float)(x + 1) / (float)textureWidth - 0.001f,
                    (float)(y + 1) / (float)textureHeight - 0.001f);
            }
        }

        public byte ID;
        public String Name;

        public Point Tile;
        public Point[] FringeTiles = null;
        [JsonIgnore]
        public FringeTileUV[] FringeTransitionUVs = null;

        public int FringePrecedence = 0;
        public String BecomeWhenSnowedOn = null;
        public String BecomeWhenDecays = null;
        public bool Decay = false;
        public byte InitialDataValue = 0;
    }
}
