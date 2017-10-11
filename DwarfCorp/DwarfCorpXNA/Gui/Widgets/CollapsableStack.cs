using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Gui.Widgets
{
    public class CollapsableStack : Gui.Widget
    {
        public class CollapsableItem
        {
            public Widget ExpandedContents;
            public Widget CollapsedContents;
            public bool Expanded = true;
            public Point ExpandedSize;
            public bool StartHidden = false;
        }

        public IEnumerable<CollapsableItem> ItemSource = null;
        public Point AnchorPoint;
        public Point CollapsedSize;

        public override void Construct()
        {
            Transparent = true;
            Rect = new Rectangle(0, 0, 0, 0);

            if (ItemSource != null)
            {
                foreach (var item in ItemSource)
                {
                    AddChild(new CollapsableFrame
                    {
                        ExpandedContents = item.ExpandedContents,
                        CollapsedContents = item.CollapsedContents,
                        Expanded = item.Expanded,
                        ExpandedSize = item.ExpandedSize,
                        CollapsedHeight = CollapsedSize.Y,
                        Hidden = item.StartHidden,
                        OnExpansionChanged = (sender) =>
                        {
                            RepositionItems();
                        }
                    });                
                }
            }

            base.Construct();
        }

        public void RepositionItems()
        {
            var currentY = AnchorPoint.Y;

            foreach (var child in EnumerateChildren().OfType<CollapsableFrame>())
            {
                if (child.Hidden)
                    continue;

                if (child.Expanded)
                {
                    child.Reposition(new Rectangle(AnchorPoint.X, currentY - child.ExpandedSize.Y,
                        child.ExpandedSize.X, child.ExpandedSize.Y));
                    currentY -= child.ExpandedSize.Y;
                }
                else
                {
                    child.Reposition(new Rectangle(AnchorPoint.X, currentY - CollapsedSize.Y,
                        CollapsedSize.X, CollapsedSize.Y));
                    currentY -= CollapsedSize.Y;
                }
            }
        }
    }
}
