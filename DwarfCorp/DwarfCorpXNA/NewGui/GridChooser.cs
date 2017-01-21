using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
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
            Rect.X = (Root.VirtualScreen.Width / 2) - (Rect.Width / 2);
            Rect.Y = (Root.VirtualScreen.Height / 2) - (Rect.Height / 2);

            Border = "border-one";

            AddChild(new Widget
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

            AddChild(new Widget
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
                    AutoLayout = Gum.AutoLayout.DockFill,
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
                index += 1;
                Panel.AddChild(item);
            }

            Layout();
        }

        protected override Gum.Mesh Redraw()
        {
            if (Selection != -1)
            {
                var border = Root.GetTileSheet(SelectionBorder);
                var rect = Panel.GetChild(Selection).Rect.Interior(-border.TileWidth, -border.TileHeight, -border.TileWidth, -border.TileHeight);
                return Gum.Mesh.Merge(base.Redraw(), Gum.Mesh.CreateScale9Background(rect, border));
            }
            else
                return base.Redraw();
        }
    }
}
