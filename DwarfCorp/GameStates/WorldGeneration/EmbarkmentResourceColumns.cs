using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using DwarfCorp.Trade;

namespace DwarfCorp.GameStates
{
    public class EmbarkmentResourceColumns : Columns
    {
        public List<TradeableItem> SourceResources;
        public List<TradeableItem> SelectedResources;
        public String LeftHeader;
        public String RightHeader;
        
        public int TotalSelectedItems
        {
            get
            {
                return SelectedResources.Sum(r => r.Count);
            }
        }

        public Action<Widget> OnTotalSelectedChanged;

        public EmbarkmentResourceColumns()
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

            var rightmostList = rightmostPanel.AddChild(new Gui.Widgets.WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0),
                Font = GameSettings.Current.GuiScale == 1 ? "font10" : "font8"
            }) as Gui.Widgets.WidgetListView;

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

        private void SetupList(WidgetListView listA, WidgetListView listB, List<TradeableItem> resourcesA, 
            List<TradeableItem> resourcesB)
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

                    var resourcesToMove = lambdaResource.Resources.Take(toMove).ToList();
                    lambdaResource.Resources.RemoveRange(0, toMove);
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_change_selection, 0.1f, MathFunctions.Rand() * 0.25f);

                    var existingEntry = resourcesB.FirstOrDefault(r => r.ResourceType == lambdaResource.ResourceType);
                    if (existingEntry == null)
                    {
                        existingEntry = new TradeableItem { ResourceType = lambdaResource.ResourceType, Resources = resourcesToMove, Prototype = resourcesToMove[0] };
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

                            var _resourcesTomove = existingEntry.Resources.Take(_toMove).ToList();
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
                            sourceEntry.Resources.AddRange(_resourcesTomove);
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
                        existingEntry.Resources.AddRange(resourcesToMove);
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
            Reconstruct(SourceResources, SelectedResources, 0);
        }

        private void UpdateColumn(Gui.Widgets.WidgetListView ListView, List<TradeableItem> selectedResources)
        {
            for (var i = 0; i < SelectedResources.Count; ++i)
                UpdateLineItemText(ListView.GetChild(i + 1), selectedResources[i]);
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

            if (Library.GetResourceType(Resource.ResourceType).HasValue(out var res))
                r.AddChild(new Play.ResourceIcon()
                {
                    MinimumSize = new Point(32 + 16, 32 + 16),
                    MaximumSize = new Point(32 + 16, 32 + 16),
                    Resource = new Resource(res),
                    AutoLayout = AutoLayout.DockLeft,
                    BackgroundColor = Resource.Count > 0 ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f) : new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
                    TextColor = Color.White.ToVector4(),
                    TextHorizontalAlign = HorizontalAlign.Right,
                    TextVerticalAlign = VerticalAlign.Bottom
                });
            else
                r.AddChild(new Widget());

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
                    var measurements = font.MeasureString(label);
                    label = font.WordWrapString(label, 1.0f, 128 / GameSettings.Current.GuiScale, LineItem.WrapWithinWords);
                    if (128 / GameSettings.Current.GuiScale < measurements.X)
                        LineItem.MinimumSize.Y = font.TileHeight * label.Split('\n').Length;
                }
                LineItem.GetChild(1).Text = label;
                LineItem.GetChild(1).Invalidate();

                var counter = LineItem.GetChild(0).Children.Last();
                counter.Text = Resource.Count.ToString();
                counter.Invalidate();

                LineItem.GetChild(0).Invalidate();
                LineItem.Tooltip = Resource.Prototype.DisplayName + "\n" + Resource.Prototype.Description;

                for (int i = 0; i < 3; i++)
                {
                    if (i > 0)
                        LineItem.GetChild(i).TextColor = Resource.Count > 0 ? Color.Black.ToVector4() : new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                    LineItem.GetChild(i).BackgroundColor = Resource.Count > 0 ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f) : new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                    LineItem.GetChild(i).Tooltip = LineItem.Tooltip;
                    LineItem.GetChild(i).Invalidate();
                }
            }
        }
    }
}
