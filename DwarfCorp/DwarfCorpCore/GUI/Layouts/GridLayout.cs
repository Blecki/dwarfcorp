// GridLayout.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Lays out GUI components in a simple grid. Each item occupies a position
    /// and extents in teh grid.
    /// </summary>
    public class GridLayout : Layout
    {
        public Dictionary<Rectangle, GUIComponent> ComponentPositions { get; set; }
        public Dictionary<GUIComponent, Point> ComponentOffsets { get; set; } 
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int EdgePadding { get; set; }
        public int RowHighlight { get; set; }
        public int ColumnHighlight { get; set; }
        public Color HighlightColor { get; set; }

        public GridLayout(DwarfGUI gui, GUIComponent parent, int rows, int cols) :
            base(gui, parent)
        {
            ComponentPositions = new Dictionary<Rectangle, GUIComponent>();
            ComponentOffsets = new Dictionary<GUIComponent, Point>();
            Rows = rows;
            Cols = cols;
            EdgePadding = 5;
            HeightSizeMode = SizeMode.Fit;
            WidthSizeMode = SizeMode.Fit;
            RowHighlight = -1;
            ColumnHighlight = -1;
        }

        public void HighlightRow(int row, Color color)
        {
            RowHighlight = row;
            HighlightColor = color;
        }

        public void HighlightColumn(int column, Color color)
        {
            ColumnHighlight = column;
            HighlightColor = color;
        }

        public void SetComponentPosition(GUIComponent component, int x, int y, int w, int h)
        {
            ComponentPositions[new Rectangle(x, y, w, h)] = component;
            UpdateSizes();
            UpdateSizeRecursive();
        }

        public void SetComponentOffset(GUIComponent component, Point offset)
        {
            ComponentOffsets[component] = offset;
            UpdateSizes();
            UpdateSizeRecursive();
        }

        public bool HasOffset(GUIComponent component)
        {
            if (component == null)
            {
                return false;
            }
            return ComponentOffsets.ContainsKey(component);
        }

        public Point GetOffset(GUIComponent component)
        {
            if (component == null)
            {
                return new Point(0, 0);
            }
            return ComponentOffsets[component];
        }

        public override void UpdateSizes()
        {
            UpdateSize();
            int w = LocalBounds.Width - EdgePadding;
            int h = LocalBounds.Height - EdgePadding;

            if(Cols > 0 && Rows > 0)
            {
                int cellX = w / Cols;
                int cellY = h / Rows;

                foreach(KeyValuePair<Rectangle, GUIComponent> comp in ComponentPositions)
                {
                    if (comp.Value == null)
                    {
                        continue;
                    }

                    if (HasOffset(comp.Value))
                    {
                        Point offset = GetOffset(comp.Value);
                        comp.Value.LocalBounds = new Rectangle(
                            comp.Key.X * cellX + offset.X,
                            comp.Key.Y * cellY + offset.Y, 
                            comp.Key.Width * cellX, 
                            comp.Key.Height * cellY);
                    }
                    else
                    {
                        int lw = comp.Key.Width * cellX - 10;
                        int lh = comp.Key.Height * cellY - 10;
                        int lx = comp.Key.X * cellX + EdgePadding;
                        int ly = comp.Key.Y * cellY + EdgePadding;
                        if (lx + lw > w)
                        {
                            lw = (w - lx);
                        }

                        if (ly + lh > h)
                        {
                            lh = (h - ly);
                        }
                        comp.Value.LocalBounds = new Rectangle(lx, ly, lw, lh);   
                    }
                }
            }
        }

        public Rectangle GetRect(Rectangle coords)
        {
            int w = LocalBounds.Width - EdgePadding;
            int h = LocalBounds.Height - EdgePadding;
            int cellX = w / Cols;
            int cellY = h / Rows;
           return new Rectangle(
                            coords.X * cellX + EdgePadding,
                            coords.Y * cellY + EdgePadding, 
                            coords.Width * cellX, 
                            coords.Height * cellY);
        }

        public override void Update(DwarfTime time)
        {
            base.Update(time);
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if(RowHighlight >= 0)
            {
                Rectangle rect = new Rectangle(GlobalBounds.X, GlobalBounds.Y + RowHighlight * (GlobalBounds.Height / Rows), GlobalBounds.Width, (GlobalBounds.Height / Rows));
                Drawer2D.FillRect(batch, rect, HighlightColor);
            }

            if(ColumnHighlight >= 0)
            {
                Rectangle rect = new Rectangle(GlobalBounds.X + ColumnHighlight * (GlobalBounds.Width / Cols), GlobalBounds.Y, (GlobalBounds.Width / Cols), GlobalBounds.Height);
                Drawer2D.FillRect(batch, rect, HighlightColor);
            }
            base.Render(time, batch);
        }
    }

    public class AlignLayout : Layout
    {
        public enum Alignment
        {
            None,
            Center,
            Top,
            Left,
            Right,
            Bottom
        }

        protected struct Aligner
        {
            public GUIComponent Child { get; set; }
            public Alignment XAlignment { get; set; }
            public Alignment YAlignment { get; set; }
            public Vector2 OriginalPos { get; set; }
        }

        protected List<Aligner> Aligners { get; set; } 

        public enum PositionMode
        {
            Pixels,
            Percent
        }

        public PositionMode Mode { get; set; }

        public AlignLayout(DwarfGUI gui, GUIComponent parent) : base(gui, parent)
        {
            Aligners = new List<Aligner>();
        }

        public void Add(GUIComponent child, Alignment xalignment, Alignment yAlignment, Vector2 originalPos)
        {
            Aligners.Add(new Aligner()
            {
                Child = child,
                XAlignment = xalignment,
                YAlignment = yAlignment,
                OriginalPos = originalPos
            });
            Children.Add(child);
        }

        public void Remove(GUIComponent child)
        {
            Children.Remove(child);

            Aligners.RemoveAll((aligner) => aligner.Child == child);
        }

        public void Clear()
        {
            Children.Clear();
            Aligners.Clear();
        }

        public Vector2 PercentToPixels(Vector2 percent)
        {
            return new Vector2(percent.X * LocalBounds.Width, percent.Y * LocalBounds.Y);
        }


        protected void Align(Aligner aligner)
        {
            Vector2 pos = aligner.OriginalPos;

            if (Mode == PositionMode.Percent)
            {
                pos = PercentToPixels(pos);
            }
            Rectangle bounds = aligner.Child.LocalBounds;
            switch (aligner.XAlignment)
            {
                case Alignment.Center:
                    pos.X = 0.5f*LocalBounds.Width - bounds.Width*0.5f;
                    break;
                case Alignment.Left:
                    pos.X = 0;
                    break;
                case Alignment.Right:
                    pos.X = LocalBounds.Width - bounds.Width;
                    break;
                default:
                    break;
            }

            switch (aligner.YAlignment)
            {
                case Alignment.Center:
                    pos.Y = 0.5f*LocalBounds.Height - bounds.Height*0.5f;
                    break;
                case Alignment.Bottom:
                    pos.Y = LocalBounds.Height - bounds.Height;
                    break;
                case Alignment.Top:
                    pos.Y = 0;
                    break;
                default:
                    break;
            }

            aligner.Child.LocalBounds = new Rectangle((int)pos.X, (int)pos.Y, bounds.Width, bounds.Height);
        }

        public override void UpdateSizes()
        {
            UpdateSize();

            foreach (Aligner aligner in Aligners)
            {
                Align(aligner);
            }
        }
    }

}