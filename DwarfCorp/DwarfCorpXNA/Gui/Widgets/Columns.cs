using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class Columns : Widget
    {
        public int ColumnCount = 2;
        public bool ReverseColumnOrder = false;
        
        public override void Layout()
        {
            var inside = GetDrawableInterior();

            var columnWidth = inside.Width / ColumnCount;

            if (!ReverseColumnOrder)
            {
                for (var i = 0; i < ColumnCount; ++i)
                {
                    if (Children.Count > i)
                    {
                        Children[i].Rect = new Rectangle(inside.X + (columnWidth * i), inside.Y,
                            columnWidth, inside.Height);
                        Children[i].Layout();
                    }
                }
            }
            else
            {
                for (var i = 0; i < ColumnCount; ++i)
                {
                    if (Children.Count > i)
                    {
                        Children[i].Rect = new Rectangle(inside.X + (columnWidth * ((ColumnCount - i) - 1)),
                            inside.Y, columnWidth, inside.Height);
                        Children[i].Layout();
                    }
                }
            }
        }
    }
}
