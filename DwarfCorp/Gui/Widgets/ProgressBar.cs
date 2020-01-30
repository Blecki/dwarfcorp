using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class ProgressBar : Widget
    {
        private float _percentage = 0.0f;
        public float Percentage
        {
            get { return _percentage; }
            set
            {
                _percentage = value;
                Invalidate();
            }
        }

        public Vector4 FillColor = new Vector4(0, 0, 1, 1);

        protected override Mesh Redraw()
        {
            var sides = Root.GetTileSheet("progress-sides");
            var middle = Root.GetTileSheet("progress-middle");
            var fill = Root.GetTileSheet("progress-fill");

            var fillArea = Rect.Width - 24;
            var fillTo = (int)(fillArea * Percentage * 0.01f);

            var r = Mesh.EmptyMesh();

            r.TiledSpritePart(new Rectangle(Rect.X + 12, Rect.Y, fillTo, Rect.Height), fill, 0)
                .Colorize(FillColor);
            r.QuadPart()
                .Scale(fill.TileWidth, fill.TileHeight)
                .Translate(Rect.X + fillTo + 12, Rect.Y)
                .Texture(fill.TileMatrix(1))
                .Colorize(FillColor);
            r.QuadPart()
                .Scale(sides.TileWidth, sides.TileHeight)
                .Translate(Rect.X, Rect.Y)
                .Texture(sides.TileMatrix(0));
            r.QuadPart()
                .Scale(sides.TileWidth, sides.TileHeight)
                .Translate(Rect.X + Rect.Width - sides.TileWidth, Rect.Y)
                .Texture(sides.TileMatrix(1));
            r.TiledSpritePart(Rect.Interior(sides.TileWidth, 0, sides.TileWidth, 0), middle, 0);

            return Mesh.Merge(r, base.Redraw()); // This can be removed when we switch completely to in place drawing
        }

        public override Point GetBestSize()
        {
            var baseBest = base.GetBestSize();
            baseBest.Y = 32;
            return baseBest;
        }
    }
}
