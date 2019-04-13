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
    public class CollapsableFrame : Gui.Widget
    {
        public Widget ExpandedContents;
        public Widget CollapsedContents;
        public Point ExpandedSize;

        public int CollapsedHeight = 16;
        public bool Expanded = true;

        public Action<Widget> OnExpansionChanged;
        
        public override void Construct()
        {
            Transparent = true;

            AddChild(CollapsedContents);
            CollapsedContents.AutoLayout = AutoLayout.FloatBottom;
            CollapsedContents.Hidden = Expanded;
            CollapsedContents.MinimumSize = new Point(MinimumSize.X, CollapsedHeight);

            AddChild(ExpandedContents);
            ExpandedContents.Hidden = !Expanded;
            ExpandedContents.AutoLayout = AutoLayout.DockFill;

            CollapsedContents.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 3),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                OnClick = (sender, args) =>
                {
                    ExpandedContents.Hidden = false;
                    CollapsedContents.Hidden = true;
                    Expanded = true;
                    Root.SafeCall(OnExpansionChanged, this);
                    Invalidate();
                },
                OnLayout = (sender) =>
                {
                    sender.Rect = new Rectangle(CollapsedContents.Rect.Right - 16, CollapsedContents.Rect.Y, 16, 16);
                }
            });

            ExpandedContents.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 7),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                OnClick = (sender, args) =>
                {
                    ExpandedContents.Hidden = true;
                    CollapsedContents.Hidden = false;
                    Expanded = false;
                    Root.SafeCall(OnExpansionChanged, this);
                    Invalidate();
                },
                OnLayout = (sender) =>
                {
                    sender.Rect = new Rectangle(ExpandedContents.Rect.Right - 16, ExpandedContents.Rect.Y, 16, 16);
                }
            });

            base.Construct();
        }

        public void Reposition(Rectangle NewRect)
        {
            Rect = NewRect;
            ExpandedContents.Rect = NewRect;
            ExpandedContents.Layout();

            CollapsedContents.Rect = new Rectangle(NewRect.X, NewRect.Bottom - CollapsedHeight,
                NewRect.Width, CollapsedHeight);
            CollapsedContents.Layout();

            Root.SafeCall(this.OnLayout, this);
        }
    }
}
