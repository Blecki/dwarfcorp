using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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
                if (NewTray != null && !Children.Contains(NewTray))
                {
                    AddChild(NewTray);
                    Layout();
                }
                foreach (var child in Children)
                {
                    child.Hidden = true;
                    foreach (var subchild in child.EnumerateChildren().Where(c => c is FramedIcon)
                                .SelectMany(c => c.EnumerateChildren()))
                    {
                        subchild.Hidden = true;
                        subchild.Invalidate();
                    }
                }

                NewTray.Hidden = false;
                RefreshVisibleTray();
            }

            private Widget CurrentTray => Children.FirstOrDefault(c => c.Hidden == false);

            public void RefreshVisibleTray()
            {
                var tray = CurrentTray as IconTray;
                if (tray != null)
                    Root.SafeCall(tray.OnRefresh, tray);

                Invalidate();
            }

            public override void Layout()
            {
                Root.SafeCall(OnLayout, this);

                foreach (var child in Children)
                {
                    child.Rect = Rect;
                    child.Layout();
                }
            }
        }

        public class Tray : IconTray
        {
            public static List<Keys> Hotkeys = null;

            public static void DetectHotKeys()
            {
                Hotkeys = new List<Keys>()
                {
                    Keys.D1,
                    Keys.D2,
                    Keys.D3,
                    Keys.D4,
                    Keys.D5,
                    Keys.D6,
                    Keys.D7,
                    Keys.D8,
                    Keys.D9,
                    Keys.D0
                };

                List<Keys> extraKeys = new List<Keys>()
                {
                    Keys.Q,
                    Keys.W,
                    Keys.E,
                    Keys.R,
                    Keys.T,
                    Keys.Y,
                    Keys.U,
                    Keys.I,
                    Keys.O,
                    Keys.P,
                    Keys.A,
                    Keys.S,
                    Keys.D,
                    Keys.F,
                    Keys.G,
                    Keys.H,
                    Keys.J,
                    Keys.K,
                    Keys.L,
                    Keys.Z,
                    Keys.X,
                    Keys.C,
                    Keys.V,
                    Keys.B,
                    Keys.N,
                    Keys.M,
                    Keys.OemComma,
                    Keys.OemPeriod,
                    Keys.OemBackslash,
                    Keys.OemMinus,
                    Keys.OemPlus,
                    Keys.OemTilde,
                    Keys.Insert,
                    Keys.Home,
                    Keys.Delete,
                    Keys.End
                };

                foreach(var key in extraKeys)
                {
                    if (!ControlSettings.Mappings.Contains(key))
                    {
                        Hotkeys.Add(key);
                    }
                }

            }

            public override void Construct()
            {
                if (ItemSource != null)
                {
                    SizeToGrid = new Point(ItemSource.Count(), 1);
                }
                Corners = 0; // Scale9Corners.Top | Scale9Corners.Right;
                Hidden = true;
                Transparent = true;
                HotKeys = true;

                base.Construct();
            }

            public void Hotkey(Keys Key)
            {
                int idx = Hotkeys.IndexOf(Key);
                if (idx < 0 || idx >= Children.Count) return;
                var icon = Children.FirstOrDefault(c => (c as FramedIcon).HotkeyValue == Key);
                if (icon == null) return;

                Root.SafeCall(icon.OnClick, icon, new InputEventArgs
                {
                    X = icon.Rect.X,
                    Y = icon.Rect.Y
                });
            }
        }

        public enum IconBehavior
        {
            LeafIcon,
            ShowSubMenu,
            ShowClickPopup,
            ShowHoverPopup,
            ShowClickPopupAndLeafIcon,
        }

        public class Icon : FramedIcon
        {
            public IconBehavior Behavior = IconBehavior.ShowSubMenu;
            public Widget PopupChild;
            public Widget ReplacementMenu;
            public bool KeepChildVisible = false;
            public bool ExpandChildWhenDisabled = false;
            
            public void ExpandPopup(Widget sender, InputEventArgs args)
            {
                foreach (var child in sender.Parent.EnumerateChildren().Where(c => c is FramedIcon)
.                       SelectMany(c => c.EnumerateChildren()))
                {
                    if (!Object.ReferenceEquals(child, PopupChild) && !child.Hidden)
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
                WrapWithinWords = true;
                OnDisable += (sender) =>
                {
                    if (PopupChild != null)
                    {
                        if (ExpandChildWhenDisabled && PopupChild.Hidden == false)
                            return;

                        PopupChild.Hidden = true;
                        PopupChild.Invalidate();
                    }
                };

                OnLayout += (sender) =>
                {
                    if (PopupChild != null)
                    {
                        var midPoint = sender.Rect.X + (sender.Rect.Width / 2);
                        PopupChild.Rect.X = midPoint - (PopupChild.Rect.Width / 2);
                        PopupChild.Rect.Y = Parent.Rect.Y - PopupChild.Rect.Height;

                        if (PopupChild.Rect.X < Parent.Rect.X)
                            PopupChild.Rect.X = Parent.Rect.X;

                        if (PopupChild.Rect.Right > Root.RenderData.VirtualScreen.Right)
                            PopupChild.Rect.X = Root.RenderData.VirtualScreen.Right - PopupChild.Rect.Width;
                    }             
                };

                base.Construct();

                if (PopupChild != null && ReplacementMenu != null)
                    throw new InvalidProgramException("Conflicting icon behavior");

                switch (Behavior)
                {
                    case IconBehavior.ShowClickPopupAndLeafIcon:
                        if (PopupChild == null)
                            throw new InvalidProgramException("Conflicting icon behavior");
                        AddChild(PopupChild);
                        PopupChild.Hidden = true;
                        OnClick += ExpandPopup;
                        break;
                    case IconBehavior.ShowClickPopup:
                        if (PopupChild == null || OnClick != null)
                            throw new InvalidProgramException("Conflicting icon behavior");
                        AddChild(PopupChild);
                        PopupChild.Hidden = true;
                        OnClick += ExpandPopup;
                        break;
                    case IconBehavior.LeafIcon:
                    case IconBehavior.ShowHoverPopup:
                        if (PopupChild == null && Behavior == IconBehavior.ShowHoverPopup) throw new InvalidProgramException("Conflicting icon behavior");
                        if (PopupChild != null)
                        {
                            AddChild(PopupChild);
                            PopupChild.Hidden = true;
                            OnMouseEnter += ExpandPopup;
                            OnMouseLeave += (sender, args) => { if (PopupChild != null) { PopupChild.Hidden = true; } };
                        }
                        break;
                    case IconBehavior.ShowSubMenu:
                        OnClick += (sender, args) =>
                        {
                            var root = Parent.Parent as RootTray;
                            if (root != null) root.SwitchTray(ReplacementMenu);
                        };
                        break;
                }

                if (OnUpdate != null)
                {
                    Root.RegisterForUpdate(this);
                }

                WrapText = true;
            }
        }
    }
}
