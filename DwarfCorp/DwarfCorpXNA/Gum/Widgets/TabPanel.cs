using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Gum.Widgets
{
    /// <summary>
    /// Display a set of widgets, controlling visibility using tabs.
    /// </summary>
    public class TabPanel : Widget
    {
        private class TabButton : Widget
        {
            public override void Construct()
            {
                Font = "font-hires";
                base.Construct();
            }

            protected override Mesh Redraw()
            {
                Rectangle drop;
                var border = Root.GetTileSheet(Graphics);
                var bgMesh = Mesh.CreateScale9Background(Rect.Interior(0, 0, 0, -border.TileHeight), border,
                    Scale9Corners.Top | Scale9Corners.Left | Scale9Corners.Right);

                var parent = Parent as TabPanel;
                if (!Object.ReferenceEquals(parent.GetTabButton(parent.SelectedTab), this))
                    bgMesh.Colorize(new Vector4(0.75f, 0.75f, 0.75f, 1.0f));

                var labelMesh = Mesh.CreateStringMesh(Text, Root.GetTileSheet(Font), new Vector2(TextSize, TextSize), out drop)
                    .Translate(Rect.X + border.TileWidth, Rect.Y + border.TileHeight)
                    .Colorize(TextColor);
                return Mesh.Merge(bgMesh, labelMesh);                
            }

            public override Point GetBestSize()
            {
                var font = Root.GetTileSheet(Font);
                var border = Root.GetTileSheet(Graphics);
                var labelSize = font.MeasureString(Text).Scale(TextSize);
                return new Point(labelSize.X + (2 * border.TileWidth), labelSize.Y + border.TileHeight);
            }
        }

        private class InteriorPanel : Widget
        {
            protected override Mesh Redraw()
            {
                var border = Root.GetTileSheet(Graphics);
                var tabPanel = Parent as TabPanel;
                var tabButton = tabPanel.GetTabButton(tabPanel.SelectedTab);
                return Mesh.CreateTabPanelBackground(Rect, border, tabButton.Rect.X, tabButton.Rect.Width);
            }

            public override void Layout()
            {
                var border = Root.GetTileSheet(Graphics);
                foreach (var child in Children)
                {
                    child.Rect = Rect.Interior(border.TileWidth, border.TileHeight, border.TileWidth,
                        border.TileHeight);
                    child.Layout();
                }
            }
        }

        private List<Widget> TabPanels = new List<Widget>();
        private List<Widget> TabButtons = new List<Widget>();
        public int TabPadding = 4;

        internal Widget GetTabButton(int Index)
        {
            return TabButtons[Index];
        }

        private int _selectedTab = 0;
        public int SelectedTab
        {
            get { return _selectedTab; }
            set
            {
                _selectedTab = value;

                foreach (var child in TabPanels)
                    child.Hidden = true;
                TabPanels[_selectedTab].Hidden = false;
                Root.SafeCall(TabPanels[_selectedTab].OnShown, TabPanels[_selectedTab]);

                foreach (var tab in TabButtons) tab.Invalidate();

                if (Root != null)
                    Root.SafeCall(OnSelectedTabChanged, this);

                Invalidate();
            }
        }

        public Action<Widget> OnSelectedTabChanged = null;
        public Vector4 SelectedTabColor = new Vector4(1, 0, 0, 1);

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Graphics)) Graphics = "border-one";
        }

        /// <summary>
        /// Override layout to disable layout engine on children - just make them fill the space.
        /// </summary>
        public override void Layout()
        {
            Root.SafeCall(this.OnLayout, this);

            if (TabButtons.Count == 0) return;

            var tabPosition = new Point(Rect.X, Rect.Y);
            foreach (var tabButton in TabButtons)
            {
                var bestSize = tabButton.GetBestSize();
                tabButton.Rect = new Rectangle(tabPosition.X, tabPosition.Y, bestSize.X, bestSize.Y);
                tabPosition.X += bestSize.X + TabPadding;
                tabButton.Invalidate();
            }

            var tabSize = TabButtons[0].GetBestSize();
            // Todo: Honor all margins.
            var interior = GetDrawableInterior().Interior(0, tabSize.Y, 0, 0);
            foreach (var child in TabPanels)
            {
                child.Rect = interior;
                child.Layout();
            }
        }

        public Widget AddTab(String Name, Widget Tab)
        {
            var tabPosition = new Point(Rect.X, Rect.Y);
            if (TabButtons.Count > 0)
                tabPosition.X = TabButtons[TabButtons.Count - 1].Rect.Right + TabPadding;

            var tabIndex = TabButtons.Count;
            var tabButton = AddChild(new TabButton
            {
                Text = Name,
                Graphics = Graphics,
                OnClick = (sender, args) => SelectedTab = tabIndex,
                TextSize = TextSize,
                TextColor = TextColor,
                OnMouseEnter = (widget, action) =>
                {
                    widget.TextColor = new Vector4(0.5f, 0, 0, 1.0f);
                    widget.Invalidate();
                },
                OnMouseLeave = (widget, action) =>
                {
                    widget.TextColor = TextColor;
                    widget.Invalidate();
                }
            });

            SendToBack(tabButton);

            var tabSize = tabButton.GetBestSize();
            tabButton.Rect = new Rectangle(tabPosition.X, tabPosition.Y, tabSize.X, tabSize.Y);
            TabButtons.Add(tabButton);

            var tabPanel = AddChild(new InteriorPanel
                {
                    Graphics = Graphics
                });
            tabPanel.AddChild(Tab);
            AddChild(tabPanel);
            TabPanels.Add(tabPanel);

            if (TabButtons.Count == 1)
                tabPanel.Hidden = false;
            else
                tabPanel.Hidden = true;
            
            return Tab;
        }        
    }
}
