using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class VerticalScrollBar : Widget
    {
        public bool SupressOnScroll = false;

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

        public Action<Widget> OnScrollValueChanged = null;

        private void AfterScroll()
        {
            if (_scrollArea < 0) _scrollArea = 0;
            if (_scrollPosition >= _scrollArea) _scrollPosition = _scrollArea - 1;
            if (_scrollPosition < 0) _scrollPosition = 0;
            
            Invalidate();

            if (SupressOnScroll)
            {
                SupressOnScroll = false;
                return;
            }

            // Could be called during construction - before Root is set.
            if (Root != null) Root.SafeCall(OnScrollValueChanged, this);
        }

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Graphics)) Graphics = "vertical-scrollbar";

            OnClick += (sender, args) => SetFromMousePosition(args.Y);

            OnMouseMove += (sender, args) =>
            {
                if (Object.ReferenceEquals(Root.MouseDownItem, this))
                    SetFromMousePosition(args.Y);
            };

            OnScroll += (sender, args) =>
            {
                ScrollPosition = args.ScrollValue > 0 ? ScrollPosition - 1 : ScrollPosition + 1;
            };
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
            var result = Mesh.EmptyMesh();

            if (ScrollArea == 0)
                return result;

            var tiles = Root.GetTileSheet(Graphics);

            result.QuadPart()
                .Scale(tiles.TileWidth, tiles.TileHeight)
                .Translate(Rect.X, Rect.Y)
                .Texture(tiles.TileMatrix(0));

            result.QuadPart()
                .Scale(tiles.TileWidth, tiles.TileHeight)
                .Translate(Rect.X, Rect.Y + Rect.Height - tiles.TileHeight)
                .Texture(tiles.TileMatrix(3));

            result.QuadPart()
                .Scale(tiles.TileWidth, Rect.Height - tiles.TileHeight - tiles.TileHeight)
                .Translate(Rect.X, Rect.Y + tiles.TileHeight)
                .Texture(tiles.TileMatrix(1));

            var scrollSize = Rect.Height - tiles.TileHeight - tiles.TileHeight;
            var barTop = (_scrollArea == 0 ? 0.0f : ((float)_scrollPosition / (float)_scrollArea)) * scrollSize;
            var barBottom = (_scrollArea == 0 ? 0.0f : ((float)(_scrollPosition + 1) / (float)_scrollArea)) * scrollSize;

            result.Scale9Part(
                new Rectangle(Rect.X, Rect.Y + tiles.TileHeight + (int)barTop, Rect.Width, Math.Max(16, (int)(barBottom - barTop))),
                Root.GetTileSheet("brown-frame"),
                Scale9Corners.Top | Scale9Corners.Bottom | Scale9Corners.Left | Scale9Corners.Right);

            return result;
        }

        public override Point GetBestSize()
        {
            var gfx = Root.GetTileSheet(Graphics);
            return new Point(gfx.TileWidth, gfx.TileHeight * 3);
        }
    }
}
