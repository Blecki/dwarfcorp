using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    /// <summary>
    /// Display a list of items with a scrollbar. Items are selected by clicking.
    /// </summary>
    public class ListView : Widget
    {
        public List<String> Items = new List<String>();

        private int _selectedIndex = 0;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set 
            { 
                _selectedIndex = value; 
                if (Root != null) 
                    Root.SafeCall(OnSelectedIndexChanged, this);
                Invalidate();
            }
        }

        public Action<Widget> OnSelectedIndexChanged = null;
        public Vector4 SelectedTextColor = new Vector4(1, 0, 0, 1);

        public String SelectedItem
        {
            get
            {
                if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
                    return Items[SelectedIndex];
                else return null;
            }
        }

        private VerticalScrollBar ScrollBar;
        public int ItemHeight { get { return (Root.GetTileSheet(Font).TileHeight * TextSize) + 2; } }

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Border)) Border = "border-one";
            ScrollBar = new VerticalScrollBar
            {
                OnScrollValueChanged = (sender) => { this.Invalidate(); },
                AutoLayout = AutoLayout.DockRight
            };
            AddChild(ScrollBar);

            OnClick += (sender, args) =>
                {
                    SelectedIndex = ScrollBar.ScrollPosition + ((args.Y - GetDrawableInterior().Y) / ItemHeight);
                };

            OnScroll = (sender, args) =>
            {
                Root.SafeCall(ScrollBar.OnScroll, ScrollBar, args);
            };
            
        }

        public override Point GetBestSize()
        {
            if (Rect.Width == 0 || Rect.Height == 0) return new Point(128, 128); // Arbitrary!
            return new Point(Rect.Width, Rect.Height);
        }

        protected override Mesh Redraw()
        {
            var font = Root.GetTileSheet(Font);
            var itemHeight = (font.TileHeight * TextSize) + 2;
            var drawableInterior = GetDrawableInterior();
            var itemsThatFit = drawableInterior.Height / itemHeight;

            // Update scrollbar scroll area.
            if (itemsThatFit > Items.Count) ScrollBar.ScrollArea = 0;
            else ScrollBar.ScrollArea = Items.Count - itemsThatFit + 1;

            var topItem = ScrollBar.ScrollPosition;
            if (topItem < 0) topItem = 0;

            var stringPos = drawableInterior.Y;
            var meshes = new List<Mesh>();
            meshes.Add(base.Redraw());
            Rectangle toss;
            for (int i = 0; i < itemsThatFit && (topItem + i) < Items.Count; ++i)
            {
                meshes.Add(Mesh.CreateStringMesh(Items[topItem + i], font, new Vector2(TextSize, TextSize), out toss)
                    .Translate(drawableInterior.X, stringPos)
                    .Colorize(topItem + i == SelectedIndex ? SelectedTextColor : TextColor));
                stringPos += (font.TileHeight * TextSize) + 2;
            }

            return Mesh.Merge(meshes.ToArray());
        }
    }
}
