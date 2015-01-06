using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    /// <summary>
    /// This componenet, when clicked, toggles on and off. It also has a 
    /// label drawn next to it.
    /// </summary>
    public class Checkbox : GUIComponent
    {
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public Color StrokeColor { get; set; }
        public Color HoverTextColor { get; set; }
        public SpriteFont TextFont { get; set; }
        public bool Checked { get; set; }

        public delegate void CheckModified(bool arg);

        public event CheckModified OnCheckModified;

        public Checkbox(DwarfGUI gui, GUIComponent parent, string text, SpriteFont textFont, bool check) :
            base(gui, parent)
        {
            Text = text;
            TextColor = gui.DefaultTextColor;
            TextFont = textFont;
            OnClicked += Clicked;
            StrokeColor = new Color(0, 0, 0, 0);
            Checked = check;
            HoverTextColor = Color.DarkRed;
            OnCheckModified += CheckBox_OnCheckModified;
        }

        private void CheckBox_OnCheckModified(bool arg)
        {
        }


        public void Clicked()
        {
            Checked = !Checked;
            OnCheckModified.Invoke(Checked);
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            Rectangle globalBounds = GlobalBounds;

            Color c = TextColor;

            if(IsMouseOver)
            {
                c = HoverTextColor;
            }

            Rectangle checkboxBounds = new Rectangle(GlobalBounds.Right - 32, GlobalBounds.Top + 1, 32, 32);

            GUI.Skin.RenderCheckbox(checkboxBounds, Checked, batch);
            Vector2 measure = Datastructures.SafeMeasure(GUI.DefaultFont, Text);


            Drawer2D.DrawStrokedText(batch, Text,
                GUI.DefaultFont,
                new Vector2(GlobalBounds.Right - measure.X - 32, GlobalBounds.Top + 5),
                c, StrokeColor);


            base.Render(time, batch);
        }
    }

}