using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class HorizontalMenuTray
    {
        public class Tray : Widget
        {
            public Point ItemSize = new Point(110, 20);
            public Point SizeToGrid = new Point(1, 1);
            public int Columns = 1;
            public bool AutoSizeColumns = false;

            public IEnumerable<Widget> ItemSource;

            public bool IsRootTray = false;

            public override void Construct()
            {
                if (AutoSizeColumns)
                {
                    var numItems = ItemSource.Count();
                    Columns = (int)(Math.Ceiling(((float)numItems * ItemSize.Y) / Root.RenderData.VirtualScreen.Height));
                }

                if (ItemSource != null)
                {
                    SizeToGrid = new Point(Columns, (int)global::System.Math.Ceiling((float)ItemSource.Count() / (float)Columns));
                }

                // Calculate perfect size. Margins + item sizes + padding.
                MaximumSize.X = SizeToGrid.X * ItemSize.X;
                MaximumSize.Y = SizeToGrid.Y * ItemSize.Y;
                MinimumSize = MaximumSize;

                Rect.Width = MinimumSize.X;
                Rect.Height = MinimumSize.Y;

                foreach (var item in ItemSource)
                    AddChild(item);

                Hidden = !IsRootTray;
                base.Construct();
            }

            public override void Layout()
            {
                Root.SafeCall(OnLayout, this);
                var rect = GetDrawableInterior();

                if (AutoSizeColumns)
                {
                    Rect.Height = Parent.Rect.Height;

                    var numItems = ItemSource.Count();

                    var cols = (int)(Math.Ceiling(((float)numItems * ItemSize.Y) / Root.RenderData.VirtualScreen.Height));
                    if (Columns != cols)
                    {
                        Clear();
                        Construct();
                    }
                }

                var pos = new Point(rect.X, rect.Y);
                foreach (var child in EnumerateChildren())
                {
                    child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                    pos.Y += ItemSize.Y;
                    if (pos.Y > rect.Bottom - ItemSize.Y)
                    {
                        pos.Y = rect.Y;
                        pos.X += ItemSize.X;
                    }
                    child.Layout();
                }

                Invalidate();
            }

            public void CollapseTrays()
            {
                foreach (var child in Children)
                    if (child is MenuItem)
                        (child as MenuItem).Unexpand();

                if (!IsRootTray && Parent != null && Parent is MenuItem)
                {
                    Hidden = true;
                    (Parent as MenuItem).CollapseTrays();
                }
            }
        }

        public class MenuItem : Widget
        {
            public Widget ExpansionChild;

            public MenuItem()
            {
                Background = new TileReference("basic", 0);
                //ChangeColorOnHover = true;
                TextColor = Color.Black.ToVector4();
                TextHorizontalAlign = HorizontalAlign.Left;
                TextVerticalAlign = VerticalAlign.Center;
                Border = "border-thin";

                OnMouseEnter = (sender, args) =>
                {
                    foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is MenuItem).SelectMany(c => c.EnumerateChildren()))
                    {
                        if (!Object.ReferenceEquals(child, ExpansionChild))
                        {
                            child.Hidden = true;
                            child.Invalidate();
                        }
                    }

                    if (ExpansionChild != null && ExpansionChild.Hidden)
                    {
                        ExpansionChild.Hidden = false;
                        Root.SafeCall(ExpansionChild.OnShown, ExpansionChild);
                        ExpansionChild.BringToFront();
                        ExpansionChild.Invalidate();
                    }
                };

                OnLayout = (sender) =>
                {
                    if (ExpansionChild != null)
                    {
                        var midPoint = sender.Rect.Y + (sender.Rect.Height / 2);
                            ExpansionChild.Rect.Y = midPoint - (ExpansionChild.Rect.Height / 2);
                            ExpansionChild.Rect.X = sender.Rect.Right;

                            if (ExpansionChild.Rect.Y < Parent.Rect.Y)
                                ExpansionChild.Rect.Y = Parent.Rect.Y;

                            if (ExpansionChild.Rect.Bottom > Root.RenderData.VirtualScreen.Bottom)
                                ExpansionChild.Rect.Y = Root.RenderData.VirtualScreen.Bottom - ExpansionChild.Rect.Height;
                    }
                };
            }

            public override void Construct()
            {
                base.Construct();

                if (ExpansionChild != null)
                {
                    AddChild(ExpansionChild);
                    ExpansionChild.Hidden = true;
                }

                if (OnClick != null)
                {
                    var lambdaOnClick = OnClick;
                    OnClick = (sender, args) =>
                    {
                        Root.SafeCall(lambdaOnClick, sender, args);
                        CollapseTrays();
                    };
                }
            }

            public void CollapseTrays()
            {
                if (Parent != null && Parent is Tray)
                    (Parent as Tray).CollapseTrays();
            }

            public void Unexpand()
            {
                if (ExpansionChild != null)
                {
                    ExpansionChild.Hidden = true;
                    ExpansionChild.Invalidate();
                }
            }

            public void Expand()
            {
                if (ExpansionChild != null)
                {
                    ExpansionChild.Hidden = false;
                    ExpansionChild.Invalidate();
                }
            }
        }

        public class CheckboxMenuItem : MenuItem
        {
            public Action<bool> SetCallback = null;
            public bool InitialState = false;

            public override void Construct()
            {
                base.Construct();

                OnMouseEnter = null;

                AddChild(new CheckBox
                {
                    Text = this.Text,
                    Background = new TileReference("basic", 0),
                    ChangeColorOnHover = true,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    AutoLayout = AutoLayout.DockFill,
                    CheckState = InitialState,
                    OnCheckStateChange = (sender) =>
                    {
                        if (SetCallback != null)
                            SetCallback((sender as CheckBox).CheckState);
                    }
                });
            }
        }

        public class UpdatingMenuItem : MenuItem
        {
            public Action<Widget> OnUpdateMenuItem;

            public override void Construct()
            {
                Root.RegisterForUpdate(this);

                OnUpdate = (sender, time) =>
                {
                    Root.SafeCall(OnUpdateMenuItem, this);
                };
            }
        }
    }
}
