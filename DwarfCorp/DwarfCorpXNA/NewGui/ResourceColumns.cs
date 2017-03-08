using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class ResourceColumns : Widget
    {
        public List<ResourceAmount> SourceResources;
        public List<ResourceAmount> SelectedResources;

        public enum ColumnOrder
        {
            SourceFirst,
            SelectedFirst
        }

        public ColumnOrder Order = ColumnOrder.SelectedFirst;

        public ResourceColumns()
        {
            SourceResources = new List<ResourceAmount>();
            SelectedResources = new List<ResourceAmount>();
        }

        public override void Construct()
        {
            var leftPanel = AddChild(new Gum.Widgets.WidgetListView
            {
                ItemHeight = 32
            }) as Gum.Widgets.WidgetListView;

            var rightPanel = AddChild(new Gum.Widgets.WidgetListView
            {

            }) as Gum.Widgets.WidgetListView;
        
            foreach (var resource in SourceResources)
            {
                var lineItem = CreateLineItem(resource);

                var lambdaResource = resource;
                lineItem.TriggerOnChildClick = true;
                lineItem.OnClick = (sender, args) =>
                {
                    if (lambdaResource.NumResources == 0) return;

                    lambdaResource.NumResources -= 1;

                    var existingEntry = SelectedResources.FirstOrDefault(r => r.ResourceType == lambdaResource.ResourceType);
                    if (existingEntry == null)
                    {
                        existingEntry = new ResourceAmount(lambdaResource.ResourceType, 0);
                        SelectedResources.Add(existingEntry);
                        var rightLineItem = CreateLineItem(existingEntry);
                        rightPanel.AddItem(rightLineItem);

                        rightLineItem.TriggerOnChildClick = true;
                        rightLineItem.OnClick = (_sender, _args) =>
                        {
                            existingEntry.NumResources -= 1;

                            if (existingEntry.NumResources == 0)
                            {
                                var index = SelectedResources.IndexOf(existingEntry);
                                SelectedResources.RemoveAt(index);
                                rightPanel.RemoveChild(rightPanel.GetChild(index + 1));
                            }

                            UpdateRightColumn(rightPanel);

                            var sourceEntry = SourceResources.FirstOrDefault(
                                r => r.ResourceType == existingEntry.ResourceType);
                            sourceEntry.NumResources += 1;
                            UpdateLineItemText(
                                leftPanel.GetChild(SourceResources.IndexOf(sourceEntry) + 1), 
                                sourceEntry);
                        };
                    }
                    existingEntry.NumResources += 1;

                    UpdateRightColumn(rightPanel);
                    UpdateLineItemText(lineItem, lambdaResource);
                };

                leftPanel.AddItem(lineItem);
            }

        }

        private void UpdateRightColumn(Gum.Widgets.WidgetListView ListView)
        {
            for (var i = 0; i < SelectedResources.Count; ++i)
                UpdateLineItemText(ListView.GetChild(i + 1), SelectedResources[i]);
        }

        private Widget CreateLineItem(ResourceAmount Resource)
        {
            var r = Root.ConstructWidget(new Gum.Widget
            {
                MinimumSize = new Point(1, 32),
                MaximumSize = new Point(1, 32)
            });

            var resourceInfo = ResourceLibrary.GetResourceByName(Resource.ResourceType);

            r.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                Background = new TileReference("resources", resourceInfo.NewGuiSprite),
                AutoLayout = AutoLayout.DockLeft
            });

            r.AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Text = String.Format("{0} at ${1} each", Resource.NumResources, resourceInfo.MoneyValue),
                Font = "outline-font",
                TextColor = new Vector4(1,1,1,1),
                TextVerticalAlign = VerticalAlign.Center
            });

            return r;
        }

        private void UpdateLineItemText(Widget LineItem, ResourceAmount Resource)
        {
            LineItem.GetChild(1).Text = String.Format("{0} at ${1} each",
                Resource.NumResources,
                ResourceLibrary.GetResourceByName(Resource.ResourceType).MoneyValue);
            LineItem.GetChild(1).Invalidate();
        }

        public override void Layout()
        {
            if (Order == ColumnOrder.SelectedFirst)
            {
                Children[0].Rect = new Rectangle(Rect.X, Rect.Y, Rect.Width / 2, Rect.Height);
                Children[0].Layout();

                Children[1].Rect = new Rectangle(Rect.X + Rect.Width / 2, Rect.Y, Rect.Width / 2, Rect.Height);
                Children[1].Layout();
            }
            else
            {
                Children[1].Rect = new Rectangle(Rect.X, Rect.Y, Rect.Width / 2, Rect.Height);
                Children[1].Layout();

                Children[0].Rect = new Rectangle(Rect.X + Rect.Width / 2, Rect.Y, Rect.Width / 2, Rect.Height);
                Children[0].Layout();
            }
        }


    }
}
