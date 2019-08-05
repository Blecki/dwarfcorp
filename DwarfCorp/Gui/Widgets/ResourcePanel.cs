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
                        AutoLayout = AutoLayout.DockTop,
                        Background = layer,
                        MaximumSize = new Point(32, 32),
                        MinimumSize = new Point(32,32),
                        TextHorizontalAlign = this.TextHorizontalAlign,
                        TextVerticalAlign = this.TextVerticalAlign,
                        BackgroundColor = this.BackgroundColor
                    });
                }
            }

            Font = "font10-outline-numsonly";
            TextHorizontalAlign = HorizontalAlign.Center;
            TextVerticalAlign = VerticalAlign.Bottom;
            TextColor = new Vector4(1, 1, 1, 1);
            WrapText = false;
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
        public WorldManager World;
        
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
            public Pair<ResourceAmount> Amount;
            public List<string> Members = new List<string>(); 
        }

        // Aggregates resources by tags so that there aren't as many to display.
        private List<AggregatedResource> AggregateResources(IEnumerable<KeyValuePair<string, Pair<ResourceAmount>>> resources)
        {
            List<AggregatedResource> aggregated = new List<AggregatedResource>();
            foreach (var pair in resources)
            {
                var resource = Library.GetResourceType(pair.Value.First.Type);
                var existing = aggregated.FirstOrDefault(existingResource => 
                AreListsEqual(Library.GetResourceType(existingResource.Amount.First.Type).Tags, resource.Tags));

                if (existing != null)
                {
                    existing.Amount.First.Count += pair.Value.First.Count;
                    existing.Amount.Second.Count += pair.Value.Second.Count;
                    existing.Members.Add(String.Format("{0}x {1}", pair.Value.First.Count, pair.Value.First.Type));
                }
                else
                {
                    aggregated.Add(new AggregatedResource(){Amount = pair.Value,
                        Members = new List<string>(){String.Format("{0}x {1}", pair.Value.First.Count, pair.Value.First.Type)}});
                }
            }
            return aggregated;
        }

        public override void Construct()
        {
            ItemSize = new Point(32, 64);
            Root.RegisterForUpdate(this);
            Background = new TileReference("basic", 0);
            BackgroundColor = new Vector4(0, 0, 0, 0.5f);
            OnUpdate = (sender, time) =>
            {
                var existingResourceEntries = new List<Widget>(Children);
                Children.Clear();

                var aggregated = AggregateResources(World.ListResourcesInStockpilesPlusMinions().Where(p => p.Value.First.Count > 0 || p.Value.Second.Count > 0));

                foreach (var resource in aggregated)
                {
                    var resourceTemplate = Library.GetResourceType(resource.Amount.First.Type);

                    // Don't display resources with no value (a hack, yes!). This is to prevent "special" resources from getting traded.
                    if (resourceTemplate.MoneyValue == 0.0m)
                        continue;

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

                    string text = "S" + resource.Amount.First.Count.ToString();
                    text += "\n";
                    if (resource.Amount.Second.Count > 0)
                    {
                        text += "I" + resource.Amount.Second.Count.ToString();
                    }
                    icon.Text = text;
                    icon.Invalidate();
                }
                var width = Root.RenderData.VirtualScreen.Width - ItemSpacing.X;
                var itemsThatFit = width / (ItemSize.X + ItemSpacing.X);
                var sensibleWidth = (Math.Min(Children.Count, itemsThatFit) * (ItemSize.X + ItemSpacing.X)) + ItemSpacing.X;
                Rect = new Rectangle((Root.RenderData.VirtualScreen.Width / 2 - sensibleWidth / 2), 0, sensibleWidth, 32);
                Layout();
            };
        }        
    }
}
