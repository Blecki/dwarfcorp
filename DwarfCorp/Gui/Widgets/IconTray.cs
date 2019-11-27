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
        public int WidthLimit = 1024;
        public int IconOffset = 0;
        private int IconCount { get { return Children.Count - 2; } }
        private Widget GetIcon(int i) { return GetChild(i + 2); }
        public bool HotKeys = false;
        public Action<Widget> OnRefresh;
        public bool AlwaysPerfectSize = false;
        private int SavedWidth = 0;

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
                ItemSource = new List<Widget>();

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

            ResetItemsFromSource();
        }

        public void ResetItemsFromSource()
        {
            Children.Clear();

            var items = ItemSource.ToList();

            if (AlwaysPerfectSize)
                SizeToGrid = new Point(items.Count, 1);
            else
            {
                SizeToGrid.X = Math.Min(items.Count, SavedWidth / ItemSize.X);
                int numRows = (int)Math.Ceiling((float)(ItemSource.Count()) / (float)(SizeToGrid.X));
                SizeToGrid.Y = Math.Max(numRows, 1);
            }
            // Todo: Ever not 1 row?

            // Calculate perfect size. Margins + item sizes + padding.
            MaximumSize.X = InteriorMargin.Left + InteriorMargin.Right + (SizeToGrid.X * ItemSize.X) +
                            ((SizeToGrid.X - 1) * ItemSpacing.X);
            MaximumSize.Y = InteriorMargin.Top + InteriorMargin.Bottom + (SizeToGrid.Y * ItemSize.Y) +
                            ((SizeToGrid.Y - 1) * ItemSpacing.Y);
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

            foreach (var item in items)
                AddChild(item);

            LayoutIcons();
        }

        public override void Layout()
        {
            Root.SafeCall(OnLayout, this);
            //Rect = MathFunctions.SnapRect(Rect, Root.RenderData.VirtualScreen);
            SavedWidth = Rect.Width;

            LayoutIcons();
        }

        public void LayoutIcons()
        { 
            var rect = GetDrawableInterior();

            foreach (var child in EnumerateChildren())
                child.Hidden = true;

            var totalItemWidth = (IconCount * (ItemSize.X + ItemSpacing.X)) - ItemSpacing.X;

            var nextHotkey = 0;

            GetChild(0).Hidden = true;
            GetChild(1).Hidden = true;
            (GetChild(0) as FramedIcon).HotkeyValue = 0;
            (GetChild(1) as FramedIcon).HotkeyValue = 0;
            
            if (totalItemWidth > rect.Width)
            {
                // Need to paginate.
                var pos = new Point(rect.X, rect.Y);

                // Always add Icon 0 first.
                GetIcon(0).Hidden = false;
                GetIcon(0).Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                GetIcon(0).Layout();
                pos.X += ItemSize.X + ItemSpacing.X;

                nextHotkey = AssignHotkey(nextHotkey, GetIcon(0));


                // Add back button.
                (GetChild(0) as FramedIcon).Enabled = IconOffset > 0;
                GetChild(0).Hidden = false;
                GetChild(0).Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                pos.X += ItemSize.X + ItemSpacing.X;
                nextHotkey = AssignHotkey(nextHotkey, GetChild(0));

                (GetChild(1) as FramedIcon).Enabled = false;

                for (var c = IconOffset + 1; c < IconCount; ++c)
                {
                    var child = GetIcon(c);
                    child.Hidden = false;
                    child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                    pos.X += ItemSize.X + ItemSpacing.X;
                    child.Layout();
                    nextHotkey = AssignHotkey(nextHotkey, child);

                    if (pos.X + ItemSize.X + ItemSpacing.X + ItemSize.X + ItemSpacing.X >= rect.Right)
                    {
                        if (c < IconCount - 1)
                            (GetChild(1) as FramedIcon).Enabled = true;
                        break;
                    }

                }

                // Add more button.
                GetChild(1).Hidden = false;
                GetChild(1).Rect = new Rectangle(pos.X, Rect.Y, ItemSize.X, ItemSize.Y);
                GetChild(1).Layout();
                nextHotkey = AssignHotkey(nextHotkey, GetChild(1));
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
                    nextHotkey = AssignHotkey(nextHotkey, child);
                    pos.X += ItemSize.X + ItemSpacing.X;
                    child.Layout();
                }
            }

            Invalidate();   
        }

        private int AssignHotkey(int nextHotkey, Widget child)
        {
            if (nextHotkey >= 0 && nextHotkey < FlatToolTray.Tray.Hotkeys.Count)
            {
                (child as FramedIcon).HotkeyValue = FlatToolTray.Tray.Hotkeys[nextHotkey];
                (child as FramedIcon).DrawHotkey = HotKeys;
                ++nextHotkey;
            }
            else
            {
                (child as FramedIcon).HotkeyValue = 0;
                (child as FramedIcon).DrawHotkey = false;
            }
            return nextHotkey;
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
