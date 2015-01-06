﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mime;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    [JsonObject(IsReference = true)]
    public class Composite : IDisposable
    {
        public class Frame
        {
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 0;
                    int tintHash = 19;
                    foreach (var tint in Tints)
                    {
                        tintHash = tintHash * 31 + tint.GetHashCode();
                    }
                    int layerHash = 19;
                    foreach (var layer in Layers)
                    {
                        layerHash = layerHash * 31 + layer.GetHashCode();
                    }
                    hashCode = (hashCode * 397) ^ (layerHash);
                    hashCode = (hashCode*397) ^ Position.GetHashCode();
                    return hashCode;
                }
            }

            public List<SpriteSheet> Layers { get; set; }
            public List<Color> Tints { get; set; } 
            public Point Position { get; set; }

            public Frame()
            {
                Layers = new List<SpriteSheet>();
                Tints = new List<Color>();
            }

            public List<NamedImageFrame> GetFrames()
            {
                return Layers.Select(sheet => sheet.GenerateFrame(Position)).ToList();
            }
            public static bool operator ==(Frame a, Frame b)
            {
                // If both are null, or both are same instance, return true.
                if (System.Object.ReferenceEquals(a, b))
                {
                    return true;
                }

                // If one is null, but not both, return false.
                if (((object)a == null) || ((object)b == null))
                {
                    return false;
                }

                // Return true if the fields match:
                return a.Equals(b);
            }

            public static bool operator !=(Frame a, Frame b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Frame) obj);
            }

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

        public RenderTarget2D Target { get; set; }
        public Point FrameSize { get; set; }
        public Point TargetSizeFrames { get; set; }

        public bool HasRendered = false;
        private Dictionary<Frame, Point> CurrentFrames { get; set; }
        private Point CurrentOffset;

        public Composite()
        {
            CurrentFrames = new Dictionary<Frame, Point>();
            CurrentOffset = new Point(0, 0);
        }

        public void Initialize()
        {
            Target = new RenderTarget2D(GameState.Game.GraphicsDevice, FrameSize.X * TargetSizeFrames.X, FrameSize.Y * TargetSizeFrames.Y, false, SurfaceFormat.Color, DepthFormat.None);
        }

        public BillboardPrimitive CreatePrimitive(GraphicsDevice device, Point frame)
        {
            string key = Target.GetHashCode() + ": " + FrameSize.X + "," + FrameSize.Y + " " + frame.X + " " + frame.Y;
            if (!PrimitiveLibrary.BillboardPrimitives.ContainsKey(key))
            {
                PrimitiveLibrary.BillboardPrimitives[key] = new BillboardPrimitive(device, (Texture2D)Target, FrameSize.X, FrameSize.Y, new Point(0, 0), FrameSize.X / 32.0f, FrameSize.Y / 32.0f);
                
            }

            return PrimitiveLibrary.BillboardPrimitives[key];
        }

        public void ApplyBillboard(BillboardPrimitive primitive, Point offset)
        {
            primitive.UVs = new BillboardPrimitive.BoardTextureCoords(Target.Width, Target.Height, FrameSize.X, FrameSize.Y, offset, false);
            primitive.UpdateVertexUvs();
        }

        public Point PushFrame(Frame frame)
        {
            if (!CurrentFrames.ContainsKey(frame))
            {
                Point toReturn = CurrentOffset;
                CurrentOffset.X += 1;
                if (CurrentOffset.X >=TargetSizeFrames.X)
                {
                    CurrentOffset.X = 0;
                    CurrentOffset.Y += 1;
                }
                CurrentFrames[frame] = toReturn;
               
                return toReturn;
            }
            else return CurrentFrames[frame];
        }

        public void DebugDraw(SpriteBatch batch, int x, int y)
        {
            batch.Begin();
            batch.Draw(Target, new Vector2(x, y), Color.White);
            batch.End();
        }

        public void Update()
        {
            if (HasRendered)
            {
                CurrentFrames.Clear();
                CurrentOffset = new Point(0, 0);
                HasRendered = false;
            }
        }

        public void RenderToTarget(GraphicsDevice device, SpriteBatch batch)
        {
            if (!HasRendered && CurrentFrames.Count > 0)
            {
                device.SetRenderTarget(Target);
                device.Clear(ClearOptions.Target, Color.Transparent, 1.0f, 0);
                batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone);
                foreach (KeyValuePair<Frame, Point> framePair in CurrentFrames)
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

        public void Dispose()
        {
            if(Target != null && !Target.IsDisposed)
                Target.Dispose();
        }
    }

    [JsonObject(IsReference = true)]
    public class CompositeLibrary
    {
        public static Dictionary<string, Composite> Composites { get; set; }
        public static bool IsInitialized = false;

        public static string Dwarf = "Dwarf";
        public static string Goblin = "Goblin";
        public static string Skeleton = "Skeleton";
        public static string Elf = "Elf";

        public static void Initialize()
        {
            if (IsInitialized) return;
            Composites = new Dictionary<string, Composite>()
            {
                {
                    Dwarf,
                    new Composite()
                    {
                        FrameSize = new Point(48, 40),
                        TargetSizeFrames = new Point(8, 8)
                    }
                },
                {
                    Goblin,
                    new Composite()
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
                {
                    Elf,
                    new Composite()
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
                {
                    Skeleton,
                    new Composite()
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
