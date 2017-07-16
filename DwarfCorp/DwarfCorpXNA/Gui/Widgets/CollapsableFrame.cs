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
        public Widget ExpandButton;
        public Widget CollapseButton;

        public Rectangle ExpandedPosition;
        public Rectangle CollapsedPosition;
        
        public override void Construct()
        {
            Border = "border-button";
            Rect = ExpandedPosition;
            AutoLayout = AutoLayout.None;

            AddChild(ExpandedContents);
            ExpandedContents.AutoLayout = AutoLayout.DockFill;

            AddChild(CollapsedContents);
            CollapsedContents.AutoLayout = AutoLayout.DockFill;
            CollapsedContents.Hidden = true;

            ExpandButton = AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 3),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.FloatTopRight,
                OnClick = (sender, args) =>
                {
                    Rect = ExpandedPosition;
                    ExpandButton.Hidden = true;
                    CollapseButton.Hidden = false;
                    ExpandedContents.Hidden = false;
                    CollapsedContents.Hidden = true;
                    Layout();
                    Invalidate();
                },
                Hidden = true
            });

            CollapseButton = AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 7),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.FloatTopRight,
                OnClick = (sender, args) =>
                {
                    Rect = CollapsedPosition;
                    ExpandButton.Hidden = false;
                    CollapseButton.Hidden = true;
                    ExpandedContents.Hidden = true;
                    CollapsedContents.Hidden = false;
                    Layout();
                    Invalidate();
                }
            });

            base.Construct();
        }

        public override void Layout()
        {
            Root.SafeCall(OnLayout, this);

            Rect = CollapseButton.Hidden ? CollapsedPosition : ExpandedPosition;

            ExpandedContents.Rect = Rect;
            CollapsedContents.Rect = Rect;

            ExpandButton.Rect = new Rectangle(Rect.Right - 32, Rect.Top + 32, 16, 16);
            CollapseButton.Rect = new Rectangle(Rect.Right - 32, Rect.Top + 32, 16, 16);

            ExpandedContents.Layout();
            CollapsedContents.Layout();
        }

    }
}
