using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using DwarfCorp.Trade;

namespace DwarfCorp.Play.Trading
{
    public class Balance : Widget
    {
        private float _balance = 0.0f;
        public float TradeBalance
        {
            get { return _balance; }
            set { _balance = value; Update(); }
        }

        public Widget LeftWidget;
        public Widget RightWidget;
        public Widget LeftHook;
        public Widget RightHook;
        //public Widget CenterWidget;
        public Widget Bar;
        public Widget LeftItems;
        public Widget RightItems;

        private IEnumerable<Resource> GetTopResources(List<Resource> resources, int num = 3)
        {
            return resources.Take(Math.Max(resources.Count, num));
        }

        public void SetTradeItems(List<Resource> leftResources, List<Resource> rightResources, DwarfBux leftMoney, DwarfBux rightMoney)
        {
            Update();
            LeftItems.Clear();
            RightItems.Clear();

            var left = GetTopResources(leftResources).ToList();
            int leftCount = left.Count + (leftMoney > 0.0m ? 1 : 0);

            int k = 0;
            foreach (var resource in left)
            {
                if (Library.GetResourceType(resource.TypeName).HasValue(out var resourceType))
                    LeftItems.AddChild(new Play.ResourceIcon()
                    {
                        Resource = resource,
                        MinimumSize = new Point(32, 32),
                        MaximumSize = new Point(32, 32),
                        Rect = new Rectangle(LeftWidget.Rect.X + 16 + k * 4 - leftCount * 2, LeftWidget.Rect.Y + 5, 32, 32)
                    });
                k++;
            }

            if (leftMoney > 0.0m)
            {
                LeftItems.AddChild(new Widget()
                {
                    Background = new TileReference("coins", 1),
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    Rect = new Rectangle(LeftWidget.Rect.X + 16 + k * 4 - leftCount * 2, LeftWidget.Rect.Y + 5, 32, 32)
                });
            }

            var right = GetTopResources(rightResources).ToList();
            int rightCount = right.Count + (rightMoney > 0.0m ? 1 : 0);
            k = 0;
            foreach (var resource in GetTopResources(rightResources))
            {
                if (Library.GetResourceType(resource.TypeName).HasValue(out var resourceType))
                    RightItems.AddChild(new Play.ResourceIcon()
                    {
                        Resource = resource,
                        MinimumSize = new Point(32, 32),
                        MaximumSize = new Point(32, 32),
                        Rect = new Rectangle(RightWidget.Rect.X + 16 + k * 4 - rightCount * 2, RightWidget.Rect.Y + 5, 32, 32)
                    });
                k++;
            }

            if (rightMoney > 0.0m)
            {
                RightItems.AddChild(new Widget()
                {
                    Background = new TileReference("coins", 1),
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    Rect = new Rectangle(RightWidget.Rect.X + 16 + k * 4 - rightCount * 2, RightWidget.Rect.Y + 5, 32, 32)
                });
            }
            LeftItems.Invalidate();
            RightItems.Invalidate();
        }

        public void Update()
        {
            var rect = Rect;
            //CenterWidget.Rect = new Rectangle(rect.Center.X - 16, rect.Top, 32, 32);
            Bar.Rect = new Rectangle(rect.Center.X - 32, rect.Top - 32, 64, 48);
            Bar.Rotation = -_balance * 0.5f;
            Bar.Invalidate();
            float dy = 32 * (float)Math.Sin(_balance * 0.5f);
            LeftWidget.Rect = new Rectangle(rect.Center.X - 28 - 32, rect.Top - 12 + (int)dy, 64, 48);
            LeftHook.Rect = new Rectangle(LeftWidget.Rect.X, LeftWidget.Rect.Y - 3, LeftWidget.Rect.Width, LeftWidget.Rect.Height);
            LeftWidget.Invalidate();
            LeftHook.Invalidate();
            RightWidget.Rect = new Rectangle(rect.Center.X + 28 - 32, rect.Top - 12 - (int)dy, 64, 48);
            RightHook.Rect = new Rectangle(RightWidget.Rect.X, RightWidget.Rect.Y - 3, RightWidget.Rect.Width, RightWidget.Rect.Height);
            RightWidget.Invalidate();
            RightHook.Invalidate();
            Layout();
        }

        public override void Construct()
        {
            LeftWidget = AddChild(new Widget()
            {
                Background = new TileReference("balance", 0),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48)
            });
            LeftHook = LeftWidget.AddChild(new Widget()
            {
                Background = new TileReference("balance", 1),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48)
            });
            LeftItems = LeftWidget.AddChild(new Widget()
            {
                MaximumSize = new Point(32, 32),
                MinimumSize = new Point(32, 32)
            });
            RightWidget = AddChild(new Widget()
            {
                Background = new TileReference("balance", 0),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48)
            });
            RightHook = RightWidget.AddChild(new Widget()
            {
                Background = new TileReference("balance", 2),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48)
            });
            RightItems = RightWidget.AddChild(new Widget()
            {
                MinimumSize = new Point(32, 32),
                MaximumSize =  new Point(32, 32)
            });
            /*
            CenterWidget = AddChild(new Widget()
            {
                Background = new TileReference("balance", 1),
                MaximumSize = new Point(32, 32),
                MinimumSize = new Point(32, 32)
            });*/
            Bar = AddChild(new Widget()
            {
                Background = new TileReference("balance", 3),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48),
                Tag = "trade_balance"
        });
            Update();
            base.Construct();
        }
    }
}
