using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class IconTray : Widget
    {
        public Point ItemSize = new Point(40, 40);
        public Point ItemSpacing = new Point(2, 2);
        public Point SizeToGrid = new Point(1, 1);
        public int WidthLimit = 512;
        public int IconOffset = 0;
        private int IconCount { get { return Children.Count - 2; } }
        private Widget GetIcon(int i) { return GetChild(i + 2); }
        public bool HotKeys = false;

        public IEnumerable<Widget> ItemSource;

        public Scale9Corners Corners = Scale9Corners.All;

        public IconTray()
        {
            Border = "tray-border";
        }

        public override Rectangle GetDrawableInterior()
        {
            // Don't account for border.
            return Rect.Interior(InteriorMargin);
        }

        public override void Construct()
        {
            if (ItemSource == null)
            {
                ItemSource = new List<Widget>();
            }

            if (Border == "tray-border")
            {
                // This is a hack.
                InteriorMargin = new Margin(0, 0, 0, 0);
                if (Corners.HasFlag(Scale9Corners.Top)) InteriorMargin.Top = 12;
                if (Corners.HasFlag(Scale9Corners.Bottom)) InteriorMargin.Bottom = 12;
                if (Corners.HasFlag(Scale9Corners.Left)) InteriorMargin.Left = 16;
                if (Corners.HasFlag(Scale9Corners.Right)) InteriorMargin.Right = 16;
            }

            Padding = new Margin(0, 0, 0, 0);

            //if (SizeToGrid.X > 1)
            //{
            //    SizeToGrid.X = Math.Min(SizeToGrid.X, WidthLimit/ItemSize.X);
            //    int numRows = (int)Math.Ceiling((float)(ItemSource.Count())/(float)(SizeToGrid.X));
            //    SizeToGrid.Y = Math.Max(numRows, 1);
            //}
            // Calculate perfect size. Margins + item sizes + padding.
            MaximumSize.X = InteriorMargin.Left + InteriorMargin.Right + (SizeToGrid.X*ItemSize.X) +
                            ((SizeToGrid.X - 1)*ItemSpacing.X);
            MaximumSize.Y = InteriorMargin.Top + InteriorMargin.Bottom + (SizeToGrid.Y*ItemSize.Y) +
                            ((SizeToGrid.Y - 1)*ItemSpacing.Y);
            MinimumSize = MaximumSize;

            Rect.Width = MinimumSize.X;
            Rect.Height = MinimumSize.Y;

            AddChild(new FramedIcon
            {
                OnClick = (sender, args) =>
                {
                    IconOffset -= 1;
                    LayoutIcons();
                },
                Icon = null,
                Text = "<<",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                EnabledTextColor = Vector4.One,
            });

            AddChild(new FramedIcon
            {
                OnClick = (sender, args) =>
                {
                    IconOffset += 1;
                    LayoutIcons();
                },
                Icon = null,
                Text = ">>",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                EnabledTextColor = Vector4.One,
            });

            foreach (var item in ItemSource)
                AddChild(item);
        }

        public override void Layout()
        {
            Root.SafeCall(OnLayout, this);
            //Rect = MathFunctions.SnapRect(Rect, Root.RenderData.VirtualScreen);

            LayoutIcons();
        }

        public void LayoutIcons()
        { 
            var rect = GetDrawableInterior();

            foreach (var child in EnumerateChildren())
            {
                child.Hidden = true;
                (child as FramedIcon).HotkeyValue = int.MaxValue;
            }

            var totalItemWidth = (IconCount * (ItemSize.X + ItemSpacing.X)) - ItemSpacing.X;

            var nextHotkey = 1;

            if (totalItemWidth > rect.Width)
            {
                // Need to paginate.
                var itemsThatFit = (rect.Width / ItemSize.X) - 1;
                var pos = new Point(rect.X, rect.Y);

                // Always add Icon 0 first.
                GetIcon(0).Hidden = false;
                GetIcon(0).Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                GetIcon(0).Layout();
                pos.X += ItemSize.X + ItemSpacing.X;

                    (GetIcon(0) as FramedIcon).HotkeyValue = nextHotkey;
                    (GetIcon(0) as FramedIcon).DrawHotkey = HotKeys;
                    ++nextHotkey;

                // Add back button.
                (GetChild(0) as FramedIcon).Enabled = IconOffset > 0;
                GetChild(0).Hidden = false;
                GetChild(0).Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                pos.X += ItemSize.X + ItemSpacing.X;
                (GetChild(0) as FramedIcon).HotkeyValue = nextHotkey;
                (GetChild(0) as FramedIcon).DrawHotkey = HotKeys;
                ++nextHotkey;


                (GetChild(1) as FramedIcon).Enabled = false;

                for (var c = IconOffset + 1; c < IconCount; ++c)
                {
                    var child = GetIcon(c);
                    child.Hidden = false;
                    child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                    pos.X += ItemSize.X + ItemSpacing.X;
                    child.Layout();
                    (child as FramedIcon).HotkeyValue = nextHotkey;
                    (child as FramedIcon).DrawHotkey = HotKeys;
                    ++nextHotkey;

                    if (pos.X >= rect.Right - ItemSize.X)
                    {
                        (GetChild(1) as FramedIcon).Enabled = true;
                        break;
                    }

                }

                // Add more button.
                GetChild(1).Hidden = false;
                GetChild(1).Rect = new Rectangle(Rect.Right - ItemSize.X, Rect.Y, ItemSize.X, ItemSize.Y);
                (GetChild(1) as FramedIcon).HotkeyValue = nextHotkey;
                (GetChild(1) as FramedIcon).DrawHotkey = HotKeys;
                ++nextHotkey;
            }
            else
            {
                IconOffset = 0;

                var pos = new Point(rect.X, rect.Y);
                for (var c = 0; c < IconCount; ++c)
                {
                    var child = GetIcon(c);
                    child.Hidden = false;
                    child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                    (child as FramedIcon).HotkeyValue = nextHotkey;
                    (child as FramedIcon).DrawHotkey = HotKeys;
                    ++nextHotkey;
                    pos.X += ItemSize.X + ItemSpacing.X;
                    //if (pos.X > rect.Right - ItemSize.X)
                    //{
                    //    pos.X = rect.X;
                    //    pos.Y += ItemSize.Y + ItemSpacing.Y;
                    //}
                    child.Layout();
                }
            }

            foreach (var child in EnumerateChildren())
            {
                if ((child as FramedIcon).HotkeyValue == 10)
                    (child as FramedIcon).HotkeyValue = 0;
            }

            Invalidate();   
        }

        protected override Gui.Mesh Redraw()
        {
            if (Border != null && !Transparent)
            {
                return Gui.Mesh.CreateScale9Background(Rect, Root.GetTileSheet(Border), Corners);
            }
            else
            {
                return base.Redraw();
            }
        }
    }
}
