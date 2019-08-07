using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play
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

    public class StockpileContentsPanel : Gui.Widgets.GridPanel
    {
        public WorldManager World;
        public Stockpile Stockpile;

        // Todo: What is there are more resources than fit on the screen? Need scrolling!
        
        public override void Construct()
        {
            ItemSize = new Point(32, 48);
            Root.RegisterForUpdate(this);
            Transparent = true;

            OnUpdate = (sender, time) =>
            {
                var existingResourceEntries = new List<Widget>(Children);
                Children.Clear();

                var aggregated = Stockpile.Resources.Enumerate();

                foreach (var resource in aggregated)
                {
                    var resourceTemplate = Library.GetResourceType(resource.Type);
                    var icon = existingResourceEntries.FirstOrDefault(w => w is ResourceIcon && w.Tag.ToString() == resource.Type);
                    var label = resourceTemplate.Name + "\n" + resourceTemplate.Description;

                    if (icon == null)
                        icon = AddChild(new ResourceIcon()
                        {
                            Layers = resourceTemplate.GuiLayers,
                            Tooltip = label,
                            Tag = resource.Type
                        });
                    else
                    {
                        icon.Tooltip = label;
                        if (!Children.Contains(icon))
                            AddChild(icon);
                    }

                    string text = resource.Count.ToString();
                    icon.Text = text;
                    icon.Invalidate();
                }

                Layout();
            };
        }        
    }
}
