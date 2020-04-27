using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using DwarfCorp.Trade;

namespace DwarfCorp.Gui.Widgets
{
    public class TradeableItem
    {
        public List<Resource> Resources = new List<Resource>();
        public int Count => Resources.Count;
        public String ResourceType;
        public Resource Prototype = null;
    }

    public class ResourceColumns : Columns
    {
        public ITradeEntity TradeEntity;
        public ITradeEntity ValueSourceEntity;
        public List<TradeableItem> SourceResources { get; private set; }
        public List<TradeableItem> SelectedResources { get; private set; }
        public String LeftHeader;
        public String RightHeader;
        public MoneyEditor MoneyField;
        public String MoneyLabel;
        public bool ShowMoneyField = true;

        public DwarfBux TradeMoney
        {
            get { return (decimal)(MoneyField == null ? 0 : MoneyField.CurrentValue); }
            set { if (MoneyField != null) MoneyField.CurrentValue = (int) value; }
        }
        public bool Valid { get { return MoneyField == null ? true : MoneyField.Valid; } }
        
        public int TotalSelectedItems
        {
            get
            {
                return SelectedResources.Sum(r => r.Count);
            }
        }

        public Action<Widget> OnTotalSelectedChanged;

        public ResourceColumns()
        {
            SourceResources = new List<TradeableItem>();
            SelectedResources = new List<TradeableItem>();
        }

        public void Reconstruct(IEnumerable<TradeableItem> sourceResource, 
                                IEnumerable<TradeableItem> selectedResources,
                                int tradeMoney)
        {
            Clear();
            SourceResources = Clone(sourceResource.ToList());
            SelectedResources = Clone(selectedResources.ToList());


            var leftmostPanel = AddChild(new Widget());

            leftmostPanel.AddChild(new Gui.Widget
            {
                Text = RightHeader,
                Font = "font16",
                TextColor = new Vector4(0, 0, 0, 1),
                AutoLayout = AutoLayout.DockTop
            });

            var rightmostPanel = AddChild(new Widget());


            rightmostPanel.AddChild(new Gui.Widget
            {
                Text = LeftHeader,
                Font = "font16",
                TextColor = new Vector4(0, 0, 0, 1),
                AutoLayout = AutoLayout.DockTop
            });

            if (ShowMoneyField)
                rightmostPanel.AddChild(new Gui.Widget
                {
                    MinimumSize = new Point(0, 32),
                    AutoLayout = AutoLayout.DockBottom,
                    Font = "font10",
                    TextColor = new Vector4(0, 0, 0, 1),
                    Text = String.Format(MoneyLabel + ": {0}", TradeEntity.Money.ToString()),
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center
                });

            var rightmostList = rightmostPanel.AddChild(new Gui.Widgets.WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0),
                Font = GameSettings.Current.GuiScale == 1 ? "font10" : "font8"
            }) as Gui.Widgets.WidgetListView;

            if (ShowMoneyField)
            {
                MoneyField = leftmostPanel.AddChild(new MoneyEditor
                {
                    MaximumValue = (int)TradeEntity.Money,
                    MinimumSize = new Point(0, 33),
                    AutoLayout = AutoLayout.DockBottom,
                    OnValueChanged = (sender) => Root.SafeCall(OnTotalSelectedChanged, this),
                    Tooltip = "Money to trade."
                }) as MoneyEditor;
            }

