using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    /// <summary>
    /// Display a list of items with a scrollbar. Items are selected by clicking.
    /// Like a normal list view, except items are widgets instead of strings.
    /// </summary>
    public class WidgetListView : Widget
    {
        private int _selectedIndex = 0;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set 
            { 
                _selectedIndex = value;
                if (_selectedIndex < 0) _selectedIndex = 0;
                if (_selectedIndex >= Children.Count - 1) _selectedIndex = Children.Count - 2;
                if (Root != null) 
                    Root.SafeCall(OnSelectedIndexChanged, this);
                Invalidate();
            }
        }

        public Action<Widget> OnSelectedIndexChanged = null;
        public Vector4 SelectedItemBackgroundColor = new Vector4(0.5f, 0, 0, 1);
        public Vector4 SelectedItemForegroundColor = new Vector4(1, 1, 1, 1);
        public Vector4 ItemBackgroundColor1 = new Vector4(0, 0, 0, 0.2f);
        public Vector4 ItemBackgroundColor2 = new Vector4(0, 0, 0, 0);

        public Widget SelectedItem
        {
            get
            {
                if (SelectedIndex >= 0 && SelectedIndex < Children.Count - 1)
                    return Children[SelectedIndex + 1];
                else return null;
            }
        }

        private VerticalScrollBar ScrollBar;
        public int ItemHeight = 32;

        public bool ChangeColorOnSelected = true;
        public bool CheckBorder = true;

        public override void Construct()
        {
            if (CheckBorder && String.IsNullOrEmpty(Border)) Border = "border-one";

            ScrollBar = new VerticalScrollBar
            {
                OnScrollValueChanged = (sender) => { this.Invalidate(); },
                AutoLayout = AutoLayout.DockRight
            };


            AddChild(ScrollBar);

            OnClick += (sender, args) =>
                {
                    if (ScrollBar.Hidden || args.X < ScrollBar.Rect.Left)
                    {
                        SelectedIndex = ScrollBar.ScrollPosition + ((args.Y - GetDrawableInterior().Y) / ItemHeight);
                    }
                };

            OnScroll = (sender, args) =>
            {
                Root.SafeCall(ScrollBar.OnScroll, ScrollBar, args);
            };

            TriggerOnChildClick = true;
        }

        public Widget AddItem(Widget Item)
        {
            Item.AutoLayout = Gui.AutoLayout.None;
            return AddChild(Item);
        }

        public void ClearItems()
        {
            Children.Clear();
            Children.Add(ScrollBar);
        }

        public List<Widget> GetItems()
        {
            return Children.Skip(1).ToList();
        }

        public override Point GetBestSize()
        {
            if (Rect.Width == 0 || Rect.Height == 0) return new Point(128, 128); // Arbitrary!
            return new Point(Rect.Width, Rect.Height);
        }

        protected override Mesh Redraw()
        {
            if (Children.Count > 1)
                ItemHeight = Math.Max(Children.GetRange(1, Children.Count - 1).Max(child => Math.Max(child.Rect.Height, child.MinimumSize.Y)), ItemHeight);
            var font = Root.GetTileSheet(Font);
            var drawableInterior = GetDrawableInterior();
            drawableInterior.Width = (Children[0].Rect.Left - drawableInterior.Left - Padding.Right);

            int itemsThatFit = 0;
            List<int> heights = new List<int>();
            int sumHeight = 0;
            foreach(var child in Children)
            {
                int h = Math.Max(ItemHeight, child.MinimumSize.Y);
                sumHeight += h;
                heights.Add(h);
                if (sumHeight <= drawableInterior.Height)
                {
                    itemsThatFit++;
                }
                else
                {
                    break;
                }
            }
            // Update scrollbar scroll area.
            if (itemsThatFit >= Children.Count - 1)
            {
                ScrollBar.ScrollArea = 0;
                ScrollBar.Hidden = true;
            }
            else
            {
                ScrollBar.ScrollArea = Children.Count - itemsThatFit;
                ScrollBar.Hidden = false;
            }

            var topItem = ScrollBar.ScrollPosition;
            if (topItem < 0) topItem = 0;

            var topPos = drawableInterior.Y;

            for (int i = 1; i < Children.Count; ++i)
            {
                Children[i].Hidden = true;
                Children[i].Invalidate();
                if (i % 2 == 0)
                {
                    Children[i].BackgroundColor = ItemBackgroundColor1;
                }
                else
                {
                    Children[i].BackgroundColor = ItemBackgroundColor2;
                }
                Children[i].TextColor = TextColor;
            }

            for (int i = 0; i < itemsThatFit && (topItem + i) < (Children.Count - 1); ++i)
            {
                Children[topItem + i + 1].Rect = new Rectangle(drawableInterior.Left, topPos, drawableInterior.Width,
                    heights[i]);
                Children[topItem + i + 1].Layout();
                Children[topItem + i + 1].Hidden = false;
                topPos += ItemHeight;
            }


            if (SelectedItem != null && ChangeColorOnSelected)
            {
                if (SelectedItemBackgroundColor.W > 1e-2)
                {
                    SelectedItem.BackgroundColor = SelectedItemBackgroundColor;
                }
                SelectedItem.TextColor = SelectedItemForegroundColor;
            }

            return base.Redraw();
        }
    }
}
