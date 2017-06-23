using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class TwoColumns : Widget
    {
        public bool ReverseColumnOrder = false;
        
        public override void Layout()
        {
            var inside = GetDrawableInterior();

            if (!ReverseColumnOrder)
            {
                if (Children.Count > 0)
                {
                    Children[0].Rect = new Rectangle(inside.X, inside.Y, inside.Width / 2, inside.Height);
                    Children[0].Layout();
                }

                if (Children.Count > 1)
                {
                    Children[1].Rect = new Rectangle(inside.X + inside.Width / 2, inside.Y, inside.Width / 2, inside.Height);
                    Children[1].Layout();
                }
            }
            else
            {
                if (Children.Count > 1)
                {
                    Children[1].Rect = new Rectangle(inside.X, inside.Y, inside.Width / 2, inside.Height);
                    Children[1].Layout();
                }

                if (Children.Count > 0)
                {
                    Children[0].Rect = new Rectangle(inside.X + inside.Width / 2, inside.Y, inside.Width / 2, inside.Height);
                    Children[0].Layout();
                }
            }
        }


    }
}
