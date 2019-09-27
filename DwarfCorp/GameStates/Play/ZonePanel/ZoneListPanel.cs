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
    public class ZoneListPanel : Widget
    {
        public WorldManager World;

        private WidgetListView ListView;
        private EditableTextField FilterBox; 

        public override void Construct()
        {
            Border = "border-fancy";
            Font = "font10";
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
                    ChangeColorOnSelected=false,
                    Border = null,
                    ItemHeight = 24
                }) as WidgetListView;

                ListView.Border = null; // Can't make WidgetListView stop defaulting its border without breaking everywhere else its used.
            };

            OnUpdate = (sender, time) =>
            {
                if (sender.Hidden) return;

                var roomsToDisplay = World.EnumerateZones().Where(r => !String.IsNullOrEmpty(FilterBox.Text) ? r.ID.Contains(FilterBox.Text) : true);

                int i = 0;
                ListView.ClearItems();
                foreach (var room in roomsToDisplay)
                {
                    i++;
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
                            TextVerticalAlign = VerticalAlign.Center,
                            Background = new TileReference("basic", 0),
                            BackgroundColor = i % 2 == 0 ? new Vector4(0.0f, 0.0f, 0.0f, 0.1f) : new Vector4(0, 0, 0, 0.25f)
                        });

                        tag.OnUpdate = (sender1, args) =>
                        {
                            if (tag.IsAnyParentHidden())
                            {
                                return;
                            }

                            if (sender1.ComputeBoundingChildRect().Contains(Root.MousePosition))
                            {
                                Drawer3D.DrawBox(lambdaCopy.GetBoundingBox(), Color.White, 0.1f, true);
                            }
                        };

                        Root.RegisterForUpdate(tag);

                        tag.AddChild(new Button
                        {
                            Text = "Destroy",
                            AutoLayout = AutoLayout.DockRight,
                            MinimumSize = new Point(16, 0),
                            ChangeColorOnHover = true,
                            TextVerticalAlign = VerticalAlign.Center,
                            OnClick = (_sender, args) =>
                            {
                                World.UserInterface.Gui.ShowModalPopup(new Gui.Widgets.Confirm
                                {
                                    Text = "Do you want to destroy this " + lambdaCopy.Type.Name + "?",
                                    OnClose = (_sender2) => DestroyZoneTool.DestroyRoom((_sender2 as Gui.Widgets.Confirm).DialogResult, lambdaCopy, World)
                                });
                            }
                        });

                        tag.AddChild(new Widget { MinimumSize = new Point(4, 0), AutoLayout = AutoLayout.DockRight });

                        tag.AddChild(new Button
                        {
                            Text = "Go to",
                            AutoLayout = AutoLayout.DockRight,
                            ChangeColorOnHover = true,
                            MinimumSize = new Point(16, 0),
                            TextVerticalAlign = VerticalAlign.Center,
                            OnClick = (_sender, args) =>
                            {
                                World.Renderer.Camera.SetZoomTarget(lambdaCopy.GetBoundingBox().Center());
                            }
                        });

                        if (lambdaCopy is Stockpile stock && stock.SupportsFilters)
                        {
                            tag.AddChild(new Button
                            {
                                Text = "Resources...",
                                AutoLayout = AutoLayout.DockRight,
                                ChangeColorOnHover = true,
                                MinimumSize = new Point(16, 0),
                                TextVerticalAlign = VerticalAlign.Center,
                                OnClick = (_sender, args) =>
                                {
                                    var savePaused = World.Paused;
                                    World.Paused = true;

                                    Root.ShowModalPopup(new StockpilePropertiesDialog
                                    {
                                        Stockpile = lambdaCopy as Stockpile,
                                        OnClose = (_sen2) => World.Paused = savePaused
                                    });
                                }
                            });
                        }

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
