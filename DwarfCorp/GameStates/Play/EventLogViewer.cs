using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace DwarfCorp
{
    public class EventLogViewer : Gui.Widget
    {
        public EventLog Log { get; set; }
        public DateTime Now { get; set; }

        public override void Construct()
        {
            AddChild(new Gui.Widget()
            {
                Text = "Events",
                Font = "font16",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(256, 32)
            });

            Gui.Widgets.WidgetListView listView = AddChild(new Gui.Widgets.WidgetListView()
            {
                AutoLayout = AutoLayout.DockFill,
                SelectedItemForegroundColor = Color.Black.ToVector4(),
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0),
                ItemHeight = 32
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

            Layout();
            base.Construct();
        }
    }
}
