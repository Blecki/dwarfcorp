using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace DwarfCorp.Play
{
    public class StockpilePropertiesDialog : Widget
    {
        public Stockpile Stockpile;

        public override void Construct()
        {
            Rect = Root.RenderData.VirtualScreen;
            Rect.Inflate(-20, -20);
            Border = "border-fancy";
            Font = "font10";
            Transparent = true;

            var button = AddChild(new Button()
            {
                Text = "Back",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnLayout = (sender1) => sender1.Rect = new Rectangle(sender1.Rect.X - 16, sender1.Rect.Y - 16, 64, 32),
                OnClick = (sender1, args1) => { this.Close(); },
                MinimumSize = new Point(64, 32)
            });

            var interiorWidget = AddChild(new TabPanel()
            {
                AutoLayout = AutoLayout.DockFill
            }) as TabPanel;

            interiorWidget.AddTab("Allowed Resources", new ResourceFilterPanel { Stockpile = Stockpile });
            interiorWidget.AddTab("Contents", new StockpileContentsPanel { Stockpile = Stockpile });
                        
            this.Layout();

            button.BringToFront();
            base.Construct();
        }
    }
}
