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
}
