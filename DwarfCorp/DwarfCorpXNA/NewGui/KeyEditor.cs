using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.NewGui
{
    public class KeyEditor : Widget
    {
        public KeyManager KeyManager;
        private List<Keys> ReservedKeys;

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

            foreach (var binding in KeyManager.Buttons)
            {
                var lambdaBinding = binding;

                var row = AddChild(new Widget
                {
                    MinimumSize = new Point(0, 20),
                    AutoLayout = AutoLayout.DockTop
                });

                row.AddChild(new Widget
                {
                    MinimumSize = new Point(128, 0),
                    AutoLayout = AutoLayout.DockLeft,
                    Text = binding.Key
                });

                row.AddChild(new Gum.Widgets.KeyField
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

            Layout();
        }
    }
}
