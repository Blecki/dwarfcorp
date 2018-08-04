using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class KeyField : Widget
    {
        private Microsoft.Xna.Framework.Input.Keys _assignedKey;
        public Microsoft.Xna.Framework.Input.Keys AssignedKey
        {
            get { return _assignedKey; }
            set
            {
                _assignedKey = value;
                Text = _assignedKey.ToString();
                Invalidate();
            }
        }

        public class BeforeKeyChangeEventArgs
        {
            public Microsoft.Xna.Framework.Input.Keys NewKey;
            public bool Cancelled;
        }

        public Action<Widget, BeforeKeyChangeEventArgs> BeforeKeyChange;
        public Action<Widget, Microsoft.Xna.Framework.Input.Keys> OnKeyChange;

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Border)) Border = "border-thin";

            TextVerticalAlign = VerticalAlign.Center;
            TextHorizontalAlign = HorizontalAlign.Left;

            OnClick += (sender, args) =>
                {
                        Root.SetFocus(this);
                };

            OnGainFocus += (sender) =>
            {
                TextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                this.Invalidate();
            };
            OnLoseFocus += (sender) =>
            {
                TextColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                this.Invalidate();
            };


            OnKeyDown += (sender, args) =>
                {
                    var beforeArgs = new BeforeKeyChangeEventArgs
                    {
                        NewKey = (Microsoft.Xna.Framework.Input.Keys)args.KeyValue,
                        Cancelled = false
                    };

                    Root.SafeCall(BeforeKeyChange, this, beforeArgs);

                    if (!beforeArgs.Cancelled)
                    {
                        AssignedKey = beforeArgs.NewKey;
                        Root.SafeCall(OnKeyChange, this, AssignedKey);
                    }
                };

        }
    }
}
