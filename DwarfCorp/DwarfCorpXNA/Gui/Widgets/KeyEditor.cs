using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.Gui.Widgets
{
    public class KeyEditor : Widget
    {
        public KeyManager KeyManager;
        public VerticalScrollBar ScrollBar;
        private List<Keys> ReservedKeys;
        private List<Widget> Rows = new List<Widget>();

        public override void Construct()
        {
            base.Construct();

            ReservedKeys = new Keys[]
            {
                Keys.Up,
                Keys.Left,
                Keys.Right,
                Keys.Down,
                Keys.LeftControl,
                //Keys.LeftShift,
                //Keys.RightShift,
                Keys.LeftAlt,
                Keys.RightAlt,
                Keys.RightControl,
                Keys.Escape
            }.ToList();

            var bindingCount = KeyManager.Buttons.Count;
            var insideScrollHandler = false;

            ScrollBar = AddChild(new VerticalScrollBar
            {
                ScrollArea = bindingCount,
                ScrollPosition = 0,
                AutoLayout = AutoLayout.DockRight,
                OnScrollValueChanged = (sender) =>
                {
                    if (insideScrollHandler) return;

                    insideScrollHandler = true;
                    LayoutRows();
                    insideScrollHandler = false;
                }
            }) as VerticalScrollBar;

            OnLayout += (sender) =>
            {
                var height = sender.Rect.Height;
                var rowsVisible = height / 20;
                ScrollBar.ScrollArea = Math.Max(0, bindingCount - rowsVisible);
            };

            foreach (var binding in KeyManager.Buttons)
            {
                var lambdaBinding = binding;

                var row = AddChild(new Widget
                {
                    MinimumSize = new Point(0, 20),
                    AutoLayout = AutoLayout.None,
                });

                Rows.Add(row);

                row.AddChild(new Widget
                {
                    MinimumSize = new Point(128, 0),
                    AutoLayout = AutoLayout.DockLeft,
                    Text = binding.Key
                });

                row.AddChild(new Gui.Widgets.KeyField
                {
                    AssignedKey = binding.Value,
                    BeforeKeyChange = (sender, args) =>
                    {
                        if (KeyManager.IsMapped(args.NewKey))
                        {
                            args.Cancelled = true;
                            Root.ShowTooltip(new Point(sender.Rect.X, sender.Rect.Y + 20),
                                String.Format("Key {0} is already assigned.", args.NewKey));
                        }
                        else if (ReservedKeys.Contains(args.NewKey))
                        {
                            args.Cancelled = true;
                            Root.ShowTooltip(new Point(sender.Rect.X, sender.Rect.Y + 20),
                                String.Format("Key {0} is reserved.", args.NewKey));
                        }
                    },
                    OnKeyChange = (sender, newKey) =>
                    {
                        KeyManager[lambdaBinding.Key] = newKey;
                        KeyManager.SaveConfigSettings();
                    },
                    AutoLayout = AutoLayout.DockFill
                });

            }

            LayoutRows();
        }

        private void LayoutRows()
        {
            var startRow = ScrollBar.ScrollPosition;

            foreach (var row in Rows)
            {
                row.AutoLayout = AutoLayout.FloatTopLeft;
                row.MinimumSize = new Point(0, 0);
                row.Rect = new Rectangle(0, 0, 0, 0);
                row.Hidden = true;
            }

            foreach (var row in Rows.Skip(startRow).Take(GetDrawableInterior().Height / 20))
            {
                row.AutoLayout = AutoLayout.DockTop;
                row.MinimumSize = new Point(0, 20);
                row.Hidden = false;
            }

            Layout();
        }
    }
}
