using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class FlatToolTray
    {
        public class RootTray : Widget
        {
            public IEnumerable<Widget> ItemSource;

            public override void Construct()
            {
                if (ItemSource != null)
                    foreach (var child in ItemSource)
                    {
                        AddChild(child);
                        child.Hidden = true;
                    };

                base.Construct();
            }

            public void SwitchTray(Widget NewTray)
            {
                foreach (var child in Children)
                    child.Hidden = true;
                NewTray.Hidden = false;
                Root.SafeCall(NewTray.OnShown, NewTray);
                Invalidate();
            }

            public override void Layout()
            {
                Root.SafeCall(OnLayout, this);

                foreach (var child in Children)
                {
                    child.Rect.X = Rect.X;
                    child.Rect.Y = Rect.Y;
                    child.Layout();
                }
            }
        }

        public class Tray : IconTray
        {
            public override void Construct()
            {
                if (ItemSource != null)
                {
                    SizeToGrid = new Point(ItemSource.Count(), 1);
                }
                Corners = Scale9Corners.Top | Scale9Corners.Right | Scale9Corners.Left;
                Hidden = true;
                base.Construct();
            }
                        
            public void Hotkey(int Key)
            {
                if (Key < 0 || Key >= Children.Count) return;
                var icon = GetChild(Key) as Icon;
                if (icon == null) return;

                Root.SafeCall(icon.OnClick, icon, new InputEventArgs
                {
                    X = icon.Rect.X,
                    Y = icon.Rect.Y
                });
            }
        }

        public class Icon : FramedIcon
        {
            public Widget PopupChild;
            public Widget ReplacementMenu;
            public bool KeepChildVisible = false;
            public bool ExpandChildWhenDisabled = false;
            
            public void Expand(Widget sender, InputEventArgs args)
            {
                foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is FramedIcon)
.                       SelectMany(c => c.EnumerateChildren()))
                {
                    if (!Object.ReferenceEquals(child, PopupChild))
                    {
                        child.Hidden = true;
                        child.Invalidate();
                    }
                }

                if (PopupChild != null && (ExpandChildWhenDisabled || (sender as FramedIcon).Enabled) && PopupChild.Hidden)
                {
                    PopupChild.Hidden = false;
                    Root.SafeCall(PopupChild.OnShown, PopupChild);
                    PopupChild.Invalidate();
                }
            }

            public Icon()
            {
  
            }

            public override void Construct()
            {
                    OnMouseEnter = Expand;

                    OnMouseLeave = (sender, args) =>
                    {
                        if (!KeepChildVisible && PopupChild != null)
                            Unexpand();
                    };
                

                OnDisable = (sender) =>
                {
                    if (PopupChild != null)
                    {
                        if (ExpandChildWhenDisabled && PopupChild.Hidden == false)
                            return;

                        PopupChild.Hidden = true;
                        PopupChild.Invalidate();
                    }
                };

                OnLayout = (sender) =>
                {
                    if (PopupChild != null)
                    {
                        var midPoint = sender.Rect.X + (sender.Rect.Width / 2);
                        PopupChild.Rect.X = midPoint - (PopupChild.Rect.Width / 2);
                        PopupChild.Rect.Y = sender.Rect.Y - PopupChild.Rect.Height;

                        if (PopupChild.Rect.X < Parent.Rect.X)
                            PopupChild.Rect.X = Parent.Rect.X;

                        if (PopupChild.Rect.Right > Root.RenderData.VirtualScreen.Right)
                            PopupChild.Rect.X = Root.RenderData.VirtualScreen.Right - PopupChild.Rect.Width;
                    }                };

                base.Construct();

                if (PopupChild != null)
                {
                    AddChild(PopupChild);
                    PopupChild.Hidden = true;
                }

                if (OnClick != null)
                {
                    System.Diagnostics.Debug.Assert(ReplacementMenu == null, "Conflicting menu options");
                }

                if (ReplacementMenu != null)
                    OnClick = (sender, args) =>
                    {
                        var root = Parent.Parent as RootTray;
                        if (root != null) root.SwitchTray(ReplacementMenu);
                    };
            }
            
            public void Unexpand()
            {
                if (PopupChild != null)
                {
                    PopupChild.Hidden = true;
                    PopupChild.Invalidate();
                    Hidden = false;
                }
            }

            public void Expand()
            {
                if (PopupChild != null)
                {
                    PopupChild.Hidden = false;
                    PopupChild.Invalidate();
                    Hidden = true;
                }
            }
        }
    }
}
