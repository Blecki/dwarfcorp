using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class RoomListPanel : Widget
    {
        public WorldManager World;

        private WidgetListView ListView;
        private EditableTextField FilterBox; 

        public override void Construct()
        {
            Border = "border-fancy";

            OnConstruct = (sender) =>
            {
                sender.Root.RegisterForUpdate(sender);

                FilterBox = AddChild(new EditableTextField
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(0, 24),
                    Text = ""
                }) as EditableTextField;

                ListView = AddChild(new WidgetListView
                {
                    AutoLayout = AutoLayout.DockFill,
                    SelectedItemForegroundColor = new Vector4(0,0,0,1),
                    Border = null,
                    ItemHeight = 16
                }) as WidgetListView;

                ListView.Border = null; // Can't make WidgetListView stop defaulting its border without breaking everywhere else its used.
            };

            OnUpdate = (sender, time) =>
            {
                if (sender.Hidden) return;

                var roomsToDisplay = World.PlayerFaction.GetRooms().Where(r => !String.IsNullOrEmpty(FilterBox.Text) ? r.ID.Contains(FilterBox.Text) : true);

                ListView.ClearItems();
                foreach (var room in roomsToDisplay)
                {
                    var tag = room.GuiTag as Widget;
                    var lambdaCopy = room;

                    if (tag != null)
                        ListView.AddItem(tag);
                    else
                    {
                        #region Create gui row

                        tag = Root.ConstructWidget(new Widget
                        {
                            Text = room.GetDescriptionString(),
                            MinimumSize = new Point(0, 16),
                            Padding = new Margin(0, 0, 4, 4),
                            TextVerticalAlign = VerticalAlign.Center
                        });

                        tag.AddChild(new Button
                        {
                            Text = "DESTROY",
                            AutoLayout = AutoLayout.DockRight,
                            MinimumSize = new Point(16, 0),
                            TextVerticalAlign = VerticalAlign.Center,
                            OnClick = (_sender, args) =>
                            {
                                World.Gui.ShowModalPopup(new Gui.Widgets.Confirm
                                {
                                    Text = "Do you want to destroy this " + lambdaCopy.RoomData.Name + "?",
                                    OnClose = (_sender2) => DestroyZoneTool.DestroyRoom((_sender2 as Gui.Widgets.Confirm).DialogResult, lambdaCopy, World.PlayerFaction, World)
                                });
                            }
                        });

                        #endregion

                        room.GuiTag = tag;
                        ListView.AddItem(tag);
                    }

                    tag.Text = room.GetDescriptionString();
                }

                ListView.Invalidate();
            };

            base.Construct();
        }

       
    }
}
