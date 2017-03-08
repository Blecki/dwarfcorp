using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class ResourceColumns : TwoColumns
    {
        public List<ResourceAmount> SourceResources;
        public List<ResourceAmount> SelectedResources;
        public String LeftHeader;
        public String RightHeader;
        public int Money;
        private Gum.Widgets.EditableTextField MoneyField;

        public int TradeMoney { get { return Int32.Parse(MoneyField.Text); } }

        public float TotalSelectedValue
        {
            get
            {
                return SelectedResources.Sum(r =>
                    ResourceLibrary.GetResourceByName(r.ResourceType).MoneyValue * r.NumResources) +
                    TradeMoney;
            }
        }

        public Action<Widget> OnTotalSelectedChanged;

        public ResourceColumns()
        {
            SourceResources = new List<ResourceAmount>();
            SelectedResources = new List<ResourceAmount>();
        }

        public override void Construct()
        {
            var leftPanel = AddChild(new Widget());

            leftPanel.AddChild(new Gum.Widget
            {
                Text = LeftHeader,
                AutoLayout = AutoLayout.DockTop
            });

            leftPanel.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockBottom,
                Text = Money.ToString()
            });

            var leftList = leftPanel.AddChild(new Gum.Widgets.WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill
            }) as Gum.Widgets.WidgetListView;

            var rightPanel = AddChild(new Widget());

            rightPanel.AddChild(new Gum.Widget
            {
                Text = RightHeader,
                AutoLayout = AutoLayout.DockTop
            });

            MoneyField = rightPanel.AddChild(new Gum.Widgets.EditableTextField
            {
                Text = "0",
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockBottom,
                BeforeTextChange = (sender, args) =>
                {
                    var v = 0;
                    if (int.TryParse(args.NewText, out v))
                    {
                        sender.Text = args.NewText;
                        Root.SafeCall(OnTotalSelectedChanged, this);
                    }
                    else
                        args.Cancelled = true;
                }
            }) as Gum.Widgets.EditableTextField;

            var rightList = rightPanel.AddChild(new Gum.Widgets.WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill
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
                        rightList.AddItem(rightLineItem);

                        rightLineItem.TriggerOnChildClick = true;
                        rightLineItem.OnClick = (_sender, _args) =>
                        {
                            existingEntry.NumResources -= 1;

                            if (existingEntry.NumResources == 0)
                            {
                                var index = SelectedResources.IndexOf(existingEntry);
                                SelectedResources.RemoveAt(index);
                                rightList.RemoveChild(rightList.GetChild(index + 1));
                            }

                            UpdateRightColumn(rightList);

                            var sourceEntry = SourceResources.FirstOrDefault(
                                r => r.ResourceType == existingEntry.ResourceType);
                            sourceEntry.NumResources += 1;
                            UpdateLineItemText(
                                leftList.GetChild(SourceResources.IndexOf(sourceEntry) + 1), 
                                sourceEntry);

                            Root.SafeCall(OnTotalSelectedChanged, this);
                        };
                    }
                    existingEntry.NumResources += 1;

                    UpdateRightColumn(rightList);
                    UpdateLineItemText(lineItem, lambdaResource);

                    Root.SafeCall(OnTotalSelectedChanged, this);
                };

                leftList.AddItem(lineItem);
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
                AutoLayout = AutoLayout.DockLeft,
                BackgroundColor = resourceInfo.Tint.ToVector4()
            });

            r.AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Text = String.Format("{0} at ${1}e", Resource.NumResources, resourceInfo.MoneyValue),
                //Font = "outline-font",
                //TextColor = new Vector4(1,1,1,1),
                TextVerticalAlign = VerticalAlign.Center
            });

            return r;
        }

        private void UpdateLineItemText(Widget LineItem, ResourceAmount Resource)
        {
            LineItem.GetChild(1).Text = String.Format("{0} at ${1}e",
                Resource.NumResources,
                ResourceLibrary.GetResourceByName(Resource.ResourceType).MoneyValue);
            LineItem.GetChild(1).Invalidate();
        }
    }
}
