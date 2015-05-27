using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This GUI component draws a filled bar specifying a certain value.
    /// </summary>
    public class ProgressBar : GUIComponent
    {
        public float Value { get; set; }
        public Color Tint { get; set; }
        public string Message { get; set; }
        public Color MessageColor { get; set; }
        public Color MessageStroke { get; set; }

        public ProgressBar(DwarfGUI gui, GUIComponent parent, float v) :
            base(gui, parent)
        {
            Value = v;
            Tint = Color.Lime;
            Message = "";
            MessageColor = Color.White;
            MessageStroke = Color.Black;
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if (!IsVisible) return;
            Rectangle rectToDraw = new Rectangle(GlobalBounds.X, GlobalBounds.Y + GlobalBounds.Height / 2 - GUI.Skin.TileHeight / 2, GlobalBounds.Width, GUI.Skin.TileHeight);
            GUI.Skin.RenderProgressBar(rectToDraw, Value, Tint, batch);
            Drawer2D.DrawAlignedStrokedText(batch, Message, GUI.DefaultFont, MessageColor, MessageStroke, Drawer2D.Alignment.Center, GlobalBounds);
            base.Render(time, batch);
        }

        public override bool IsMouseOverRecursive()
        {
            if (!IsVisible) return false;
            else return base.IsMouseOverRecursive();
        }
    }

}