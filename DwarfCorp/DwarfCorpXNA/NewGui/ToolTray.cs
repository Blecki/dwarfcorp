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
            public override void Construct()
            {
                SizeToGrid = new Point(ItemSource.Count(), 1);
                Corners = Scale9Corners.Top | Scale9Corners.Right | Scale9Corners.Left;
                Hidden = true;
                base.Construct();
            }
        }
        
        public class ExpandingIcon : FramedIcon
        {
            public Widget ExpansionChild;

            public ExpandingIcon()
            {
                OnHover = (sender) =>
                {
                    foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is FramedIcon)
                    .SelectMany(c => c.EnumerateChildren()))
                    {
                        child.Hidden = true;
                        child.Invalidate();
                    }

                    if (ExpansionChild != null && (sender as FramedIcon).Enabled)
                    {
                        ExpansionChild.Hidden = false;
                        ExpansionChild.Invalidate();
                    }
                };

                OnDisable = (sender) =>
                {
                    if (ExpansionChild != null)
                    {
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
            }
        }

        public class LeafIcon : FramedIcon
        {
            public Widget ExpansionChild;

            public LeafIcon()
            {
                OnMouseEnter = (sender, args) =>
                {
                    foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is FramedIcon)
                    .SelectMany(c => c.EnumerateChildren()))
                    {
                        child.Hidden = true;
                        child.Invalidate();
                    }

                    if (ExpansionChild != null && (sender as FramedIcon).Enabled)
                    {
                        ExpansionChild.Hidden = false;
                        ExpansionChild.Invalidate();
                    }
                };

                OnMouseLeave = (sender, args) =>
                {
                    if (ExpansionChild != null)
                    {
                        ExpansionChild.Hidden = true;
                        ExpansionChild.Invalidate();
                    }
                };

                OnDisable = (sender) =>
                {
                    if (ExpansionChild != null)
                    {
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
            }
        }
    }
}
