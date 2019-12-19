using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play
{
    public class DraggedResourceIcon : DragAndDrop.DraggedItem
    {
        private Resource _Resource = null;
        public Resource Resource
        {
            set
            {
                _Resource = value;
                Invalidate();
            }

            get
            {
                return _Resource;
            }
        }

        protected override Mesh Redraw()
        {
            if (_Resource == null)
                return base.Redraw();

            var r = new List<Mesh>();
            foreach (var layer in _Resource.GuiLayers)
                r.Add(Mesh.Quad()
                            .Scale(Rect.Width, Rect.Height)
                            .Translate(Rect.X, Rect.Y)
                            .Colorize(BackgroundColor)
                            .Texture(Root.GetTileSheet(layer.Sheet).TileMatrix(layer.Tile)));
            r.Add(base.Redraw());
            return Mesh.Merge(r.ToArray());
        }
    }
}
