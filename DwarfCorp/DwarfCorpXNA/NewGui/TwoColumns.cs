using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class TwoColumns : Widget
    {
        public bool ReverseColumnOrder = false;
        
        public override void Layout()
        {
            if (!ReverseColumnOrder)
            {
                if (Children.Count > 0)
                {
                    Children[0].Rect = new Rectangle(Rect.X, Rect.Y, Rect.Width / 2, Rect.Height);
                    Children[0].Layout();
                }

                if (Children.Count > 1)
                {
                    Children[1].Rect = new Rectangle(Rect.X + Rect.Width / 2, Rect.Y, Rect.Width / 2, Rect.Height);
                    Children[1].Layout();
                }
            }
            else
            {
                if (Children.Count > 1)
                {
                    Children[1].Rect = new Rectangle(Rect.X, Rect.Y, Rect.Width / 2, Rect.Height);
                    Children[1].Layout();
                }

                if (Children.Count > 0)
                {
                    Children[0].Rect = new Rectangle(Rect.X + Rect.Width / 2, Rect.Y, Rect.Width / 2, Rect.Height);
                    Children[0].Layout();
                }
            }
        }


    }
}
