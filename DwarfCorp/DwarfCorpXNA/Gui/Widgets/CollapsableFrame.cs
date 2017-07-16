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

            ExpandButton = AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 3),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.FloatTopRight,
                OnClick = (sender, args) =>
                {
                    Rect = ExpandedPosition;
                    ExpandButton.Hidden = false;
                    CollapseButton.Hidden = true;
                    ExpandedContents.Hidden = false;
                    CollapsedContents.Hidden = true;
                    Invalidate();
                }
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
                    ExpandButton.Hidden = true;
                    CollapseButton.Hidden = false;
                    ExpandedContents.Hidden = true;
                    CollapsedContents.Hidden = false;
                    Invalidate();
                },
                Hidden = true
            });

            AddChild(ExpandedContents);
            ExpandedContents.AutoLayout = AutoLayout.DockFill;

            AddChild(CollapsedContents);
            CollapsedContents.AutoLayout = AutoLayout.DockFill;
            CollapsedContents.Hidden = true;

            base.Construct();
        }
        
    }
}
