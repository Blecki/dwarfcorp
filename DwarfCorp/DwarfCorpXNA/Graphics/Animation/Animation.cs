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
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public string Name { get; set; }
        public List<Point> Frames { get; set; }
        public Color Tint { get; set; }
        public float FrameHZ { get; set; }
        public List<float> Speeds { get; set; } 
        private float FrameTimer { get; set; }
        public float WorldWidth { get; set; }
        public float WorldHeight { get; set; }
        public bool Flipped { get; set; }
        public float SpeedMultiplier { get; set; }
        public bool Loops = false;

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
        }

        public Animation()
        {
            SpriteSheet = null;
            Frames = new List<Point>();
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }

        public Animation(SimpleDescriptor descriptor) :
            this(descriptor.AssetName, descriptor.Width, descriptor.Height, descriptor.Frames.ToArray())
        {
            SpeedMultiplier = descriptor.Speed;
        }

        public Animation(Animation other, SpriteSheet spriteSheet, GraphicsDevice device)
            : this(device, spriteSheet, other.Name, other.FrameWidth, other.FrameHeight, other.Frames, other.Tint, other.FrameHZ, other.WorldWidth, other.WorldHeight, other.Flipped)
        {
            Speeds = new List<float>();
            Speeds.AddRange(other.Speeds);
            SpeedMultiplier = 1.0f;
        }


        public Animation(GraphicsDevice device, SpriteSheet sheet, string name, List<Point> frames, Color tint, float frameHZ, bool flipped) :
            this(device, sheet, name, sheet.FrameWidth, sheet.FrameHeight, frames, tint, frameHZ, sheet.FrameWidth / 32.0f, sheet.FrameHeight / 32.0f, flipped)
        {
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }


        public Animation(string asset, int frameWidth, int frameHeigt, params int[] frames) :
             this(0, asset, frameWidth, frameHeigt, frames)
        {
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }

        public Animation(int row, string asset, int frameWidth, int frameHeigt, params int[] frames) :
            this(GameState.Game.GraphicsDevice, new SpriteSheet(asset), asset, frameWidth, frameHeigt, new List<Point>(), Color.White, 15.0f, 1.0f, 1.0f, false)
        {
            Frames = new List<Point>();
            foreach (int i in frames)
            {
                Frames.Add(new Point(i, row));
            }
            Speeds = new List<float>();
 
        }

        public Animation(NamedImageFrame frame) :
            this(GameState.Game.GraphicsDevice, new SpriteSheet(frame.AssetName), frame.AssetName, frame.SourceRect.Width, frame.SourceRect.Height, new List<Point>(), Color.White, 15.0f, frame.SourceRect.Width / 32.0f, frame.SourceRect.Height / 32.0f, false)
        {
            Frames.Add(new Point(frame.SourceRect.X / frame.SourceRect.Width, frame.SourceRect.Y / frame.SourceRect.Height));
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }

        public Animation(GraphicsDevice device, SpriteSheet sheet, string name, int frameWidth, int frameHeight, List<Point> frames, Color tint, float frameHZ, float worldWidth, float worldHeight, bool flipped)
        {
            Name = name;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            Frames = frames;
            Tint = tint;
            Speeds = new List<float>() {frameHZ + MathFunctions.Rand()};
            FrameHZ = frameHZ;
            FrameTimer = 0.0f;
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            SpriteSheet = sheet;
            Flipped = flipped;
            SpeedMultiplier = 1.0f;
        }

        public virtual Rectangle GetFrameRect(int Frame)
        {
            Rectangle toReturn = new Rectangle(Frames[Frame].X * FrameWidth, Frames[Frame].Y * FrameHeight, FrameWidth, FrameHeight);
            return toReturn;
        }

        public virtual void UpdatePrimitive(BillboardPrimitive Primitive, int CurrentFrame)
        {
            if (CurrentFrame >= Frames.Count)
                return;
            var rect = GetFrameRect(CurrentFrame);
            Primitive.SetFrame(SpriteSheet, rect, WorldWidth, WorldHeight, Color.White, Tint, Flipped);
        }
    }
}