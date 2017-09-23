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
        
        public override void Construct()
        {
            ItemSize = new Point(32, 32);
            Root.RegisterForUpdate(this);

            OnUpdate = (sender, time) =>
            {
                var existingResourceEntries = new List<Widget>(Children);
                Children.Clear();

                foreach (var resource in Master.Faction.ListResourcesInStockpilesPlusMinions().Where(p => p.Value.NumResources > 0))
                {
                    var resourceTemplate = ResourceLibrary.GetResourceByName(resource.Key);

                    // Don't display resources with no value (a hack, yes!). This is to prevent "special" resources from getting traded.
                    if (resourceTemplate.MoneyValue == 0.0m)
                    {
                        continue;
                    }

                    var icon = existingResourceEntries.FirstOrDefault(w => w is ResourceIcon && (w as ResourceIcon).EqualsLayers(resourceTemplate.GuiLayers));

                    if (icon == null)
                    {
                        icon = AddChild(new ResourceIcon()
                        {
                            Layers = resourceTemplate.GuiLayers,
                            Tooltip = string.Format("{0} - {1}",
                                    resourceTemplate.ResourceName,
                                    resourceTemplate.Description),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            TextColor = new Vector4(1,1,1,1)
                        });                        
                    }
                    else if (!Children.Contains(icon))
                    {
                        AddChild(icon);
                    }

                    icon.Children.Last().Text = resource.Value.NumResources.ToString();
                    icon.Invalidate();                    
                }

                Layout();
            };
        }        
    }
}
