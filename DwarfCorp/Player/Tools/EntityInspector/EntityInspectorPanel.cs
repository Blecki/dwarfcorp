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
        private Widget ComponentProperties;
        private PropertyPanel PropertyPanel;

        public GameComponent SelectedEntity;
        public GameComponent SelectedComponent;

        public override void Construct()
        {
            Border = "border-one";
            Font = "font10";
            OnConstruct = (sender) =>
            {
                sender.Root.RegisterForUpdate(sender);

                AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockBottom,
                    MinimumSize = new Point(0, 32),
                    Text = "CLOSE",
                    ChangeColorOnHover = true,
                    OnClick = (sender1, args) => sender.Close()
                });

                ComponentProperties = AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockBottom,
                    MinimumSize = new Point(0, 128),
                });

                ListView = AddChild(new WidgetListView
                {
                    AutoLayout = AutoLayout.DockFill,
                    SelectedItemForegroundColor = new Vector4(0,0,0,1),
                    ChangeColorOnSelected=false,
                    Border = null,
                    ItemHeight = 24
                }) as WidgetListView;

                ListView.Border = null; // Can't make WidgetListView stop defaulting its border without breaking everywhere else its used.

                PropertyPanel = AddChild(new PropertyPanel { Hidden = true }) as PropertyPanel;

            };

            OnLayout = (sender) =>
            {
                PropertyPanel.Rect = new Rectangle(sender.Rect.Right, sender.Rect.Top, Root.RenderData.VirtualScreen.Width - sender.Rect.Width, 512);
                PropertyPanel.Layout();
            };

            OnUpdate = (sender, time) =>
            {
                if (sender.Hidden)
                {
                    if (SelectedEntity != null || SelectedComponent != null)
                    {
                        SelectedEntity = null;
                        SelectedComponent = null;
                        ListView.ClearItems();
                    }
                    return;
                }

                if (SelectedEntity == null)
                {
                    SelectedComponent = null;
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
                            Tag = lambdaCopy
                        });

                        tag.OnClick = (sender1, args) =>
                        {
                            if (tag.IsAnyParentHidden())
                                return;
                            SelectedComponent = lambdaCopy;
                            PropertyPanel.SelectedComponent = SelectedComponent;
                        };

                        #endregion

                        component.GuiTag = tag;
                        ListView.AddItem(tag);
                    }
                }

                ListView.Invalidate();

                if (SelectedComponent != null)
                {
                    Drawer3D.DrawBox(SelectedComponent.GetBoundingBox(), Color.White, 0.1f, true);
                    ComponentProperties.Text = SelectedComponent.GetType().Name
                        + "\n" + SelectedComponent.Position.ToString() 
                        + "\nBB Extents: " + SelectedComponent.BoundingBoxSize.ToString() 
                        + "\nBB Offset: " + SelectedComponent.LocalBoundingBoxOffset.ToString();
                    PropertyPanel.SelectedComponent = SelectedComponent;
                    PropertyPanel.Hidden = false;
                    PropertyPanel.Invalidate();
                }
                else
                {
                    ComponentProperties.Text = "";
                    PropertyPanel.SelectedComponent = null;
                    PropertyPanel.Hidden = true;
                    PropertyPanel.Invalidate();
                }
            };

            base.Construct();
        }
    }
}
