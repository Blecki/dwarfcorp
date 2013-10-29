using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class GridLayout : Layout
    {


        public Dictionary<Rectangle, SillyGUIComponent> ComponentPositions { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int EdgePadding { get; set; }
        public bool FitToParent { get; set; }

        public GridLayout(SillyGUI gui, SillyGUIComponent parent, int rows, int cols) :
            base(gui, parent)
        {
            ComponentPositions = new Dictionary<Rectangle, SillyGUIComponent>();
            Rows = rows;
            Cols = cols;
            EdgePadding = 5;
            FitToParent = true;
        }

        public void SetComponentPosition(SillyGUIComponent component, int x, int y, int w, int h)
        {
            ComponentPositions[new Rectangle(x, y, w, h)] = component;
        }

        public override void UpdateSizes() 
        {
            if (FitToParent)
            {
                LocalBounds = new Rectangle(EdgePadding, EdgePadding, Parent.LocalBounds.Width - EdgePadding, Parent.LocalBounds.Height - EdgePadding);
            }
            int w = LocalBounds.Width;
            int h = LocalBounds.Height;

            if (Cols > 0 && Rows > 0)
            {
                int cellX = w / Cols;
                int cellY = h / Rows;

                foreach (KeyValuePair<Rectangle, SillyGUIComponent> comp in ComponentPositions)
                {
                    comp.Value.LocalBounds = new Rectangle(comp.Key.X * cellX, comp.Key.Y * cellY, comp.Key.Width * cellX - 10, comp.Key.Height * cellY - 10);
                }
            }
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
        }

    }
}
