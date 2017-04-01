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
                (Children[i] as FramedIcon).Tint = i == 0 ? ToggledTint : OffTint;
                int i1 = i;
                Children[i].OnMouseEnter += (widget, args) =>
                {
                    (widget as FramedIcon).Tint = HoverTint;
                    widget.Invalidate();
                };

                Children[i].OnMouseLeave += (widget, args) =>
                {
                    (widget as FramedIcon).Tint = i1 == SelectedChild ? ToggledTint : OffTint;
                    widget.Invalidate();
                };

                Children[i].OnClick += (widget, args) =>
                {
                    SelectedChild = i1;
                    (widget as FramedIcon).Tint = ToggledTint;

                    for (int j = 0; j < Children.Count; j++)
                    {
                        if (i1 == j) continue;
                        (Children[j] as FramedIcon).Tint = OffTint;
                        Children[j].Invalidate();
                    }
                };
            }

            SelectedChild = 0;
        }
    }
}
