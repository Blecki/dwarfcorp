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
        public override void Construct()
        {
            TextVerticalAlign = VerticalAlign.Center;
            TextHorizontalAlign = HorizontalAlign.Center;
            

            if (string.IsNullOrEmpty(Border))
            {
                Border = "border-button";
            }

            if (Border == "none")
            {
                Border = null;
            }


            previousTextColor = TextColor;
            OnMouseEnter += (widget, action) =>
            {
                previousTextColor = TextColor;
                widget.TextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                widget.Invalidate();
            };

            OnMouseLeave += (widget, action) =>
            {
                widget.TextColor = previousTextColor;
                widget.Invalidate();
            };
        }
    }

    public class ImageButton : Widget
    {
        public override void Construct()
        {
            var color = BackgroundColor;
            OnMouseEnter += (widget, action) =>
            {
                widget.BackgroundColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
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
