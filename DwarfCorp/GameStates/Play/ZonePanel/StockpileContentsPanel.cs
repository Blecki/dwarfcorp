using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play
{
    public class StockpileContentsPanel : Gui.Widgets.GridPanel
    {
        public WorldManager World;
        public ResourceSet Resources;

        // Todo: What if there are more resources than fit on the screen? Need scrolling!
        
        public override void Construct()
        {
            ItemSize = new Point(32, 48);
            Root.RegisterForUpdate(this);
            Transparent = true;

            OnUpdate = (sender, time) =>
            {
                if (Resources == null)
                {
                    Children.Clear();
                    return;
                }

                var existingResourceEntries = new List<Widget>(Children);
                Children.Clear();

                var aggregated = Resources.AggregateByType();

                foreach (var resource in aggregated)
                {
                    var resourceTemplate = Library.GetResourceType(resource.Type);
                    if (!resourceTemplate.HasValue())
                        resourceTemplate = Library.GetResourceType("Invalid");

                    if (resourceTemplate.HasValue(out var template))
                    {
                        var icon = existingResourceEntries.FirstOrDefault(w => w is ResourceIcon && w.Tag.ToString() == resource.Type);

                        var label = template.Name + "\n" + template.Description;

                        if (icon == null)
                            icon = AddChild(new ResourceIcon()
                            {
                                Layers = template.GuiLayers,
                                Tooltip = label,
                                Tag = resource.Type
                            });
                        else
                        {
                            icon.Tooltip = label;
                            if (!Children.Contains(icon))
                                AddChild(icon);
                        }

                        //string text = resource.ToString();
                        icon.Text = resource.Count.ToString();
                        icon.Invalidate();
                    }
                }

                Layout();
            };
        }        
    }
}
