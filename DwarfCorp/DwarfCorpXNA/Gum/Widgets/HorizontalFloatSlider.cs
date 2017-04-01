using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Gum.Widgets
{
    public class HorizontalFloatSlider : Widget
    {
        private float _scrollArea;
        public float ScrollArea
        {
            get { return _scrollArea; }
            set { _scrollArea = value; AfterScroll(); }
        }

        private float _scrollPosition;
        public float ScrollPosition
        {
            get { return _scrollPosition; }
            set { _scrollPosition = value; AfterScroll(); }
        }

        public float ScrollPercentage
        {
            get { return _scrollArea == 0 ? 0.0f : (_scrollPosition / _scrollArea); }
            set { _scrollPosition = (_scrollArea * value); AfterScroll(); }
        }

        public int EndBufferSize = 12;

        public Action<Widget> OnScroll = null;

        private void AfterScroll()
        {
            if (_scrollPosition < 0) _scrollPosition = 0;
            if (_scrollPosition > _scrollArea) _scrollPosition = _scrollArea;

            Invalidate();

            // Could be called during construction - before Root is set.
            if (Root != null) Root.SafeCall(OnScroll, this);
        }

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Graphics)) Graphics = "horizontal-slider";

            OnClick += (sender, args) => SetFromMousePosition(args.X);
            OnMouseMove += (sender, args) => SetFromMousePosition(args.X);
        }

        private void SetFromMousePosition(int X)
        {
            var gfx = Root.GetTileSheet(Graphics);
            var scrollSize = Rect.Width - (2 * EndBufferSize);
            var clickX = X - Rect.X - EndBufferSize;
            ScrollPercentage = (float)clickX / (float)scrollSize;
        }

        protected override Mesh Redraw()
        {
            var tiles = Root.GetTileSheet(Graphics);

            var background = Mesh.Quad()
                .Scale(Rect.Width, Rect.Height)
                .Translate(Rect.X, Rect.Y)
                .Texture(tiles.TileMatrix(0));

            var scrollSize = Rect.Width - (2 * EndBufferSize);
            var barPosition = ScrollPercentage * scrollSize;
            var pixelPosition = Rect.X + (int)barPosition + EndBufferSize;

            var bar = Mesh.Quad()
                .Scale(tiles.TileWidth, tiles.TileHeight)
                .Translate(pixelPosition - (tiles.TileWidth / 2), Rect.Y)
                .Texture(tiles.TileMatrix(1));

            return Mesh.Merge(background, bar);
        }

        public override Point GetBestSize()
        {
            var gfx = Root.GetTileSheet(Graphics);
            return new Point(gfx.TileWidth * 5, gfx.TileHeight);
        }
    }
}
