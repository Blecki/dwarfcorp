using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class GridChooser : Widget
    {
        public Point ItemSize = new Point(32, 32);
        public Point ItemSpacing = new Point(8, 8);

        public enum Result
        {
            OKAY,
            CANCEL
        }

        public Result DialogResult = Result.CANCEL;
        public string OkayText = "OKAY";
        public string CancelText = "CANCEL";

        public string SelectionBorder = "border-thin";

        private int _selection = -1;
        public int Selection
        {
            get { return _selection; }
            set
            {
                _selection = value;
                Invalidate();
            }
        }

        private int _hover = -1;
        public int HoverItem
        {
            get { return _hover; }
            set
            {
                _hover = value;
                Invalidate();
            }
        }

        public Widget SelectedItem = null;
        
        private GridPanel Panel = null;

        public IEnumerable<Widget> ItemSource;

        public override void Construct()
        {
            if (Rect.Width == 0 || Rect.Height == 0) // Ooops.
            {
                Rect.Width = 480;
                Rect.Height = 320;
            }

            //Center on screen.
            Rect.X = (Root.RenderData.VirtualScreen.Width / 2) - (Rect.Width / 2);
            Rect.Y = (Root.RenderData.VirtualScreen.Height / 2) - (Rect.Height / 2);

            Border = "border-one";

            AddChild(new Button
            {
                Text = OkayText,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) => 
                    {
                        DialogResult = Result.OKAY;
                        this.Close();
                    },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            AddChild(new Button
            {
                Text = CancelText,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) => 
                    {
                        DialogResult = Result.CANCEL;
                        this.Close();
                    },
                AutoLayout = AutoLayout.FloatBottomLeft
            });

            Panel = AddChild(new GridPanel
                {
                    AutoLayout = Gui.AutoLayout.DockFill,
                    OnLayout = (sender) => sender.Rect.Height -= 36,
                    ItemSize = ItemSize,
                    ItemSpacing = ItemSpacing
                }) as GridPanel;

            var index = 0;
            foreach (var item in ItemSource)
            {
                var lambdaIndex = index;
                var lambdaItem = item;
                item.OnClick += (sender, args) =>
                    {
                        Selection = lambdaIndex;
                        SelectedItem = item;
                    };
                item.OnMouseEnter += (sender, args) =>
                {
                    if (HoverItem != lambdaIndex)
                    {
                        HoverItem = lambdaIndex;
                    }
                };
                item.OnMouseLeave += (sender, args) =>
                {
                    if (HoverItem == lambdaIndex)
                    {
                        HoverItem = -1;
                    }
                };
                index += 1;
                Panel.AddChild(item);
            }

            Layout();
        }

        protected override Mesh Redraw()
        {
            var mesh = base.Redraw();

            if (Selection != -1)
            {
                var border = Root.GetTileSheet(SelectionBorder);
                var rect = Panel.GetChild(Selection).Rect.Interior(-border.TileWidth, -border.TileHeight, -border.TileWidth, -border.TileHeight);
                mesh.Scale9Part(rect, border);
            }

            if (HoverItem != -1)
            {
                var border = Root.GetTileSheet(SelectionBorder);
                var rect = Panel.GetChild(HoverItem).Rect.Interior(-border.TileWidth, -border.TileHeight, -border.TileWidth, -border.TileHeight);
                mesh.Scale9Part(rect, border).Colorize(new Vector4(0.5f, 0, 0, 1.0f));
            }

            return mesh;
        }
    }
}
