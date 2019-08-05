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

            var interiorWidget = AddChild(new TabPanel()
            {
                AutoLayout = AutoLayout.DockFill
            }) as TabPanel;

            interiorWidget.AddTab("Allowed Resources", new ResourceFilterPanel { Stockpile = Stockpile });
            interiorWidget.AddTab("Contents", new StockpileContentsPanel { Stockpile = Stockpile });
                        
            AddChild(new Button()
            {
                Text = "OK",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender1, args1) => { this.Close(); }
            });

            this.Layout();
            base.Construct();
        }

        private static string SplitCamelCase(string str)
        {
            return Regex.Replace(
                Regex.Replace(
                    str,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }
    }
}
