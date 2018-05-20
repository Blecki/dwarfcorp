using System;
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
    public class Composite : IDisposable
    {
        public static int MaxPageSize = 1024;
        public class Page
        {
            public RenderTarget2D Target { get; set; }
            public Point FrameSize { get; set; }
            public Point TargetSizeFrames { get; set; }
            public bool HasChanged = true;
            public bool HasRendered = false;

            public void Initialize()
            {
                if (Target != null)
                {
                    Target.Dispose();
                }

                Target = new RenderTarget2D(GameState.Game.GraphicsDevice, FrameSize.X * TargetSizeFrames.X, FrameSize.Y * TargetSizeFrames.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }

            public Rectangle GetFrameRect(Point Frame)
            {
                return new Rectangle(Frame.X * FrameSize.X, Frame.Y * FrameSize.Y, FrameSize.X, FrameSize.Y);
            }

            public bool ValidateFrameSizeChange(Point newFrameSize)
            {
                return TargetSizeFrames.X * newFrameSize.X <= MaxPageSize;
            }

            public bool Grow()
            {
                if (TargetSizeFrames.X * 2 * FrameSize.X > MaxPageSize)
                {
                    return false;
                }
                TargetSizeFrames = new Point(TargetSizeFrames.X * 2, TargetSizeFrames.Y * 2);
                HasRendered = false;
                HasChanged = true;
                return true;
            }

            public void RenderToTarget(GraphicsDevice device, IEnumerable<KeyValuePair<CompositeFrame, FrameID>> currentFrames)
            {
                if (HasChanged)
                {
                    if (Target == null || Target.IsDisposed || Target.GraphicsDevice.IsDisposed)
                    {
                        return;
                    }
                    device.SetRenderTarget(Target);
                    device.Clear(ClearOptions.Target, Color.Transparent, 1.0f, 0);
                    try
                    {
                        DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp,
                            DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
                        foreach (KeyValuePair<CompositeFrame, FrameID> framePair in currentFrames)
                        {
                            CompositeFrame frame = framePair.Key;
                            FrameID currentOffset = framePair.Value;
                            
                            List<NamedImageFrame> images = frame.GetFrames();

                            for (int i = 0; i < images.Count; i++)
                            {
                                int y = FrameSize.Y - images[i].SourceRect.Height;
                                int x = (FrameSize.X / 2) - images[i].SourceRect.Width / 2;
                                DwarfGame.SpriteBatch.Draw(images[i].Image,
                                    new Rectangle(currentOffset.Offset.X * FrameSize.X + x, 
                                                  currentOffset.Offset.Y * FrameSize.Y + y,
                                                  images[i].SourceRect.Width, 
                                                  images[i].SourceRect.Height), 
                                    images[i].SourceRect,
                                    frame.Cells[i].Tint);
                            }
                        }
                    }
                    finally
                    {
                        DwarfGame.SpriteBatch.End();
                    }
                    device.SetRenderTarget(null);
                    HasRendered = true;
                    HasChanged = false;
                }
            }

        }

        public struct FrameID
        {
            public int Page;
            public Point Offset;
            public FrameID(int page, int x, int y)
            {
                Page = page;
                Offset = new Point(x, y);
            }
        }
        public List<Page> Pages = new List<Page>();
        private Dictionary<CompositeFrame, FrameID> CurrentFrames { get; set; }
        private FrameID CurrentOffset;

        public Composite()
        {
            CurrentFrames = new Dictionary<CompositeFrame, FrameID>();
            CurrentOffset = new FrameID(0, 0, 0);
        }

        public void Init(Point frameSize, Point targetSize)
        {
            Pages.Clear();
            Pages.Add(new Page()
            {
                FrameSize = frameSize,
                TargetSizeFrames = targetSize
            });
            Pages[0].Initialize();
        }


        public FrameID PushFrame(CompositeFrame frame)
        {
            var page = Pages[CurrentOffset.Page];
            if (page.Target.IsContentLost)
            {
                page.Initialize();
                CurrentFrames.Clear();
            }
            if (!CurrentFrames.ContainsKey(frame))
            {
                Point newFrameSize = page.FrameSize;
                foreach (var layer in frame.Cells)
                {
                    if (layer.Sheet.FrameWidth > page.FrameSize.X || 
                        layer.Sheet.FrameHeight > page.FrameSize.Y)
                    {
                        newFrameSize = new Point(Math.Max(layer.Sheet.FrameWidth, page.FrameSize.X),
                            Math.Max(layer.Sheet.FrameHeight, page.FrameSize.Y));
                    }
                }

                if (newFrameSize != page.FrameSize)
                {
                    if (page.ValidateFrameSizeChange(newFrameSize))
                    {
                        page.FrameSize = newFrameSize;
                        page.Initialize();
                        page.HasChanged = true;
                    }
                    else
                    {
                        page.FrameSize = newFrameSize;
                        if (!page.Grow())
                        {
                            var newPage = new Page()
                            {
                                TargetSizeFrames = new Point(4, 4),
                                FrameSize = newFrameSize
                            };
                            newPage.Initialize();
                            Pages.Add(newPage);
                            page = newPage;
                            CurrentOffset.Page++;
                            CurrentOffset.Offset.X = 0;
                            CurrentOffset.Offset.Y = 0;
                        }
                    }
                }

                FrameID toReturn = CurrentOffset;
                CurrentOffset.Offset.X += 1;
                if (CurrentOffset.Offset.X >= page.TargetSizeFrames.X)
                {
                    CurrentOffset.Offset.X = 0;
                    CurrentOffset.Offset.Y += 1;
                }

                if (CurrentOffset.Offset.Y >= page.TargetSizeFrames.Y)
                {
                    if (!page.Grow())
                    {
                        var newPage = new Page()
                        {
                            TargetSizeFrames = new Point(4, 4),
                            FrameSize = page.FrameSize
                        };
                        newPage.Initialize();
                        Pages.Add(newPage);
                        page = newPage;
                        CurrentOffset.Page++;
                        CurrentOffset.Offset.X = 0;
                        CurrentOffset.Offset.Y = 0;
                        toReturn = CurrentOffset;
                    }
                    return PushFrame(frame);
                }

                CurrentFrames[frame] = toReturn;
                page.HasChanged = true;
                return toReturn;
            }
            else
                return CurrentFrames[frame];
        }

        public Vector2 DebugDraw(SpriteBatch batch, int x, int y)
        {
            batch.Begin();
            Vector2 offset = Vector2.Zero;
            foreach (var page in Pages)
            {
                Drawer2D.DrawRect(batch, new Rectangle(x + (int)offset.X, y + (int)offset.Y, page.Target.Width, page.Target.Height), Color.White, 1);
                batch.Draw(page.Target, offset + new Vector2(x, y), Color.White);
                offset.X += page.TargetSizeFrames.X * page.FrameSize.X;
            }
            batch.End();
            return offset;
        }

        public void Update()
        {
        }

        public void RenderToTarget(GraphicsDevice device)
        {
            for(int i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i];
                page.RenderToTarget(device, CurrentFrames.Where(kvp => kvp.Value.Page == i));
            }
        }

        public void Dispose()
        {
            foreach (var page in Pages)
            {
                if (page.Target != null && !page.Target.IsDisposed)
                {
                    page.Target.Dispose();
                    page.Target = null;
                }
            }
        }

        public Texture2D GetTarget(FrameID frame)
        {
            return Pages[frame.Page].Target;
        }

        public Rectangle GetFrameRect(FrameID frame)
        {
            return Pages[frame.Page].GetFrameRect(frame.Offset);
        }
    }
}
