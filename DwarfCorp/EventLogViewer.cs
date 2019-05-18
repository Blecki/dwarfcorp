using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Newtonsoft.Json;
#if !XNA_BUILD && !GEMMONO
using SDL2;
#endif
using SharpRaven;
using SharpRaven.Data;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class EventLogViewer : Gui.Widget
    {
        public EventLog Log { get; set; }
        public DateTime Now { get; set; }
        public Widget CloseButton { get; set; }
        public override void Construct()
        {
            Border = "border-fancy";
            AddChild(new Gui.Widget()
            {
                Text = "Events",
                Font = "font16",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(256, 32)
            });
            Gui.Widgets.WidgetListView listView = AddChild(new Gui.Widgets.WidgetListView()
            {
                AutoLayout = AutoLayout.DockTop,
                SelectedItemForegroundColor = Color.Black.ToVector4(),
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0),
                ItemHeight = 32,
                MinimumSize = new Point(0, 3 * Root.RenderData.VirtualScreen.Height / 4)
            }) as Gui.Widgets.WidgetListView;
            foreach (var logged in Log.GetEntries().Reverse())
            {
                listView.AddItem(Root.ConstructWidget(new Widget()
                {
                    Background = new TileReference("basic", 0),
                    Text = TextGenerator.AgeToString(Now - logged.Date) + " " + logged.Text,
                    Tooltip = logged.Details,
                    TextColor = logged.TextColor.ToVector4(),
                    Font = "font10",
                    MinimumSize = new Point(640, 32),
                    Padding = new Margin(0, 0, 4, 4),
                    TextVerticalAlign = VerticalAlign.Center
                }));
            }
            CloseButton = AddChild(new Gui.Widgets.Button()
            {
                Text = "Close",
                Font = "font10",
                Border = "border-button",
                MinimumSize = new Point(128, 32),
                AutoLayout = AutoLayout.FloatBottomRight
            });
            Layout();
            base.Construct();
        }
    }
}
