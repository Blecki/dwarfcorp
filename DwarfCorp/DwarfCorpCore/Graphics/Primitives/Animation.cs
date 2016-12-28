using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     An animation is a set of frames on a sprite sheet, and associated 3D geometry. An Animation also
    ///     keeps track of animation speed, the current frame, and other miscellanious book keeping.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Animation
    {
        /// <summary>
        ///     Called whenever the animation ends.
        /// </summary>
        public delegate void EndEvent();

        /// <summary>
        ///     If the animation loops, gets called whenever the loop
        ///     starts again.
        /// </summary>
        public delegate void LoopEvent();

        /// <summary>
        ///     Called whenever a new frame is displayed.
        /// </summary>
        /// <param name="frame">The frame getting displayed.</param>
        public delegate void NextFrameEvent(int frame);

        /// <summary>
        ///     Constructs the default animation with empty frames and no sprite sheet.
        /// </summary>
        public Animation()
        {
            Primitives = new List<BillboardPrimitive>();
            SpriteSheet = null;
            Frames = new List<Point>();
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }

        /// <summary>
        ///     Create an animation using a simplified descriptor
        /// </summary>
        /// <param name="descriptor">A simplified descriptor of the animation.</param>
        public Animation(SimpleDescriptor descriptor) :
            this(descriptor.AssetName, descriptor.Width, descriptor.Height, descriptor.Frames.ToArray())
        {
            SpeedMultiplier = descriptor.Speed;
            Play();
        }

        /// <summary>
        ///     Constructs an animation using another animation as a template, but with a different sprite sheet.
        ///     This is useful for re-skinning animations.
        /// </summary>
        /// <param name="other">The Animation to copy.</param>
        /// <param name="spriteSheet">The new sprite sheet to use.</param>
        /// <param name="device">The graphics device (used for constructing new vertex buffers)</param>
        public Animation(Animation other, SpriteSheet spriteSheet, GraphicsDevice device)
            : this(
                device, spriteSheet, other.Name, other.FrameWidth, other.FrameHeight, other.Frames, other.Loops,
                other.Tint, other.FrameHZ, other.WorldWidth, other.WorldHeight, other.Flipped)
        {
            Speeds = new List<float>();
            Speeds.AddRange(other.Speeds);
            SpeedMultiplier = 1.0f;
        }


        /// <summary>
        ///     Creates an animation based on its frames, asset and speed.
        /// </summary>
        /// <param name="device">Graphics device to construct vertex buffers using.</param>
        /// <param name="sheet">The sprite sheet associated with this animation.</param>
        /// <param name="name">The unique name of the animation.</param>
        /// <param name="frames">The list of (x, y) grid positions of the frames.</param>
        /// <param name="loops">True if the animation loops.</param>
        /// <param name="tint">Optionally multiply the RGBA color of the animation with this.</param>
        /// <param name="frameHZ">Speed in Hz of the animation.</param>
        /// <param name="flipped">If true, flips the animation horizontally.</param>
        public Animation(GraphicsDevice device, SpriteSheet sheet, string name, List<Point> frames, bool loops,
            Color tint, float frameHZ, bool flipped) :
                this(
                device, sheet, name, sheet.Width, sheet.Height, frames, loops, tint, frameHZ, sheet.Width/32.0f,
                sheet.Height/32.0f, flipped)
        {
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }


        /// <summary>
        ///     Simplified Animation constructor that just takes an image asset and row-major order frames. The
        ///     default speed is used.
        /// </summary>
        /// <param name="asset">The string identifier of the animation asset.</param>
        /// <param name="frameWidth">The width of a frame in pixes.</param>
        /// <param name="frameHeigt">The height of a frame in pixels.</param>
        /// <param name="frames">A list of row-major order frames in the animation.</param>
        public Animation(string asset, int frameWidth, int frameHeigt, params int[] frames) :
            this(0, asset, frameWidth, frameHeigt, frames)
        {
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }

        /// <summary>
        ///     Simplified Animation constructor that takes in an asset and row-relative frames.
        /// </summary>
        /// <param name="row">The row in the sprite shet to use (top-to-bottom)</param>
        /// <param name="asset">The image asset associated with the animation.</param>
        /// <param name="frameWidth">The width in pixels of each frame in the animation.</param>
        /// <param name="frameHeigt">The heiht in pixels of each frame in the animation.</param>
        /// <param name="frames">A list of columns to use for frames of the animation (using the row specified)</param>
        public Animation(int row, string asset, int frameWidth, int frameHeigt, params int[] frames) :
            this(
            GameState.Game.GraphicsDevice, new SpriteSheet(asset), asset, frameWidth, frameHeigt, new List<Point>(),
            false, Color.White, 15.0f, 1.0f, 1.0f, false)
        {
            Frames = new List<Point>();
            foreach (int i in frames)
            {
                Frames.Add(new Point(i, row));
            }
            CreatePrimitives(GameState.Game.GraphicsDevice);
            Speeds = new List<float>();
        }

        /// <summary>
        ///     Constructs a single-frame animation from a sprite sheet.
        /// </summary>
        /// <param name="frame">The asset and source rectangle of the asset to use.</param>
        public Animation(NamedImageFrame frame) :
            this(
            GameState.Game.GraphicsDevice, new SpriteSheet(frame.AssetName), frame.AssetName, frame.SourceRect.Width,
            frame.SourceRect.Height, new List<Point>(), false, Color.White, 15.0f, frame.SourceRect.Width/32.0f,
            frame.SourceRect.Height/32.0f, false)
        {
            Frames.Add(new Point(frame.SourceRect.X/frame.SourceRect.Width, frame.SourceRect.Y/frame.SourceRect.Height));
            CreatePrimitives(GameState.Game.GraphicsDevice);
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
        }

        /// <summary>
        ///     Most versatile animation constructor.
        /// </summary>
        /// <param name="device">Graphics device to use to create vertex buffers.</param>
        /// <param name="sheet">Sprite sheet to base the animation on.</param>
        /// <param name="name">Unique name of the animation.</param>
        /// <param name="frameWidth">Width of the frames in pixels.</param>
        /// <param name="frameHeight">Height of the frames in pixels.</param>
        /// <param name="frames">List of (x, y) frame grid positions.</param>
        /// <param name="loops">When true, the animation will loop.</param>
        /// <param name="tint">Optionally multiply the pixel RGBA values by this amount.</param>
        /// <param name="frameHZ">The speed of the animation in Hz.</param>
        /// <param name="worldWidth">The width of the animation billboard in voxels.</param>
        /// <param name="worldHeight">The height of the animation billboard in voxels.</param>
        /// <param name="flipped">If true, flips the animation horizontally.</param>
        public Animation(GraphicsDevice device, SpriteSheet sheet, string name, int frameWidth, int frameHeight,
            List<Point> frames, bool loops, Color tint, float frameHZ, float worldWidth, float worldHeight, bool flipped)
        {
            Name = name;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            Frames = frames;
            CurrentFrame = 0;
            IsPlaying = false;
            Loops = loops;
            Tint = tint;
            Speeds = new List<float> {frameHZ + MathFunctions.Rand()};
            FrameHZ = frameHZ;
            FrameTimer = 0.0f;
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            SpriteSheet = sheet;
            Primitives = new List<BillboardPrimitive>();
            Flipped = flipped;
            SpeedMultiplier = 1.0f;
            CreatePrimitives(device);
            Play();
        }

        /// <summary>
        ///     Width of a frame of the animation in pixels.
        ///     All animations have frames of the same size.
        /// </summary>
        public int FrameWidth { get; set; }

        /// <summary>
        ///     Height of a frame of the animation in pixels.
        ///     All animations have frames of the same size.
        /// </summary>
        public int FrameHeight { get; set; }

        /// <summary>
        ///     Animations are uniquely described by their names.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     List of (X, Y) positions of all the frames in the
        ///     animation. These are grid positions, not pixel locations.
        ///     For example, a 64 x 64 image with 32 x 32 frames will have
        ///     four grid positions (0, 0), (0, 1), (1, 0) and (1, 1), and
        ///     the frames come from this list of grid positions.
        /// </summary>
        public List<Point> Frames { get; set; }

        /// <summary>
        ///     The current animation frame to be displayed, from the list of
        ///     Frames.
        /// </summary>
        public int CurrentFrame { get; set; }

        /// <summary>
        ///     The previous frame that was displayed. -1 if no frame was displayed
        ///     previously.
        /// </summary>
        public int LastFrame { get; set; }

        /// <summary>
        ///     True if the animation is swapping frames over time. False if the
        ///     animation is paused.
        /// </summary>
        public bool IsPlaying { get; set; }

        /// <summary>
        ///     If true, when the current frame reaches the end of the list, the
        ///     first frame is displayed again.
        /// </summary>
        public bool Loops { get; set; }

        /// <summary>
        ///     Optionally multiply the colors in the animation by this value.
        /// </summary>
        public Color Tint { get; set; }

        /// <summary>
        ///     The speed of the animation in Hz. Only used if Speeds is not defined.
        /// </summary>
        public float FrameHZ { get; set; }

        /// <summary>
        ///     List of times that each frame is displayed in Seconds.
        /// </summary>
        public List<float> Speeds { get; set; }

        /// <summary>
        ///     The time that has elapsed for the current animation frame.
        /// </summary>
        private float FrameTimer { get; set; }

        /// <summary>
        ///     The width in voxels that this frame takes up in the world.
        /// </summary>
        public float WorldWidth { get; set; }

        /// <summary>
        ///     The height in voxels that this frame takes up in the world.
        /// </summary>
        public float WorldHeight { get; set; }

        /// <summary>
        ///     If true, the animation flips the pixel data horizontally before displaying.
        /// </summary>
        public bool Flipped { get; set; }

        /// <summary>
        ///     Multiplies FrameHz or divides Speed by this amount. For example, if
        ///     Speed multiplier is 2.0, the animation plays twice as fast as normal.
        /// </summary>
        public float SpeedMultiplier { get; set; }

        /// <summary>
        ///     List of vertex buffers associated with this animation.
        /// </summary>
        [JsonIgnore]
        public List<BillboardPrimitive> Primitives { get; set; }

        /// <summary>
        ///     Defines the sprite sheet that this animation comes from.
        /// </summary>
        public SpriteSheet SpriteSheet { get; set; }

        /// <summary>
        ///     Event that gets called whenever a new frame is displayed.
        /// </summary>
        public event NextFrameEvent OnNextFrame;

        /// <summary>
        ///     Event that gets called whenever the animation ends.
        /// </summary>
        public event EndEvent OnAnimationCompleted;

        /// <summary>
        ///     Event that gets called whenever the animation loops back to
        ///     frame 0.
        /// </summary>
        public event LoopEvent OnAnimationLooped;

        /// <summary>
        ///     Invoke the loop delegate and inform subscribers.
        /// </summary>
        protected virtual void InvokeAnimationLooped()
        {
            LoopEvent handler = OnAnimationLooped;
            if (handler != null) handler.Invoke();
        }

        /// <summary>
        ///     Invoke the animation completed delegate and inform
        ///     subscribers.
        /// </summary>
        protected virtual void InvokeAnimationCompleted()
        {
            EndEvent handler = OnAnimationCompleted;
            if (handler != null) OnAnimationCompleted.Invoke();
        }

        /// <summary>
        ///     Invoke the "NextFrame" delegate and inform subscribers.
        /// </summary>
        /// <param name="frame">The frame to display.</param>
        protected virtual void InvokeNextFrame(int frame)
        {
            NextFrameEvent handler = OnNextFrame;
            if (handler != null)
            {
                handler.Invoke(frame);
            }
        }

        /// <summary>
        ///     Generates a list of vertex buffers to use for this animation. Caches them in a library.
        /// </summary>
        /// <param name="device">The graphics device to create vertex buffers using.</param>
        public void CreatePrimitives(GraphicsDevice device)
        {
            foreach (Point frame in Frames)
            {
                // We want to find a unique identifier for each frame of the animation.
                string key = GetHashCode() + ": " + FrameWidth + "," + FrameHeight + frame + "," + Flipped;

                // If the billboard doesn't exist yet, create a new one.
                if (!PrimitiveLibrary.BillboardPrimitives.ContainsKey(key))
                {
                    PrimitiveLibrary.BillboardPrimitives[key] = new BillboardPrimitive(SpriteSheet.GetTexture(),
                        FrameWidth, FrameHeight, frame, WorldWidth, WorldHeight, Tint, Flipped);
                }

                // We want to indirectly reference billboards from a library to avoid duplicating many billboards in memory.
                Primitives.Add(PrimitiveLibrary.BillboardPrimitives[key]);
            }
        }

        /// <summary>
        ///     Returns an image-relative rectangle in pixels of the current frame.
        /// </summary>
        /// <returns>A rectangle bounding box describing the currently displayed frame in the image.</returns>
        public virtual Rectangle GetCurrentFrameRect()
        {
            var toReturn = new Rectangle(Frames[CurrentFrame].X*FrameWidth, Frames[CurrentFrame].Y*FrameHeight,
                FrameWidth, FrameHeight);
            return toReturn;
        }

        /// <summary>
        ///     Just set the current frame to 0.
        /// </summary>
        public void Reset()
        {
            CurrentFrame = 0;
        }

        /// <summary>
        ///     Stop playing the animation.
        /// </summary>
        public void Pause()
        {
            IsPlaying = false;
        }

        /// <summary>
        ///     Start plalying the animation.
        /// </summary>
        public void Play()
        {
            IsPlaying = true;
        }

        /// <summary>
        ///     Stop playing the animation and reset it.
        /// </summary>
        public void Stop()
        {
            IsPlaying = false;
            Reset();
        }

        /// <summary>
        ///     Start the animation playing, and ensure that it loops.
        /// </summary>
        public void Loop()
        {
            IsPlaying = true;
            Loops = true;
        }

        /// <summary>
        ///     Stops the animation playing and ensures that it won't loop again.
        /// </summary>
        public void StopLooping()
        {
            IsPlaying = false;
            Loops = false;
        }

        /// <summary>
        ///     When the animation is deserialized from a JSON file, re-create all of its
        ///     billboards.
        /// </summary>
        /// <param name="context">Parameters of the deserialization.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            CreatePrimitives(GameState.Game.GraphicsDevice);
        }

        /// <summary>
        ///     Update the animation, updating the frame to be displayed.
        /// </summary>
        /// <param name="gameTime">The current time.</param>
        /// <param name="mode">Either update the animation using game time or real time. (Game time can be paused or sped up).</param>
        public virtual void Update(DwarfTime gameTime, Timer.TimerMode mode = Timer.TimerMode.Game)
        {
            if (IsPlaying)
            {
                LastFrame = CurrentFrame;
                float dt = mode == Timer.TimerMode.Game
                    ? (float) gameTime.ElapsedGameTime.TotalSeconds
                    : (float) gameTime.ElapsedRealTime.TotalSeconds;
                FrameTimer += dt;

                float time = FrameHZ;

                // Override FrameHz if Speed per frame is specified.
                if (Speeds.Count > 0)
                {
                    time = Speeds[Math.Min(CurrentFrame, Speeds.Count - 1)];
                }
                if (FrameTimer*SpeedMultiplier >= 1.0f/time)
                {
                    NextFrame();
                    FrameTimer = 0.0f;
                }
            }
        }

        /// <summary>
        ///     Does all the bookkeeping needed whenever we go to the next frame.
        ///     Subscribers can watch an animation for a specific frame (for example, to trigger
        ///     damage).
        /// </summary>
        public virtual void NextFrame()
        {
            CurrentFrame++;
            InvokeNextFrame(CurrentFrame);

            if (CurrentFrame >= Frames.Count)
            {
                if (Loops)
                {
                    InvokeAnimationLooped();
                    CurrentFrame = 0;
                }
                else
                {
                    InvokeAnimationCompleted();
                    CurrentFrame = Frames.Count - 1;
                }
            }
        }

        /// <summary>
        ///     Creates a deep copy of this animation.
        /// </summary>
        /// <returns>A new animation which is a deep copy of this one.</returns>
        public virtual Animation Clone()
        {
            return new Animation(this, SpriteSheet, GameState.Game.GraphicsDevice);
        }

        /// <summary>
        ///     Virtual function called just before the animation is rendered.
        /// </summary>
        public virtual void PreRender()
        {
        }

        /// <summary>
        ///     Returns true if the animation has completed (and is not looping)
        /// </summary>
        /// <returns>True if the animation has completed (and is not looping)</returns>
        public virtual bool IsDone()
        {
            return CurrentFrame >= Frames.Count - 1;
        }

        /// <summary>
        ///     This is a simplified descriptor of animations to make it easier to define them
        ///     in JSON files. It restricts the kinds of animations that can be described to a
        ///     subset of animations with fixed speed and simplified layout.
        /// </summary>
        public struct SimpleDescriptor
        {
            /// <summary>
            ///     The name of the asset associated with the animation.
            /// </summary>
            public string AssetName;

            /// <summary>
            ///     A list of frames in the animation. These are in row-major order.
            ///     (that is, starting from the top left, reading left to right top to bottom).
            /// </summary>
            public List<int> Frames;

            /// <summary>
            ///     The height of each frame in pixels.
            /// </summary>
            public int Height;

            /// <summary>
            ///     The number of seconds between each frame.
            /// </summary>
            public float Speed;

            /// <summary>
            ///     The width of each frame in pixels.
            /// </summary>
            public int Width;
        }
    }
}