using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class HorizontalSlider : Widget
    {
        private int _scrollMin = 1;
        public int ScrollMin
        {
            get { return _scrollMin; }
            set { _scrollMin = value; AfterScroll(); }
        }

        private int _scrollMax = 100;
        public int ScrollMax
        {
            get { return _scrollMax; }
            set { _scrollMax = value; AfterScroll(); }
        }

        private int _scrollSize => _scrollMax - _scrollMin + 1;

        private int _scrollPosition;
        public int ScrollPosition
        {
            get { return _scrollPosition; }
            set { _scrollPosition = value; AfterScroll(); }
        }

        public float ScrollPercentage
        {
            get { return _scrollSize <= 0 ? 0.0f : ((float)(_scrollPosition - _scrollMin) / (float)_scrollSize); }
            set { _scrollPosition = _scrollMin + (int)(_scrollSize * value); AfterScroll(); }
        }

        public int EndBufferSize = 12;

        public Action<Widget> OnSliderChanged = null;

        private void AfterScroll()
        {
            if (_scrollPosition < _scrollMin) _scrollPosition = _scrollMin;
            if (_scrollPosition > _scrollMax) _scrollPosition = _scrollMax;

            Invalidate();

            // Could be called during construction - before Root is set.
            if (Root != null) Root.SafeCall(OnSliderChanged, this);
        }

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Graphics)) Graphics = "horizontal-slider";

            OnClick += (sender, args) => UpdateFromMousePosition(args.X);
            OnMouseMove += (sender, args) =>
            {
                if (Object.ReferenceEquals(Root.MouseDownItem, this))
                    UpdateFromMousePosition(args.X);
            };

            OnScroll += (sender, args) =>
            {
                ScrollPosition = args.ScrollValue > 0 ? ScrollPosition + 1 : ScrollPosition - 1;
            };
        }

        private void UpdateFromMousePosition(int X)
        {
            var gfx = Root.GetTileSheet(Graphics);
            var scrollSize = Rect.Width - (2 * EndBufferSize);
            var clickX = X - Rect.X - EndBufferSize;
            ScrollPercentage = (float)clickX / (float)scrollSize;
        }

        protected override Mesh Redraw()
        {
            var mesh = Mesh.EmptyMesh();
            var tiles = Root.GetTileSheet(Graphics);

            mesh.QuadPart()
                .Scale(Rect.Width, Rect.Height)
                .Translate(Rect.X, Rect.Y)
                .Texture(tiles.TileMatrix(0));

            var scrollSize = Rect.Width - (2 * EndBufferSize);
            var barPosition = ScrollPercentage * scrollSize;
            var pixelPosition = Rect.X + (int)barPosition + EndBufferSize;

            mesh.QuadPart()
                .Scale(tiles.TileWidth, tiles.TileHeight)
                .Translate(pixelPosition - (tiles.TileWidth / 2), Rect.Y)
                .Texture(tiles.TileMatrix(1));

            return mesh;
        }

        public override Point GetBestSize()
        {
            var gfx = Root.GetTileSheet(Graphics);
            return new Point(gfx.TileWidth * 5, gfx.TileHeight);
        }
    }
}
