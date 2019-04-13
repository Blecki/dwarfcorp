using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
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
        private Gui.Widgets.EditableTextField TextField;
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
                Font = "font16",
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

            buttons.AddChild(new Gui.Widget
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
                    sender.BackgroundColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                    sender.Invalidate();
                },
                OnMouseLeave = (sender, args) =>
                {
                    sender.BackgroundColor = Vector4.One;
                    sender.Invalidate();
                }
            });

            buttons.AddChild(new Gui.Widget
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
                    sender.BackgroundColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                    sender.Invalidate();
                },
                OnMouseLeave = (sender, args) =>
                {
                    sender.BackgroundColor = Vector4.One;
                    sender.Invalidate();
                }
            });

            TextField = AddChild(new Gui.Widgets.EditableTextField
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "font16",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "",
                ArrowKeyUpDown = (sender, args) =>
                {
                    CurrentValue = Math.Min(Math.Max(CurrentValue + args, 0), MaximumValue);   
                    sender.Invalidate();
                },
                BeforeTextChange = (sender, args) =>
                {
                    if (args.NewText == "")
                    {
                        args.NewText = "";
                        _currentValue = 0;
                        args.Cancelled = false;
                        return;
                    }
                    int newValue;
                    if (Int32.TryParse(args.NewText, out newValue))
                    {
                        if (newValue < 0) newValue = 0;
                        if (newValue > MaximumValue) newValue = MaximumValue;
                        _currentValue = newValue;
                        if (newValue == 0)
                        {
                            args.NewText = "";
                        }
                        else
                        {
                            args.NewText = newValue.ToString();
                        }
                        args.Cancelled = false;
                        return;
                    }
                    args.Cancelled = true;
                },
                OnTextChange = (sender) =>
                {
                    Root.SafeCall(OnValueChanged, this);
                    sender.Invalidate();
                }
            }) as Gui.Widgets.EditableTextField;

        }
    }
}
