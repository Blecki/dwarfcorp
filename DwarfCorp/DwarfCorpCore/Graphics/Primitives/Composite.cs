using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A composite is a list of sub-textures which are overlaid on one another
    /// to produce a layered composite texture. This is useful for layering multiple
    /// sprites on top of one another (For example, for giving dwarves different weapons).
    /// This is accomplished by laying sprites out on a RenderTarget using SpriteBatch.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    [JsonObject(IsReference = true)]
    public class Composite : IDisposable
    {
        /// <summary>
        /// The current offset into the composite texture (grid cell).
        /// </summary>
        private Point CurrentOffset;
        /// <summary>
        /// If true, the composite has been rendered.
        /// </summary>
        public bool HasRendered = false;

        public Composite()
        {
            CurrentFrames = new Dictionary<Frame, Point>();
            CurrentOffset = new Point(0, 0);
        }


        public Composite(List<Frame> frames)
        {
            CurrentFrames = new Dictionary<Frame, Point>();
            CurrentOffset = new Point(0, 0);

            FrameSize = new Point(32, 32);
            TargetSizeFrames = new Point(8, 8);

            Initialize();
        }

        /// <summary>
        /// Gets or sets the render target that this composite is rendered to.
        /// </summary>
        /// <value>
        /// The target.
        /// </value>
        public RenderTarget2D Target { get; set; }
        /// <summary>
        /// Gets or sets the size of each frame in pixels.
        /// </summary>
        /// <value>
        /// The size of the frame.
        /// </value>
        public Point FrameSize { get; set; }
        /// <summary>
        /// This is the size of the render target in frames. 
        /// For example, if the render target was 128 x 128 and the
        /// FrameSize was 32 x 32, then TargetSizeFrames = 4 x 4
        /// </summary>
        /// <value>
        /// The target size frames.
        /// </value>
        public Point TargetSizeFrames { get; set; }

        /// <summary>
        /// A dictionary from frames in the composite to their
        /// locations on the render target.
        /// </summary>
        /// <value>
        /// The current frames.
        /// </value>
        private Dictionary<Frame, Point> CurrentFrames { get; set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Target != null && !Target.IsDisposed)
                Target.Dispose();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            Target = new RenderTarget2D(GameState.Game.GraphicsDevice, FrameSize.X*TargetSizeFrames.X,
                FrameSize.Y*TargetSizeFrames.Y, false, SurfaceFormat.Color, DepthFormat.None);
        }

        /// <summary>
        /// Creates a new billboard for the specified frame.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="frame">The frame to use.</param>
        /// <returns>A new billboard UV mapped to that coordinate.</returns>
        public BillboardPrimitive CreatePrimitive(GraphicsDevice device, Point frame)
        {
            string key = Target.GetHashCode() + ": " + FrameSize.X + "," + FrameSize.Y + " " + frame.X + " " + frame.Y;
            if (!PrimitiveLibrary.BillboardPrimitives.ContainsKey(key))
            {
                PrimitiveLibrary.BillboardPrimitives[key] = new BillboardPrimitive(Target, FrameSize.X, FrameSize.Y,
                    new Point(0, 0), FrameSize.X/32.0f, FrameSize.Y/32.0f, Color.White);
            }

            return PrimitiveLibrary.BillboardPrimitives[key];
        }

        /// <summary>
        /// Changes the UV coordinates of a Billboard to match the frame offset given.
        /// </summary>
        /// <param name="primitive">The primitive.</param>
        /// <param name="offset">The offset.</param>
        public void ApplyBillboard(BillboardPrimitive primitive, Point offset)
        {
            primitive.UVs = new BillboardPrimitive.BoardTextureCoords(Target.Width, Target.Height, FrameSize.X,
                FrameSize.Y, offset, false);
            primitive.UpdateVertexUvs();
        }

        /// <summary>
        /// Adds a new Frame to the Composite.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <returns>The grid location on the render target that the frame corresponds to.</returns>
        public Point PushFrame(Frame frame)
        {
            bool resize = false;
            if (CurrentFrames.ContainsKey(frame)) return CurrentFrames[frame];
            foreach (SpriteSheet layer in frame.Layers)
            {
                if (layer.FrameWidth <= FrameSize.X && layer.FrameHeight <= FrameSize.Y) continue;
                FrameSize = new Point(Math.Max(layer.FrameWidth, FrameSize.X),
                    Math.Max(layer.FrameHeight, FrameSize.Y));
                resize = true;
            }
            Point toReturn = CurrentOffset;
            CurrentOffset.X += 1;
            if (CurrentOffset.X >= TargetSizeFrames.X)
            {
                CurrentOffset.X = 0;
                CurrentOffset.Y += 1;
            }
            if (CurrentOffset.Y >= TargetSizeFrames.Y)
            {
                resize = true;
                TargetSizeFrames = new Point(TargetSizeFrames.X*2, TargetSizeFrames.Y*2);
            }
            CurrentFrames[frame] = toReturn;

            if (resize)
            {
                Initialize();
            }

            return toReturn;
        }

        /// <summary>
        /// Draws the render target to the screen.
        /// </summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="x">The x position on the screen.</param>
        /// <param name="y">The y position on the screen.</param>
        public void DebugDraw(SpriteBatch batch, int x, int y)
        {
            batch.Begin();
            batch.Draw(Target, new Vector2(x, y), Color.White);
            batch.End();
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        public void Update()
        {
            if (HasRendered)
            {
                CurrentFrames.Clear();
                CurrentOffset = new Point(0, 0);
                HasRendered = false;
            }
        }

        /// <summary>
        /// Renders the Composite to the render target.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="batch">The batch.</param>
        public void RenderToTarget(GraphicsDevice device, SpriteBatch batch)
        {
            if (!HasRendered && CurrentFrames.Count > 0)
            {
                device.SetRenderTarget(Target);
                device.Clear(ClearOptions.Target, Color.Transparent, 1.0f, 0);
                batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone);
                foreach (var framePair in CurrentFrames)
                {
                    Frame frame = framePair.Key;
                    Point currentOffset = framePair.Value;
                    List<NamedImageFrame> images = frame.GetFrames();

                    for (int i = 0; i < images.Count; i++)
                    {
                        int y = FrameSize.Y - images[i].SourceRect.Height;
                        int x = (FrameSize.X/2) - images[i].SourceRect.Width/2;
                        batch.Draw(images[i].Image,
                            new Rectangle(currentOffset.X*FrameSize.X + x, currentOffset.Y*FrameSize.Y + y,
                                images[i].SourceRect.Width, images[i].SourceRect.Height), images[i].SourceRect,
                            frame.Tints[i]);
                    }
                }
                batch.End();
                device.SetRenderTarget(null);
                HasRendered = true;
            }
        }

        /// <summary>
        /// A Frame is a layered sprite consisting of multiple
        /// sub-rectangles in multiple sprite sheets with arbitrary colored tints.
        /// </summary>
        public class Frame
        {
            public Frame()
            {
                Layers = new List<SpriteSheet>();
                Tints = new List<Color>();
            }

            /// <summary>
            /// This is a list of sprite sheets to draw from.
            /// </summary>
            /// <value>
            /// The layers.
            /// </value>
            public List<SpriteSheet> Layers { get; set; }
            /// <summary>
            /// This is the tint of each sprite sheet.
            /// </summary>
            /// <value>
            /// The tints.
            /// </value>
            public List<Color> Tints { get; set; }
            /// <summary>
            /// This is the grid position of the frame. It is the same for each sprite sheet.
            /// </summary>
            /// <value>
            /// The position.
            /// </value>
            public Point Position { get; set; }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 0;
                    int tintHash = Tints.Aggregate(19, (current, tint) => current*31 + tint.GetHashCode());
                    int layerHash = Layers.Aggregate(19, (current, layer) => current*31 + layer.GetHashCode());
                    hashCode = (hashCode*397) ^ (layerHash);
                    hashCode = (hashCode*397) ^ Position.GetHashCode();
                    hashCode = (hashCode * 397) ^ (tintHash);
                    return hashCode;
                }
            }

            /// <summary>
            /// Convert the Frame to a list of subrectangles in each sprite sheet.
            /// </summary>
            /// <returns>A list of subrectangles in each sprite sheet.</returns>
            public List<NamedImageFrame> GetFrames()
            {
                return Layers.Select(sheet => sheet.GenerateFrame(Position)).ToList();
            }

            /// <summary>
            /// Implements the operator ==.
            /// </summary>
            /// <param name="a">a.</param>
            /// <param name="b">The b.</param>
            /// <returns>
            /// The result of the operator.
            /// </returns>
            public static bool operator ==(Frame a, Frame b)
            {
                // If both are null, or both are same instance, return true.
                if (ReferenceEquals(a, b))
                {
                    return true;
                }

                // If one is null, but not both, return false.
                if (((object) a == null) || ((object) b == null))
                {
                    return false;
                }

                // Return true if the fields match:
                return a.Equals(b);
            }

            /// <summary>
            /// Implements the operator !=.
            /// </summary>
            /// <param name="a">a.</param>
            /// <param name="b">The b.</param>
            /// <returns>
            /// The result of the operator.
            /// </returns>
            public static bool operator !=(Frame a, Frame b)
            {
                return !(a == b);
            }

            /// <summary>
            /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
            /// </summary>
            /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
            /// <returns>
            ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
            /// </returns>
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Frame) obj);
            }

            /// <summary>
            /// Determines whether two Frames are equal.
            /// </summary>
            /// <param name="otherFrame">The other frame.</param>
            /// <returns>True if the frames hold the same data, false otherwise.</returns>
            protected bool Equals(Frame otherFrame)
            {
                if (Layers.Count != otherFrame.Layers.Count || Tints.Count != otherFrame.Tints.Count) return false;
                if (!Position.Equals(otherFrame.Position)) return false;

                if (Layers.Where((t, i) => !t.Equals(otherFrame.Layers[i])).Any())
                {
                    return false;
                }

                if (Tints.Where((t, i) => !t.Equals(otherFrame.Tints[i])).Any())
                {
                    return false;
                }

                return true;
            }
        }
    }

    /// <summary>
    /// The Composite Library is a static collection of Composites for each type of thing to be drawn.
    /// This allows all Dwarves to share the same texture, for instance (even though they might use different layers).
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CompositeLibrary
    {
        public static bool IsInitialized = false;

        public static string Dwarf = "Dwarf";
        public static string Goblin = "Goblin";
        public static string Skeleton = "Skeleton";
        public static string Elf = "Elf";
        public static string Demon = "Demon";
        public static Dictionary<string, Composite> Composites { get; set; }

        public static void Initialize()
        {
            if (IsInitialized) return;
            Composites = new Dictionary<string, Composite>
            {
                {
                    Dwarf,
                    new Composite
                    {
                        FrameSize = new Point(48, 40),
                        TargetSizeFrames = new Point(8, 8)
                    }
                },
                {
                    Goblin,
                    new Composite
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
                {
                    Elf,
                    new Composite
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
                {
                    Demon,
                    new Composite
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
                {
                    Skeleton,
                    new Composite
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
            };

            foreach (var composite in Composites)
            {
                composite.Value.Initialize();
            }

            IsInitialized = true;
        }

        public static void Update()
        {
            foreach (var composite in Composites)
            {
                composite.Value.Update();
            }
        }

        public static void Render(GraphicsDevice device, SpriteBatch batch)
        {
            foreach (var composite in Composites)
            {
                composite.Value.RenderToTarget(device, batch);
            }
        }
    }
}