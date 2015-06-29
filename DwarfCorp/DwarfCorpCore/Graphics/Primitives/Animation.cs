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
        public int CurrentFrame { get; set; }
        public bool IsPlaying { get; set; }
        public bool Loops { get; set; }
        public Color Tint { get; set; }
        public float FrameHZ { get; set; }
        public List<float> Speeds { get; set; } 
        private float FrameTimer { get; set; }
        public float WorldWidth { get; set; }
        public float WorldHeight { get; set; }
        public bool Flipped { get; set; }
        public float SpeedMultiplier { get; set; }

        [JsonIgnore]
        public List<BillboardPrimitive> Primitives { get; set; }

        public SpriteSheet SpriteSheet { get; set; }


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
            Primitives = new List<BillboardPrimitive>();
            SpriteSheet = null;
            Frames = new List<Point>();
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }

        public Animation(SimpleDescriptor descriptor) :
            this(descriptor.AssetName, descriptor.Width, descriptor.Height, descriptor.Frames.ToArray())
        {
            SpeedMultiplier = descriptor.Speed;
            Play();
        }

        public Animation(Animation other, SpriteSheet spriteSheet, GraphicsDevice device)
            : this(device, spriteSheet, other.Name, other.FrameWidth, other.FrameHeight, other.Frames, other.Loops, other.Tint, other.FrameHZ, other.WorldWidth, other.WorldHeight, other.Flipped)
        {
            Speeds = new List<float>();
            Speeds.AddRange(other.Speeds);
            SpeedMultiplier = 1.0f;
        }


        public Animation(GraphicsDevice device, SpriteSheet sheet, string name, List<Point> frames, bool loops, Color tint, float frameHZ, bool flipped) :
            this(device, sheet, name, sheet.Width, sheet.Height, frames, loops, tint, frameHZ, sheet.Width / 32.0f, sheet.Height / 32.0f, flipped)
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
            this(GameState.Game.GraphicsDevice, new SpriteSheet(asset), asset, frameWidth, frameHeigt, new List<Point>(), false, Color.White, 15.0f, 1.0f, 1.0f, false)
        {
            Frames = new List<Point>();
            foreach (int i in frames)
            {
                Frames.Add(new Point(i, row));
            }
            CreatePrimitives(GameState.Game.GraphicsDevice);
            Speeds = new List<float>();
 
        }

        public Animation(NamedImageFrame frame) :
            this(GameState.Game.GraphicsDevice, new SpriteSheet(frame.AssetName), frame.AssetName, frame.SourceRect.Width, frame.SourceRect.Height, new List<Point>(), false, Color.White, 15.0f, frame.SourceRect.Width / 32.0f, frame.SourceRect.Height / 32.0f, false)
        {
            Frames.Add(new Point(frame.SourceRect.X / frame.SourceRect.Width, frame.SourceRect.Y / frame.SourceRect.Height));
            CreatePrimitives(GameState.Game.GraphicsDevice);
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }

        public Animation(GraphicsDevice device, SpriteSheet sheet, string name, int frameWidth, int frameHeight, List<Point> frames, bool loops, Color tint, float frameHZ, float worldWidth, float worldHeight, bool flipped)
        {
            Name = name;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            Frames = frames;
            CurrentFrame = 0;
            IsPlaying = false;
            Loops = loops;
            Tint = tint;
            Speeds = new List<float>() {frameHZ + MathFunctions.Rand()};
            FrameHZ = frameHZ;
            FrameTimer = 0.0f;
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            SpriteSheet = sheet;
            Primitives = new List<BillboardPrimitive>();
            Flipped = flipped;
            SpeedMultiplier = 1.0f;
            CreatePrimitives(device);
        }




        public void CreatePrimitives(GraphicsDevice device)
        {
            foreach(Point frame in Frames)
            {
                string key = GetHashCode() + ": " + FrameWidth + "," + FrameHeight + frame.ToString() + "," + Flipped;
                if(!PrimitiveLibrary.BillboardPrimitives.ContainsKey(key))
                {
                    PrimitiveLibrary.BillboardPrimitives[key] = new BillboardPrimitive(device, SpriteSheet.GetTexture(), FrameWidth, FrameHeight, frame, WorldWidth, WorldHeight, Flipped);
                }

                Primitives.Add(PrimitiveLibrary.BillboardPrimitives[key]);
            }
        }

        public virtual Rectangle GetCurrentFrameRect()
        {
            Rectangle toReturn = new Rectangle(Frames[CurrentFrame].X * FrameWidth, Frames[CurrentFrame].Y * FrameHeight, FrameWidth, FrameHeight);
            return toReturn;
        }

        public void Reset()
        {
            CurrentFrame = 0;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Play()
        {
            IsPlaying = true;
        }

        public void Stop()
        {
            IsPlaying = false;
            CurrentFrame = 0;
        }

        public void Loop()
        {
            IsPlaying = true;
            Loops = true;
        }

        public void StopLooping()
        {
            IsPlaying = false;
            Loops = false;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            CreatePrimitives(GameState.Game.GraphicsDevice);
        }

        public virtual void Update(DwarfTime gameTime, Timer.TimerMode mode = Timer.TimerMode.Game)
        {
            if(IsPlaying)
            {
                float dt = mode == Timer.TimerMode.Game ? (float)gameTime.ElapsedGameTime.TotalSeconds : (float)gameTime.ElapsedRealTime.TotalSeconds;
                FrameTimer += dt;

                float time = FrameHZ;

                if (Speeds.Count > 0)
                {
                    time = Speeds[Math.Min(CurrentFrame, Speeds.Count - 1)];
                }
                if(FrameTimer * SpeedMultiplier >= 1.0f / time)
                {
                    NextFrame();
                    FrameTimer = 0.0f;
                }
            }
        }

        public virtual void NextFrame()
        {
            CurrentFrame++;

            if(CurrentFrame >= Frames.Count)
            {
                if(Loops)
                {
                    CurrentFrame = 0;
                }
                else
                {
                    CurrentFrame = Frames.Count - 1;
                }
            }
        }

        public virtual Animation Clone()
        {
            return new Animation(this, SpriteSheet, GameState.Game.GraphicsDevice);
        }

        public virtual void PreRender()
        {
            
        }
    }

}