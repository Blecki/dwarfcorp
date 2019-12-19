using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play
{
    public class ContentsPanel : Gui.Widgets.GridPanel
    {
        public WorldManager World;
        public ResourceSet Resources;
        public bool EnableDragAndDrop = false;
        public Func<Widget, DragAndDrop.DraggedItem> CreateDraggableItem = null;

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
                    var icon = existingResourceEntries.FirstOrDefault(w => w is ResourceIcon  resIcon && Object.ReferenceEquals(resIcon.Resource, resource.Sample));

                    if (icon == null)
                    {
                        icon = AddChild(new ResourceIcon()
                        {
                            Resource = resource.Sample,
                            EnableDragAndDrop = EnableDragAndDrop,
                            CreateDraggableItem = CreateDraggableItem
                        });

                    }
                    else
                    {
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
