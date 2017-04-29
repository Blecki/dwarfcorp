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
                if (ItemSource != null)
                {
                    SizeToGrid = new Point(ItemSource.Count(), 1);
                }
                Corners = Scale9Corners.Top | Scale9Corners.Right | Scale9Corners.Left;
                Hidden = !IsRootTray;
                base.Construct();
            }

            public void CollapseTrays()
            {
                foreach (var child in Children)
                    if (child is Icon)
                        (child as Icon).Unexpand();
                
                if (!IsRootTray && Parent != null && Parent is Icon)
                {
                    Hidden = true;
                    (Parent as Icon).CollapseTrays();
                }
            }

            public Tray FindTopTray()
            {
                foreach (var child in Children)
                {
                    var icon = child as Icon;
                    if (icon == null) continue;
                    var nestedTray = icon.ExpansionChild as Tray;
                    if (nestedTray != null && nestedTray.Hidden == false)
                        return nestedTray.FindTopTray();
                }

                return this;
            }

            public void Hotkey(int Key)
            {
                if (Key < 0 || Key >= Children.Count) return;
                var icon = GetChild(Key) as Icon;
                if (icon == null) return;

                if (icon.ExpansionChild is Tray)
                    Root.SafeCall(icon.OnMouseEnter, icon, new InputEventArgs
                    {
                        X = icon.Rect.X,
                        Y = icon.Rect.Y
                    });
                else
                {
                    Root.SafeCall(icon.OnClick, icon, new InputEventArgs
                    {
                        X = icon.Rect.X,
                        Y = icon.Rect.Y
                    });
                    CollapseTrays();
                }                
            }
        }

        public class Icon : FramedIcon
        {
            public Widget ExpansionChild;
            public bool KeepChildVisible = false;
            public bool ExpandChildWhenDisabled = false;

            public enum ExpansionDirections
            {
                Up,
                Left
            }

            public ExpansionDirections ExpansionDirection = ExpansionDirections.Up;

            public Icon()
            {
                OnMouseEnter = (sender, args) =>
                {
                    foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is FramedIcon)
                    .SelectMany(c => c.EnumerateChildren()))
                    {
                        if (!Object.ReferenceEquals(child, ExpansionChild))
                        {
                            child.Hidden = true;
                            child.Invalidate();
                        }
                    }

                    if (ExpansionChild != null && (ExpandChildWhenDisabled || (sender as FramedIcon).Enabled) && ExpansionChild.Hidden)
                    {
                        ExpansionChild.Hidden = false;
                        Root.SafeCall(ExpansionChild.OnShown, ExpansionChild);
                        ExpansionChild.Invalidate();
                    }
                };

                OnMouseLeave = (sender, args) =>
                {
                    if (!KeepChildVisible && ExpansionChild != null)
                        Unexpand();
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
                        if (ExpansionDirection == ExpansionDirections.Up)
                        {
                            var midPoint = sender.Rect.X + (sender.Rect.Width / 2);
                            ExpansionChild.Rect.X = midPoint - (ExpansionChild.Rect.Width / 2);
                            ExpansionChild.Rect.Y = sender.Rect.Y - ExpansionChild.Rect.Height;

                            if (ExpansionChild.Rect.X < Parent.Rect.X)
                                ExpansionChild.Rect.X = Parent.Rect.X;

                            if (ExpansionChild.Rect.Right > Root.VirtualScreen.Right)
                                ExpansionChild.Rect.X = Root.VirtualScreen.Right - ExpansionChild.Rect.Width;
                        }
                        else if (ExpansionDirection == ExpansionDirections.Left)
                        {
                            // Position child to left.



                        }
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
    }
}
