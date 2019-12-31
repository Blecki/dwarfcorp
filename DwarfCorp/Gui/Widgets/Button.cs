using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class Button : Widget
    {
        private Vector4 previousTextColor = Color.Black.ToVector4();
        public bool Enabled = true;

        public override void Construct()
        {
            TextVerticalAlign = VerticalAlign.Center;
            TextHorizontalAlign = HorizontalAlign.Center;
            
            if (string.IsNullOrEmpty(Border))
                Border = "border-button";
            
            if (Border == "none") // WTF??
                Border = null;
            
            previousTextColor = TextColor;

            OnMouseEnter += (widget, action) =>
            {
                if (Enabled)
                {
                    previousTextColor = TextColor;
                    widget.TextColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                    widget.Invalidate();
                }
            };

            OnMouseLeave += (widget, action) =>
            {
                if (Enabled)
                {
                    widget.TextColor = previousTextColor;
                    widget.Invalidate();
                }
            };
        }

        protected override Mesh Redraw()
        {
            if (Enabled) BackgroundColor = new Vector4(1, 1, 1, 1);
            else BackgroundColor = new Vector4(0, 0, 0, 0.25f);

            return base.Redraw();
        }
    }

    public class ImageButton : Widget
    {
        public override void Construct()
        {
            var color = BackgroundColor;
            OnMouseEnter += (widget, action) =>
            {
                widget.BackgroundColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                widget.Invalidate();
            };

            OnMouseLeave += (widget, action) =>
            {
                widget.BackgroundColor = color;
                widget.Invalidate();
            };
        }
    }
}
