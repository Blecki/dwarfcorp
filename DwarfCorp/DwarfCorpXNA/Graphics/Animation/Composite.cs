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
        public static int MaxPageSize = 2048;
        public class Page
        {
            public RenderTarget2D Target { get; set; }
            public Point FrameSize { get; set; }
            public Point TargetSizeFrames { get; set; }
            public bool HasChanged = true;
            public bool HasRendered = false;
            private Point NextFreeCell = new Point(-1, 0);

            public enum AddCellResult
            {
                PageFull,
                PageGrown,
                CellChanged
            }


            public AddCellResult GetNextFreeCell(out Point freeCell)
            {
                var result = InsertCell();
                freeCell = NextFreeCell;
                return result;
            }

            private AddCellResult InsertCell()
            {
                NextFreeCell.X += 1;
                if (NextFreeCell.X >= TargetSizeFrames.X)
                {
                    NextFreeCell.X = 0;
                    NextFreeCell.Y += 1;
                }
                if (NextFreeCell.Y >= TargetSizeFrames.Y)
                {
                    if(Grow())
                    {
                        return AddCellResult.PageGrown;
                    }
                    else
                    {
                        return AddCellResult.PageFull;
                    }
                }
                return AddCellResult.CellChanged;
            }

            public void ResetCell()
            {
                NextFreeCell = new Point(-1, 0);
            }

            public void Initialize()
            {
                if (Target != null)
                {
                    Target.Dispose();
                }

                Target = new RenderTarget2D(GameState.Game.GraphicsDevice, FrameSize.X * TargetSizeFrames.X, FrameSize.Y * TargetSizeFrames.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                HasChanged = true;
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
                ResetCell();
                Initialize();
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

        public Composite()
        {
            CurrentFrames = new Dictionary<CompositeFrame, FrameID>();
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

        private void EraseFrames(int page)
        {
            List<CompositeFrame> invalidFrames = new List<CompositeFrame>();
            foreach (var dirtyFrame in CurrentFrames.Where(f => f.Value.Page == page))
            {
                invalidFrames.Add(dirtyFrame.Key);
            }
            foreach (var dirtyFrame in invalidFrames)
            {
                CurrentFrames.Remove(dirtyFrame);
            }
            Pages[page].HasChanged = true;
            Pages[page].ResetCell();
        }

        public FrameID PushFrame(CompositeFrame frame)
        {
            foreach(var page in Pages.Where(p => p.Target.IsContentLost))
            {
                page.Initialize();
            }

            if (!CurrentFrames.ContainsKey(frame))
            {
                Point nextFreeCell = new Point(-1, -1);
                Page page = null;
                int k = -1;
                foreach (var pages in Pages)
                {
                    k++;
                    var result = pages.GetNextFreeCell(out nextFreeCell);
                    if (result != Page.AddCellResult.PageFull)
                    {
                        page = pages;
                        if (result == Page.AddCellResult.PageGrown)
                        {
                            EraseFrames(k);
                        }
                        break;
                    }
                }


                Point newFrameSize = new Point(1, 1);
                foreach (var layer in frame.Cells)
                {
                    newFrameSize = new Point(Math.Max(layer.Sheet.FrameWidth, newFrameSize.X),
                        Math.Max(layer.Sheet.FrameHeight, newFrameSize.Y));
                }

                if (page == null)
                {
                    page = new Page()
                    {
                        TargetSizeFrames = new Point(4, 4),
                        FrameSize = newFrameSize
                    };
                    page.Initialize();
                    Pages.Add(page);
                    page.GetNextFreeCell(out nextFreeCell);
                    k = Pages.Count - 1;
                }

                if (page.FrameSize.X < newFrameSize.X || page.FrameSize.Y < newFrameSize.Y)
                {
                    EraseFrames(k);

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
                            newPage.GetNextFreeCell(out nextFreeCell);
                            k++;
                        }
                        else
                        {
                            page.GetNextFreeCell(out nextFreeCell);
                            EraseFrames(k);
                        }
                    }
                }

                FrameID toReturn = new FrameID(k, nextFreeCell.X, nextFreeCell.Y);
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
            int k = 0;
            foreach (var page in Pages)
            {
                Color pageColor = new HSLColor(255 * ((float)k / Pages.Count), 255, 255);
                Color cellColor = new HSLColor(255 * ((float)k / Pages.Count), 255, 128);
                Drawer2D.DrawRect(batch, new Rectangle(x + (int)offset.X, y + (int)offset.Y, page.Target.Width, page.Target.Height), pageColor, 1);
                batch.Draw(page.Target, offset + new Vector2(x, y), Color.White);
                foreach(var frame in CurrentFrames.Where(f => f.Value.Page == k))
                {
                    Drawer2D.DrawRect(batch, new Rectangle(x + (int)offset.X + frame.Value.Offset.X * page.FrameSize.X, y + (int)offset.Y + frame.Value.Offset.Y * page.FrameSize.Y, page.FrameSize.X, page.FrameSize.Y), cellColor, 1);
                }
                k++;
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