            var leftmostList = leftmostPanel.AddChild(new Gui.Widgets.WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0),
                Font = GameSettings.Current.GuiScale == 1 ? "font10" : "font8"
            }) as Gui.Widgets.WidgetListView;

            // Lists should have bidirectional properties.
            SetupList(leftmostList, rightmostList, SourceResources, SelectedResources);
            SetupList(rightmostList, leftmostList, SelectedResources, SourceResources);
        }

        private void SetupList(WidgetListView listA, WidgetListView listB, List<TradeableItem> resourcesA, List<TradeableItem> resourcesB)
        {
            foreach (var resource in resourcesA)
            {
                var lineItem = CreateLineItem(resource);
                var lambdaResource = resource;

                lineItem.TriggerOnChildClick = true;
                lineItem.EnableHoverClick();

                lineItem.OnClick = (sender, args) =>
                {
                    if (lambdaResource.Count <= 0) return;
                    var toMove = 1;
                    if (args.Control) toMove = lambdaResource.Count;
                    if (args.Shift) toMove = Math.Min(5, lambdaResource.Count);
                    if (lambdaResource.Count - toMove < 0)
                        return;

                    var movedItems = lambdaResource.Resources.Take(toMove).ToList();
                    lambdaResource.Resources.RemoveRange(0, toMove);
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_change_selection, 0.1f, MathFunctions.Rand() * 0.25f);

                    var existingEntry = resourcesB.FirstOrDefault(r => r.Prototype.DisplayName == lambdaResource.Prototype.DisplayName);
                    if (existingEntry == null)
                    {
                        existingEntry = new TradeableItem { Resources = movedItems, ResourceType = lambdaResource.ResourceType, Prototype = movedItems[0] };
                        resourcesB.Add(existingEntry);
                        var rightLineItem = CreateLineItem(existingEntry);
                        rightLineItem.EnableHoverClick();
                        listA.AddItem(rightLineItem);

                        rightLineItem.TriggerOnChildClick = true;
                        rightLineItem.OnClick = (_sender, _args) =>
                        {
                            var _toMove = 1;
                            if (_args.Control) _toMove = existingEntry.Count;
                            if (_args.Shift)
                                _toMove = Math.Min(5, existingEntry.Count);
                            if (existingEntry.Count - _toMove < 0)
                                return;

                            var _movedItems = existingEntry.Resources.Take(_toMove).ToList();
                            existingEntry.Resources.RemoveRange(0, _toMove);
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_change_selection, 0.1f, MathFunctions.Rand() * 0.25f);

                            if (existingEntry.Count == 0)
                            {
                                var index = resourcesB.IndexOf(existingEntry);
                                if (index >= 0)
                                {
                                    resourcesB.RemoveAt(index);
                                    listA.RemoveChild(listA.GetChild(index + 1));
                                }
                            }

                            UpdateColumn(listA, resourcesB);

                            var sourceEntry = resourcesA.FirstOrDefault(r => r.ResourceType == existingEntry.ResourceType);
                            int idx = resourcesA.IndexOf(sourceEntry);
                            sourceEntry.Resources.AddRange(_movedItems);
                            if (idx >= 0)
                            {
                                UpdateLineItemText(
                                    listB.GetChild(resourcesA.IndexOf(sourceEntry) + 1),
                                    sourceEntry);
                            }
                            Root.SafeCall(OnTotalSelectedChanged, this);
                        };
                    }
                    else
                    {
                        existingEntry.Resources.AddRange(movedItems);
                    }

                    UpdateColumn(listA, resourcesB);
                    UpdateLineItemText(lineItem, lambdaResource);

                    Root.SafeCall(OnTotalSelectedChanged, this);
                };

                listB.AddItem(lineItem);
            }
        }


        private List<TradeableItem> Clone(List<TradeableItem> resources)
        {
            return resources.Select(r => new TradeableItem { Resources = new List<Resource>(r.Resources), ResourceType = r.ResourceType, Prototype = r.Prototype }).ToList();
        }

        public override void Construct()
        {
            //SourceResources = Play.Trading.Helper.AggregateResourcesIntoTradeableItems(TradeEntity.Resources);

            //Reconstruct(SourceResources, SelectedResources, 0);
        }

        private void UpdateColumn(Gui.Widgets.WidgetListView ListView, List<TradeableItem> selectedResources)
        {
            var lineItems = ListView.GetItems();
            ListView.ClearItems();
            for (var i = 0; i < SelectedResources.Count; ++i)
            {
                Widget lineItem = null;
                if (i >= lineItems.Count)
                    lineItem = CreateLineItem(selectedResources[i]);
                else
                    lineItem = lineItems[i];
                
                UpdateLineItemText(lineItem, selectedResources[i]);

                ListView.AddItem(lineItem);
            }
        }

        private void UpdateRightColumn(Gui.Widgets.WidgetListView ListView)
        {
            UpdateColumn(ListView, SelectedResources);
        }

        private Widget CreateLineItem(TradeableItem Resource)
        {
            var r = Root.ConstructWidget(new Gui.Widget
            {
                MinimumSize = new Point(1, 32),
                Background = new TileReference("basic", 0)
            });

            r.AddChild(new Play.ResourceIcon()
            {
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                Resource = Resource.Prototype,
                AutoLayout = AutoLayout.DockLeft,
                BackgroundColor = Resource.Count > 0 ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f) : new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
                TextColor = Color.White.ToVector4(),
                TextHorizontalAlign = HorizontalAlign.Right,
                TextVerticalAlign = VerticalAlign.Bottom
            });

            r.AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(128 / GameSettings.Current.GuiScale, 0),
                MaximumSize = new Point(128 / GameSettings.Current.GuiScale, 32),
                TextColor = Resource.Count > 0 ? Color.Black.ToVector4() : new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Left,
                HoverTextColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4(),
                Font = GameSettings.Current.GuiScale == 1 ? "font10" : "font8",
                ChangeColorOnHover = true,
                WrapText = true
            });

            r.AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockRight,
                //Text = String.Format("{0} at ${1}e", Resource.NumResources, resourceInfo.MoneyValue),
                //Font = "font18-outline",
                //TextColor = new Vector4(1,1,1,1),
                TextColor = Resource.Count > 0 ? Color.Black.ToVector4() : new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
                TextVerticalAlign = VerticalAlign.Center,
                HoverTextColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4(),
                Font = GameSettings.Current.GuiScale == 1 ? "font10" : "font8",
                ChangeColorOnHover = true,
            });
            

            r.Layout();
            UpdateLineItemText(r, Resource);

            return r;
        }

        private void UpdateLineItemText(Widget LineItem, TradeableItem Resource)
        {
            if (Resource.Prototype != null)
            {
                var font = LineItem.Root.GetTileSheet("font10");
                var label = Resource.Prototype.DisplayName;
                if (font != null)
                {
                    Point measurements = font.MeasureString(label);
                    label = font.WordWrapString(label, 1.0f, 128 / GameSettings.Current.GuiScale, LineItem.WrapWithinWords);
                    if (128 / GameSettings.Current.GuiScale < measurements.X)
                    {
                        LineItem.MinimumSize.Y = font.TileHeight * label.Split('\n').Length;
                    }
                }
                LineItem.GetChild(1).Text = label;
                LineItem.GetChild(1).Invalidate();
                LineItem.GetChild(2).Text = String.Format("{0}", ValueSourceEntity.ComputeValue(Resource.Prototype));
                LineItem.GetChild(0).Text = Resource.Count.ToString();
                LineItem.GetChild(0).Invalidate();
                LineItem.Tooltip = Resource.Prototype.DisplayName + "\n" + Resource.Prototype.Description;
                for (int i = 0; i < 3; i++)
                {
                    if (i > 0)
                    {
                        LineItem.GetChild(i).TextColor = Resource.Count > 0
                            ? Color.Black.ToVector4()
                            : new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                    }
                    LineItem.GetChild(i).BackgroundColor = Resource.Count > 0
                        ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                        : new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                    LineItem.GetChild(i).Tooltip = Resource.Prototype.DisplayName + "\n" + Resource.Prototype.Description;
                    LineItem.GetChild(i).Invalidate();
                }
            }
        }
    }
}
