using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This is a GUI component which merely draws text.
    /// </summary>
    public class Label : GUIComponent
    {
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public Color StrokeColor { get; set; }
        public SpriteFont TextFont { get; set; }
        public Drawer2D.Alignment Alignment { get; set; }
        public bool WordWrap { get; set; }

        public Label(DwarfGUI gui, GUIComponent parent, string text, SpriteFont textFont) :
            base(gui, parent)
        {
            Text = text;
            TextColor = gui.DefaultTextColor;
            StrokeColor = gui.DefaultStrokeColor;
            TextFont = textFont;
            Alignment = Drawer2D.Alignment.Left;
            WordWrap = false;
        }

      

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            string text = Text;

            if(WordWrap)
            {
                text = DwarfGUI.WrapLines(Text, LocalBounds, TextFont);
            }

            Drawer2D.DrawAlignedStrokedText(batch, text, TextFont, TextColor, StrokeColor, Alignment, GlobalBounds);
            base.Render(time, batch);
        }
    }

    public class DynamicLabel : Label 
    {
        public Func<float> ValueFn;
        public string Prefix;
        public string Postfix;
        public string Format;
        public float LastValue = default(float);
        public DynamicLabel(DwarfGUI gui, GUIComponent parent, string prefixText, string postfix, SpriteFont textFont, string format, Func<float> valuefn)
            : base(gui, parent, prefixText, textFont)
        {
            ValueFn = valuefn;
            Prefix = prefixText;
            Postfix = postfix;
            Format = format;
        }

        public override void Update(DwarfTime time)
        {
            if (ValueFn != null)
            {
                float value = ValueFn();

                if (value.CompareTo(LastValue) != 0)
                {
                    string operand = "-";
                    Color color = Color.Red;
                    if (value.CompareTo(LastValue) > 0)
                    {
                        operand = "+";
                        color = Color.Green;
                    }

                    IndicatorManager.DrawIndicator(operand + (value - LastValue).ToString(Format) + Postfix,
                        new Vector3(GlobalBounds.Center.X, GlobalBounds.Center.Y, 0), 1.0f, color, Indicator.IndicatorMode.Indicator2D);
                    LastValue = value;

                    Text = Prefix + value.ToString(Format) + Postfix;
                }
            }
            base.Update(time);
        }
    }

}