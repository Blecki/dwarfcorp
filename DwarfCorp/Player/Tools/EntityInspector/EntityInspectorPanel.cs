using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace DwarfCorp
{
    public class EntityInspectionPanel : Widget
    {
        private WidgetListView ListView;

        public GameComponent SelectedEntity;

        public override void Construct()
        {
            Border = "border-fancy";
            Font = "font10";
            OnConstruct = (sender) =>
            {
                sender.Root.RegisterForUpdate(sender);

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
                if (SelectedEntity == null)
                {
                    ListView.ClearItems();
                    return;
                }

                var components = SelectedEntity.EnumerateAll();

                int i = 0;
                ListView.ClearItems();
                foreach (var component in components)
                {
                    i++;
                    var tag = component.GuiTag as Widget;
                    var lambdaCopy = component;

                    if (tag != null)
                        ListView.AddItem(tag);
                    else
                    {
                        #region Create gui row

                        tag = Root.ConstructWidget(new Widget
                        {
                            Text = component.GetType().Name,
                            MinimumSize = new Point(0, 16),
                            Padding = new Margin(0, 0, 4, 4),
                            TextVerticalAlign = VerticalAlign.Center,
                            Background = new TileReference("basic", 0),
                            BackgroundColor = i % 2 == 0 ? new Vector4(0.0f, 0.0f, 0.0f, 0.1f) : new Vector4(0, 0, 0, 0.25f)
                        });

                        tag.OnUpdate = (sender1, args) =>
                        {
                            if (tag.IsAnyParentHidden())
                                return;

                            if (sender1.ComputeBoundingChildRect().Contains(Root.MousePosition))
                                Drawer3D.DrawBox(lambdaCopy.GetBoundingBox(), Color.White, 0.1f, true);
                        };

                        Root.RegisterForUpdate(tag);

                        //tag.AddChild(new Button
                        //{
                        //    Text = "Destroy",
                        //    AutoLayout = AutoLayout.DockRight,
                        //    MinimumSize = new Point(16, 0),
                        //    ChangeColorOnHover = true,
                        //    TextVerticalAlign = VerticalAlign.Center,
                        //    OnClick = (_sender, args) =>
                        //    {
                        //        World.UserInterface.Gui.ShowModalPopup(new Gui.Widgets.Confirm
                        //        {
                        //            Text = "Do you want to destroy this " + lambdaCopy.Type.Name + "?",
                        //            OnClose = (_sender2) => DestroyZoneTool.DestroyRoom((_sender2 as Gui.Widgets.Confirm).DialogResult, lambdaCopy, World)
                        //        });
                        //    }
                        //});

                        //tag.AddChild(new Widget { MinimumSize = new Point(4, 0), AutoLayout = AutoLayout.DockRight });

                        //tag.AddChild(new Button
                        //{
                        //    Text = "Go to",
                        //    AutoLayout = AutoLayout.DockRight,
                        //    ChangeColorOnHover = true,
                        //    MinimumSize = new Point(16, 0),
                        //    TextVerticalAlign = VerticalAlign.Center,
                        //    OnClick = (_sender, args) =>
                        //    {
                        //        World.Renderer.Camera.SetZoomTarget(lambdaCopy.GetBoundingBox().Center());
                        //    }
                        //});

                        #endregion

                        component.GuiTag = tag;
                        ListView.AddItem(tag);
                    }
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
