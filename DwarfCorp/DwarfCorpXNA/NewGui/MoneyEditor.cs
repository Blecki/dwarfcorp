using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class MoneyEditor: Widget
    {
        public int MaximumValue;

        private int _currentValue;
        public int CurrentValue
        {
            get { return _currentValue; }
            set
            {
                _currentValue = value;
                if (TextField != null)
                {
                    TextField.Text = _currentValue.ToString();
                    TextField.TextColor = new Vector4(0, 0, 0, 1);
                    TextField.Invalidate();
                }
                Root.SafeCall(OnValueChanged, this);
            }
        }

        public bool Valid = true;
        private Gum.Widgets.EditableTextField TextField;
        public Action<Widget> OnValueChanged;
        
        public override void Construct()
        {
            AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockLeft,
                Background = new TileReference("coins", 1),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
            });

            AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockLeft,
                Font = "font-hires",
                Text = "$",
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Right,
                MinimumSize = new Point(16, 32),
                MaximumSize = new Point(16, 32)
            });

            var buttons = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(16, 32)
            });

            buttons.AddChild(new Gum.Widget
            {
                Background = new TileReference("round-buttons", 7),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = AutoLayout.DockBottom,
                OnClick = (sender, args) =>
                {
                    if (CurrentValue > 0)
                        CurrentValue = CurrentValue - 1;
                },
                OnMouseEnter = (sender, args) =>
                {
                    sender.BackgroundColor = Color.DarkRed.ToVector4();
                    sender.Invalidate();
                },
                OnMouseLeave = (sender, args) =>
                {
                    sender.BackgroundColor = Vector4.One;
                    sender.Invalidate();
                }
            });

            buttons.AddChild(new Gum.Widget
            {
                Background = new TileReference("round-buttons", 3),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    if (CurrentValue < MaximumValue)
                        CurrentValue = CurrentValue + 1;
                },
                OnMouseEnter = (sender, args) =>
                {
                    sender.BackgroundColor = Color.DarkRed.ToVector4();
                    sender.Invalidate();
                },
                OnMouseLeave = (sender, args) =>
                {
                    sender.BackgroundColor = Vector4.One;
                    sender.Invalidate();
                }
            });

            TextField = AddChild(new Gum.Widgets.EditableTextField
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "font-hires",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "0",
                BeforeTextChange = (sender, args) =>
                {
                    Valid = false;

                    int newValue;
                    if (args.NewText.Length == 0) args.NewText = "0";

                    if (Int32.TryParse(args.NewText, out newValue))
                    {
                        if (newValue < 0) newValue = 0;
                        if (newValue > MaximumValue) newValue = MaximumValue;
                        _currentValue = newValue;
                        Valid = true;
                        args.NewText = newValue.ToString();
                    }
                    else
                        args.Cancelled = true;
                },
                OnTextChange = (sender) =>
                {
                    Root.SafeCall(OnValueChanged, this);
                    sender.Invalidate();
                }
            }) as Gum.Widgets.EditableTextField;

        }
    }
}
