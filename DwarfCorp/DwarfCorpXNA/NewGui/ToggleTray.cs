using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class ToggleTray : IconTray
    {
        private int _selectedChild = -1;
        public int SelectedChild { get { return _selectedChild; } private set { _selectedChild = value; } }
        public Vector4 ToggledTint = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public Vector4 OffTint = new Vector4(0.15f, 0.15f, 0.15f, 0.5f);
        public Vector4 HoverTint = new Vector4(0.95f, 0.8f, 0.6f, 1.0f);
        public override void Construct()
        {
            base.Construct();


            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].Tint = i == 0 ? ToggledTint : OffTint;
                int i1 = i;
                Children[i].OnMouseEnter += (widget, args) =>
                {
                    widget.Tint = HoverTint;
                };

                Children[i].OnMouseLeave += (widget, args) =>
                {
                    widget.Tint = i1 == SelectedChild ? ToggledTint : OffTint;
                };

                Children[i].OnClick += (widget, args) =>
                {
                    SelectedChild = i1;
                    widget.Tint = ToggledTint;

                    for (int j = 0; j < Children.Count; j++)
                    {
                        if (i1 == j) continue;
                        Children[j].Tint = OffTint;
                    }
                };
            }

            SelectedChild = 0;
        }
    }
}
