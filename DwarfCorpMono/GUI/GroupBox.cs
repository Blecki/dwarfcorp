using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace DwarfCorp
{
    public class GroupBox : SillyGUIComponent
    {
        public string Title { get; set; }
        public bool DrawBounds { get; set; }

        public GroupBox(SillyGUI gui, SillyGUIComponent parent, string title) :
            base(gui, parent)
        {
            Title = title;
            DrawBounds = true;
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            if (!IsVisible)
            {
                return;
            }

            if (DrawBounds)
            {
                GUI.Skin.RenderGroup(GlobalBounds, batch);
            }
            Drawer2D.DrawAlignedText(batch, Title, GUI.DefaultFont, Color.Black, Drawer2D.Alignment.Top | Drawer2D.Alignment.Left, GlobalBounds);
            base.Render(time, batch);
        }

    }
}
