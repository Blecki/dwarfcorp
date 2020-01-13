
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
        public Action<Widget, InputEventArgs> OnIconClicked = null;

        // Todo: What if there are more resources than fit on the screen? Need scrolling!

        private class AggregatedResource
        {
            public Resource Sample;
            public int Count;
        }

        private List<AggregatedResource> AggregateByType()
        {
            var r = new Dictionary<String, AggregatedResource>();
            foreach (var res in Resources.Enumerate())
            {
                if (r.ContainsKey(res.DisplayName))
                    r[res.DisplayName].Count += 1;
                else
                    r.Add(res.DisplayName, new AggregatedResource
                    {
                        Sample = res,
                        Count = 1
                    });
            }

            return r.Values.ToList();
        }

        public override void Construct()
        {
            ItemSize = new Point(32, 48);
            Root.RegisterForUpdate(this);
            if (String.IsNullOrEmpty(Border))
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
                            CreateDraggableItem = CreateDraggableItem,
                            OnClick = OnIconClicked
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
