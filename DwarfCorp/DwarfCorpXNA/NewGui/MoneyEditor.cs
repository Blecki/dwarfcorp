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
                    TextField.TextColor = new Vector4(1, 1, 1, 1);
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
            AddChild(new Gum.Widget
            {
                Background = new TileReference("round-buttons", 7),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = AutoLayout.DockLeft,
                OnClick = (sender, args) =>
                {
                    if (CurrentValue > 0)
                        CurrentValue = CurrentValue - 1;
                }
            });

            AddChild(new Gum.Widget
            {
                Background = new TileReference("round-buttons", 3),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    if (CurrentValue < MaximumValue)
                        CurrentValue = CurrentValue + 1;
                }
            });

            TextField = AddChild(new Gum.Widgets.EditableTextField
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "outline-font",
                TextColor = new Vector4(1, 1, 1, 1),
                Text = "0",
                HiliteOnMouseOver = false,
                BeforeTextChange = (sender, args) =>
                {
                    sender.TextColor = new Vector4(1, 0, 0, 1);
                    Valid = false;

                    int newValue;
                    if (Int32.TryParse(args.NewText, out newValue))
                    {
                        if (newValue < 0) newValue = 0;
                        if (newValue > MaximumValue) newValue = MaximumValue;
                        _currentValue = newValue;
                        sender.TextColor = new Vector4(1, 1, 1, 1);
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
