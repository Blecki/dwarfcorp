using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class ResourceIcon : Widget
    {
        public IEnumerable<TileReference> Layers = null;

        public override void Construct()
        {
            if (Layers != null)
            {
                Widget child = this;
                foreach (var layer in Layers)
                {
                    child = child.AddChild(new Widget()
                    {
                        Rect = this.Rect,
                        AutoLayout = AutoLayout.DockFill,
                        Background = layer,
                        MaximumSize = new Point(32, 32),
                        TextHorizontalAlign = HorizontalAlign.Right,
                        TextVerticalAlign = VerticalAlign.Bottom,
                        TextColor = new Vector4(1, 1, 1, 1)
                    });
                }
            }
            base.Construct();
        }

        public bool EqualsLayers(IEnumerable<TileReference> tiles)
        {
            if (Layers == null || tiles == null)
                return false;

            var layerList = Layers.ToList();
            var tilesList = tiles.ToList();

            if (layerList.Count != tilesList.Count)
            {
                return false;
            }

            if (layerList.Where((t, i) => !t.Equals(tilesList[i])).Any())
            {
                return false;
            }
            return true;
        }
    }
    public class ResourcePanel : GridPanel
    {
        public GameMaster Master;


        private bool AreListsEqual<T>(List<T> listA, List<T> listB)
        {
            if (listA.Any(obj => !listB.Contains(obj)))
            {
                return false;
            }

            if (listB.Any(obj => !listA.Contains(obj)))
            {
                return false;
            }
            return true;
        }

        private class AggregatedResource
        {
            public ResourceAmount Amount;
            public List<string> Members = new List<string>(); 
        }
        // Aggregates resources by tags so that there aren't as many to display.
        private List<AggregatedResource> AggregateResources(IEnumerable<KeyValuePair<string, ResourceAmount>> resources)
        {
            List<AggregatedResource> aggregated = new List<AggregatedResource>();
            foreach (var pair in resources)
            {
                var resource = ResourceLibrary.GetResourceByName(pair.Value.ResourceType);
                var existing = aggregated.FirstOrDefault(existingResource => AreListsEqual(ResourceLibrary.GetResourceByName(existingResource.Amount.ResourceType).Tags, resource.Tags));

                if (existing != null)
                {
                    existing.Amount.NumResources += pair.Value.NumResources;
                    existing.Members.Add(String.Format("{0}x {1}", pair.Value.NumResources, pair.Value.ResourceType));
                }
                else
                {
                    aggregated.Add(new AggregatedResource(){Amount = pair.Value, Members = new List<string>(){String.Format("{0}x {1}", pair.Value.NumResources, pair.Value.ResourceType)}});
                }
            }
            return aggregated;
        }

        public override void Construct()
        {
            ItemSize = new Point(32, 32);
            Root.RegisterForUpdate(this);

            OnUpdate = (sender, time) =>
            {
                var existingResourceEntries = new List<Widget>(Children);
                Children.Clear();
                var aggregated =
                    AggregateResources(
                        Master.Faction.ListResourcesInStockpilesPlusMinions().Where(p => p.Value.NumResources > 0));
                foreach (var resource in aggregated)
                {
                    var resourceTemplate = ResourceLibrary.GetResourceByName(resource.Amount.ResourceType);

                    // Don't display resources with no value (a hack, yes!). This is to prevent "special" resources from getting traded.
                    if (resourceTemplate.MoneyValue == 0.0m)
                    {
                        continue;
                    }

                    var icon = existingResourceEntries.FirstOrDefault(w => w is ResourceIcon && (w as ResourceIcon).EqualsLayers(resourceTemplate.GuiLayers));

                    StringBuilder label = new StringBuilder();
                    foreach (var aggregates in resource.Members)
                    {
                        label.Append(aggregates);
                        label.Append("\n");
                    }
                    label.Append(resourceTemplate.Description);
                    if (icon == null)
                    {
                        icon = AddChild(new ResourceIcon()
                        {
                            Layers = resourceTemplate.GuiLayers,
                            Tooltip = label.ToString(),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            TextColor = new Vector4(1,1,1,1)
                        });                        
                    }
                    else
                    {
                        icon.Tooltip = label.ToString();
                        if (!Children.Contains(icon))
                        {
                            AddChild(icon);
                        }
                    }

                    icon.Children.Last().Text = resource.Amount.NumResources.ToString();
                    icon.Invalidate();                    
                }

                Layout();
            };
        }        
    }
}
