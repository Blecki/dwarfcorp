using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// An animation flips a billboard sprite between several
    /// frames on a sprite sheet at a fixed rate.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Animation
    {

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            //throw new InvalidOperationException();
        }

        public string Name { get; set; }
        public List<Point> Frames { get; set; }
        public Color Tint { get; set; }
        public float FrameHZ { get; set; }
        public List<float> Speeds { get; set; } 
        public List<float> YOffset { get; set; }
        public bool Flipped { get; set; }
        public float SpeedMultiplier { get; set; }
        public bool Loops = false;

        [JsonIgnore]
        public virtual bool CanUseInstancing { get { return true; } }

        public SpriteSheet SpriteSheet { get; set; }

        public virtual int GetFrameCount()
        {
            return Frames.Count;
        }

        public struct SimpleDescriptor
        {
            public string AssetName;
            public int Width;
            public int Height;
            public List<int> Frames;
            public float Speed;
            public float YOffset;
        }

        public Animation()
        {
            SpriteSheet = null;
            Frames = new List<Point>();
            Speeds = new List<float>();
            YOffset = new List<float>();
            SpeedMultiplier = 1.0f;
            Tint = Color.White;
        }

        private Rectangle GetFrameRect(int Frame)
        {
            Rectangle toReturn = new Rectangle(Frames[Frame].X * SpriteSheet.FrameWidth, Frames[Frame].Y * SpriteSheet.FrameHeight, SpriteSheet.FrameWidth, SpriteSheet.FrameHeight);
            return toReturn;
        }

        public virtual void UpdatePrimitive(BillboardPrimitive Primitive, int CurrentFrame)
        {
            if (CurrentFrame >= Frames.Count)
                return;
            var rect = GetFrameRect(CurrentFrame);

            // Don't scale here - sprite will be scaled by the world matrix.
            Primitive.SetFrame(SpriteSheet, rect, 1.0f, 1.0f, /*rect.Width / 32.0f, rect.Height / 32.0f,*/ Color.White, Tint, Flipped);
        }

        public virtual NamedImageFrame GetAsImageFrame(int CurrentFrame)
        {
            return new NamedImageFrame(SpriteSheet.AssetName, GetFrameRect(CurrentFrame));
        }

        public virtual Texture2D GetTexture()
        {
            return SpriteSheet.GetTexture();
        }
    }
}