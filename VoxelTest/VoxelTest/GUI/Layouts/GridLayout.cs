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
        public bool FitToParent { get; set; }
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
            FitToParent = true;
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
        }

        public void SetComponentOffset(GUIComponent component, Point offset)
        {
            ComponentOffsets[component] = offset;
        }

        public bool HasOffset(GUIComponent component)
        {
            return ComponentOffsets.ContainsKey(component);
        }

        public Point GetOffset(GUIComponent component)
        {
            return ComponentOffsets[component];
        }

        public override void UpdateSizes()
        {
            if(FitToParent)
            {
                LocalBounds = new Rectangle(EdgePadding, EdgePadding, Parent.LocalBounds.Width - EdgePadding, Parent.LocalBounds.Height - EdgePadding);
            }
            int w = LocalBounds.Width;
            int h = LocalBounds.Height;

            if(Cols > 0 && Rows > 0)
            {
                int cellX = w / Cols;
                int cellY = h / Rows;

                foreach(KeyValuePair<Rectangle, GUIComponent> comp in ComponentPositions)
                {
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
                        comp.Value.LocalBounds = new Rectangle(comp.Key.X * cellX, comp.Key.Y * cellY, comp.Key.Width * cellX - 10, comp.Key.Height * cellY - 10);   
                    }
                }
            }
        }

        public Rectangle GetRect(Rectangle coords)
        {
            int w = LocalBounds.Width;
            int h = LocalBounds.Height;
            int cellX = w / Cols;
            int cellY = h / Rows;
           return new Rectangle(
                            coords.X * cellX,
                            coords.Y * cellY, 
                            coords.Width * cellX, 
                            coords.Height * cellY);
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
        }

        public override void Render(GameTime time, SpriteBatch batch)
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

}