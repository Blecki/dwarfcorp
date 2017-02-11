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
        public static Widget CreateTray(Root Root, IEnumerable<Widget> Icons)
        {
            return Root.ConstructWidget(new NewGui.IconTray
            {
                SizeToGrid = new Point(Icons.Count(), 1),
                Corners = Scale9Corners.Top | Scale9Corners.Right | Scale9Corners.Left,
                ItemSource = Icons,
                Hidden = true,
            });
        }

        public static Widget CreateExpandingIcon(Root Root, TileReference Icon, Widget Child)
        {
            var r = new FramedIcon
            {
                Icon = Icon,
                OnHover = (sender) =>
                {
                    foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is FramedIcon)
                    .SelectMany(c => c.EnumerateChildren()))
                    {
                        child.Hidden = true;
                        child.Invalidate();
                    }

                    if (Child != null)
                    {
                        Child.Hidden = false;
                        Child.Invalidate();
                    }
                },
                OnLayout = (sender) =>
                {
                    if (Child != null)
                    {
                        var midPoint = sender.Rect.X + (sender.Rect.Width / 2);
                        Child.Rect.X = midPoint - (Child.Rect.Width / 2);
                        Child.Rect.Y = sender.Rect.Y - Child.Rect.Height;
                    }
                }
            };

            Root.ConstructWidget(r);

            if (Child != null)
            {
                r.AddChild(Child);
                Child.Hidden = true;
            }

            return r;
        }


        public static Widget CreateLeafButton(Root Root, TileReference Icon, Widget Child, Action<Widget, InputEventArgs> OnClick)
        {
            var r = new FramedIcon
            {
                Icon = Icon,
                OnClick = (sender, args) =>
                {
                    Root.SafeCall(OnClick, sender, args);
                },
                OnMouseEnter = (sender, args) =>
                {
                    foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is FramedIcon)
                    .SelectMany(c => c.EnumerateChildren()))
                    {
                        child.Hidden = true;
                        child.Invalidate();
                    }

                    if (Child != null)
                    {
                        Child.Hidden = false;
                        Child.Invalidate();
                    }
                },
                OnMouseLeave = (sender, args) =>
                {
                    if (Child != null)
                    {
                        Child.Hidden = true;
                        Child.Invalidate();
                    }
                },
                OnLayout = (sender) =>
                {
                    if (Child != null)
                    {
                        var midPoint = sender.Rect.X + (sender.Rect.Width / 2);
                        Child.Rect.X = midPoint - (Child.Rect.Width / 2);
                        Child.Rect.Y = sender.Rect.Y - Child.Rect.Height;
                    }
                }
            };

            Root.ConstructWidget(r);

            if (Child != null)
            {
                r.AddChild(Child);
                Child.Hidden = true;
            }

            return r;
        }
    }
}
