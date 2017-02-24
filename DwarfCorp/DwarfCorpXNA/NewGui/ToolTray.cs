using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class ToolTray
    {
        public class Tray : IconTray
        {
            public bool IsRootTray = false;

            public override void Construct()
            {
                SizeToGrid = new Point(ItemSource.Count(), 1);
                Corners = Scale9Corners.Top | Scale9Corners.Right | Scale9Corners.Left;
                Hidden = !IsRootTray;
                base.Construct();
            }

            public void CollapseTrays()
            {
                if (IsRootTray)
                {
                    foreach (var child in Children)
                        if (child is Icon)
                            (child as Icon).Unexpand();
                    return;
                }

                Hidden = true;

                if (Parent != null && Parent is Icon)
                {
                    (Parent as Icon).CollapseTrays();
                }
            }
        }

        public class Icon : FramedIcon
        {
            public Widget ExpansionChild;
            public bool KeepChildVisible = false;
            public bool ExpandChildWhenDisabled = false;

            public Icon()
            {
                OnMouseEnter = (sender, args) =>
                {
                    foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is FramedIcon)
                    .SelectMany(c => c.EnumerateChildren()))
                    {
                        child.Hidden = true;
                        child.Invalidate();
                    }

                    if (ExpansionChild != null && (ExpandChildWhenDisabled || (sender as FramedIcon).Enabled))
                    {
                        ExpansionChild.Hidden = false;
                        Root.SafeCall(ExpansionChild.OnShown, ExpansionChild);
                        ExpansionChild.Invalidate();
                    }
                };

                OnMouseLeave = (sender, args) =>
                {
                    if (!KeepChildVisible && ExpansionChild != null)
                    {
                        ExpansionChild.Hidden = true;
                        ExpansionChild.Invalidate();
                    }
                };

                OnDisable = (sender) =>
                {
                    if (ExpansionChild != null)
                    {
                        if (ExpandChildWhenDisabled && ExpansionChild.Hidden == false)
                            return;

                        ExpansionChild.Hidden = true;
                        ExpansionChild.Invalidate();
                    }
                };

                OnLayout = (sender) =>
                {
                    if (ExpansionChild != null)
                    {
                        var midPoint = sender.Rect.X + (sender.Rect.Width / 2);
                        ExpansionChild.Rect.X = midPoint - (ExpansionChild.Rect.Width / 2);
                        ExpansionChild.Rect.Y = sender.Rect.Y - ExpansionChild.Rect.Height;

                        if (ExpansionChild.Rect.X < Parent.Rect.X)
                            ExpansionChild.Rect.X = Parent.Rect.X;
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
        }
    }
}
