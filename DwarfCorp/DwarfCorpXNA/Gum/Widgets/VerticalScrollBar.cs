using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Gum.Widgets
{
    public class VerticalScrollBar : Widget
    {
        private int _scrollArea;
        public int ScrollArea
        {
            get { return _scrollArea; }
            set { _scrollArea = value; AfterScroll(); }
        }

        private int _scrollPosition;
        public int ScrollPosition
        {
            get { return _scrollPosition; }
            set { _scrollPosition = value; AfterScroll(); }
        }

        public float ScrollPercentage
        {
            get { return _scrollArea == 0 ? 0.0f : ((float)_scrollPosition / (float)_scrollArea); }
            set { _scrollPosition = (int)(_scrollArea * value); AfterScroll(); }
        }

        public Action<Widget> OnScroll = null;

        private void AfterScroll()
        {
            if (_scrollArea < 0) _scrollArea = 0;
            if (_scrollPosition >= _scrollArea) _scrollPosition = _scrollArea - 1;
            if (_scrollPosition < 0) _scrollPosition = 0;
            
            Invalidate();

            // Could be called during construction - before Root is set.
            if (Root != null) Root.SafeCall(OnScroll, this);
        }

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Graphics)) Graphics = "vertical-scrollbar";

            OnClick += (sender, args) => SetFromMousePosition(args.Y);
            OnMouseMove += (sender, args) => SetFromMousePosition(args.Y);
        }

        private void SetFromMousePosition(int Y)
        {
            var gfx = Root.GetTileSheet(Graphics);
            var scrollSize = Rect.Height - gfx.TileHeight - gfx.TileHeight;
            var clickY = Y - Rect.Y - gfx.TileHeight;
            if (clickY >= 0 && clickY < scrollSize)
                ScrollPercentage = (float)clickY / (float)scrollSize;
            else if (clickY < 0)
                ScrollPosition -= 1;
            else if (clickY >= scrollSize)
                ScrollPosition += 1;
        }

        protected override Mesh Redraw()
        {
            var tiles = Root.GetTileSheet(Graphics);

            var topButton = Mesh.Quad()
                .Scale(tiles.TileWidth, tiles.TileHeight)
                .Translate(Rect.X, Rect.Y)
                .Texture(tiles.TileMatrix(0));

            var bottomButton = Mesh.Quad()
                .Scale(tiles.TileWidth, tiles.TileHeight)
                .Translate(Rect.X, Rect.Y + Rect.Height - tiles.TileHeight)
                .Texture(tiles.TileMatrix(3));

            var background = Mesh.Quad()
                .Scale(tiles.TileWidth, Rect.Height - tiles.TileHeight - tiles.TileHeight)
                .Translate(Rect.X, Rect.Y + tiles.TileHeight)
                .Texture(tiles.TileMatrix(1));

            var scrollSize = Rect.Height - tiles.TileHeight - tiles.TileHeight;
            var barTop = (_scrollArea == 0 ? 0.0f : ((float)_scrollPosition / (float)_scrollArea)) * scrollSize;
            var barBottom = (_scrollArea == 0 ? 0.0f : ((float)(_scrollPosition + 1) / (float)_scrollArea)) * scrollSize;
            var barPosition = ScrollPercentage * scrollSize;
            var pixelPosition = Rect.Y + tiles.TileHeight + (int)barPosition;

            var bar = Mesh.CreateScale9Background(
                new Rectangle(Rect.X, Rect.Y + tiles.TileHeight + (int)barTop, Rect.Width, Math.Max(16, (int)(barBottom - barTop))),
                Root.GetTileSheet("border-thin"),
                Scale9Corners.Top | Scale9Corners.Bottom);

            return Mesh.Merge(topButton, bottomButton, background, bar);
        }

        public override Point GetBestSize()
        {
            var gfx = Root.GetTileSheet(Graphics);
            return new Point(gfx.TileWidth, gfx.TileHeight * 3);
        }
    }
}
