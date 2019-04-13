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

            return Mesh.Merge(
                Mesh.TiledSprite(new Rectangle(Rect.X + 12, Rect.Y, fillTo, Rect.Height),
                    fill, 0)
                    .Colorize(FillColor),
                Mesh.Quad()
                    .Scale(fill.TileWidth, fill.TileHeight)
                    .Translate(Rect.X + fillTo + 12, Rect.Y)
                    .Texture(fill.TileMatrix(1))
                    .Colorize(FillColor),
                Mesh.Quad()
                    .Scale(sides.TileWidth, sides.TileHeight)
                    .Translate(Rect.X, Rect.Y)
                    .Texture(sides.TileMatrix(0)),
                Mesh.Quad()
                    .Scale(sides.TileWidth, sides.TileHeight)
                    .Translate(Rect.X + Rect.Width - sides.TileWidth, Rect.Y)
                    .Texture(sides.TileMatrix(1)),
                Mesh.TiledSprite(Rect.Interior(sides.TileWidth, 0, sides.TileWidth, 0),
                    middle, 0),
                base.Redraw());
        }

        public override Point GetBestSize()
        {
            var baseBest = base.GetBestSize();
            baseBest.Y = 32;
            return baseBest;
        }
    }
}
