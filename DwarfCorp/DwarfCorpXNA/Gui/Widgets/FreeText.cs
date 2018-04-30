using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class FreeText : Gui.Widget
    {
        public Vector4 TextBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f);

        protected override Gui.Mesh Redraw()
        {
            var meshes = new List<Gui.Mesh>();
            var stringScreenSize = new Rectangle();
            var font = Root.GetTileSheet(Font);
            var basic = Root.GetTileSheet("basic");

                var stringMesh = Gui.Mesh.CreateStringMesh(Text, font, new Vector2(TextSize, TextSize), out stringScreenSize)
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(TextColor);
                meshes.Add(Gui.Mesh.Quad()
                    .Scale(stringScreenSize.Width, stringScreenSize.Height)
                    .Translate(Rect.X, Rect.Y)
                    .Texture(basic.TileMatrix(1))
                    .Colorize(TextBackgroundColor));
                meshes.Add(stringMesh);

            return Gui.Mesh.Merge(meshes.ToArray());
        }
    }
}
