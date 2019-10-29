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

        private class AggregatedResource
        {
            public Resource Sample;
            public int Count;
        }

        private List<AggregatedResource> AggregateByType()
        {
            var r = new Dictionary<String, AggregatedResource>();
            var nonStackables = new List<Resource>();
            foreach (var res in Resources.Enumerate())
            {
                if (res.Aggregate == false)
                    nonStackables.Add(res);
                else
                {
                    if (r.ContainsKey(res.TypeName))
                        r[res.TypeName].Count += 1;
                    else
                        r.Add(res.TypeName, new AggregatedResource
                        {
                            Sample = res,
                            Count = 1
                        });
                }
            }

            return r.Values.Concat(nonStackables.Select(n => new AggregatedResource { Sample = n, Count = 1 })).ToList();
        }

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

                foreach (var resource in AggregateByType())
                {
                    var icon = existingResourceEntries.FirstOrDefault(w => w is ResourceIcon && Object.ReferenceEquals(w.Tag, resource.Sample));

                    var label = resource.Sample.DisplayName + "\n" + resource.Sample.Description; // Resources of the same type will get collapsed won't they?

                    if (icon == null)
                        icon = AddChild(new ResourceIcon()
                        {
                            Layers = resource.Sample.GuiLayers,
                            Tooltip = label,
                            Tag = resource.Sample
                        });
                    else
                    {
                        icon.Tooltip = label;
                        if (!Children.Contains(icon))
                            AddChild(icon);
                    }

                    icon.Text = resource.Count.ToString();
                    icon.Invalidate();
                }

                Layout();
            };
        }        
    }
}
