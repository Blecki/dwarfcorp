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
        public RenderTarget2D Target { get; set; }
        public Point FrameSize { get; set; }
        public Point TargetSizeFrames { get; set; }
        public bool HasChanged = true;
        public bool HasRendered = false;
        private Dictionary<CompositeFrame, Point> CurrentFrames { get; set; }
        private Point CurrentOffset;

        public Composite()
        {
            CurrentFrames = new Dictionary<CompositeFrame, Point>();
            CurrentOffset = new Point(0, 0);
        }

        public void Initialize()
        {
            if (Target != null)
            {
                Target.Dispose();
            }

            Target = new RenderTarget2D(GameState.Game.GraphicsDevice, FrameSize.X * TargetSizeFrames.X, FrameSize.Y * TargetSizeFrames.Y, false, SurfaceFormat.Color, DepthFormat.None);
        }

        public Rectangle GetFrameRect(Point Frame)
        {
            return new Rectangle(Frame.X * FrameSize.X, Frame.Y * FrameSize.Y, FrameSize.X, FrameSize.Y);
        }

        public Point PushFrame(CompositeFrame frame)
        {
            if (!CurrentFrames.ContainsKey(frame))
            {
                foreach (var layer in frame.Cells)
                {
                    if (layer.Sheet.FrameWidth > FrameSize.X || layer.Sheet.FrameHeight > FrameSize.Y)
                    {
                        FrameSize = new Point(Math.Max(layer.Sheet.FrameWidth, FrameSize.X), Math.Max(layer.Sheet.FrameHeight, FrameSize.Y));
                        HasChanged = true;
                    }
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
                    TargetSizeFrames = new Point(TargetSizeFrames.X * 2, TargetSizeFrames.Y * 2);
                    Initialize();
                    HasChanged = true;
                    return PushFrame(frame);
                }

                CurrentFrames[frame] = toReturn;
                HasChanged = true;
                return toReturn;
            }
            else
                return CurrentFrames[frame];
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
                /*
                CurrentFrames.Clear();
                CurrentOffset = new Point(0, 0);
                HasRendered = false;
                */
            }
        }

        public void RenderToTarget(GraphicsDevice device)
        {
            if (HasChanged && CurrentFrames.Count > 0)
            {
                device.SetRenderTarget(Target);
                device.Clear(ClearOptions.Target, Color.Transparent, 1.0f, 0);
                try
                {
                    DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp,
                        DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
                    foreach (KeyValuePair<CompositeFrame, Point> framePair in CurrentFrames)
                    {
                        CompositeFrame frame = framePair.Key;
                        Point currentOffset = framePair.Value;
                        List<NamedImageFrame> images = frame.GetFrames();

                        for (int i = 0; i < images.Count; i++)
                        {
                            int y = FrameSize.Y - images[i].SourceRect.Height;
                            int x = (FrameSize.X / 2) - images[i].SourceRect.Width / 2;
                            DwarfGame.SpriteBatch.Draw(images[i].Image,
                                new Rectangle(currentOffset.X * FrameSize.X + x, currentOffset.Y * FrameSize.Y + y,
                                    images[i].SourceRect.Width, images[i].SourceRect.Height), images[i].SourceRect,
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

        public void Dispose()
        {
            if(Target != null && !Target.IsDisposed)
                Target.Dispose();
        }
    }
}
