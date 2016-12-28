// Label.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     This is a GUI component which merely draws text.
    /// </summary>
    public class Label : GUIComponent
    {
        public Label(DwarfGUI gui, GUIComponent parent, string text, SpriteFont textFont) :
            base(gui, parent)
        {
            Text = text;
            TextColor = gui.DefaultTextColor;
            StrokeColor = gui.DefaultStrokeColor;
            TextFont = textFont;
            Alignment = Drawer2D.Alignment.Left;
            WordWrap = false;
            Truncate = false;
        }

        public string Text { get; set; }
        public Color TextColor { get; set; }
        public Color StrokeColor { get; set; }
        public SpriteFont TextFont { get; set; }
        public Drawer2D.Alignment Alignment { get; set; }
        public bool WordWrap { get; set; }
        public bool Truncate { get; set; }


        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            string text = Text;

            if (WordWrap)
            {
                text = DwarfGUI.WrapLines(Text, LocalBounds, TextFont);
            }

            if (Truncate)
            {
                Vector2 measure = Datastructures.SafeMeasure(TextFont, text);
                Vector2 wMeasure = Datastructures.SafeMeasure(TextFont, "W");
                if (measure.X > GlobalBounds.Width)
                {
                    int numLetters = GlobalBounds.Width/(int) wMeasure.X;
                    text = Text.Substring(0, Math.Min(numLetters, Text.Length)) + "...";
                }
            }

            if (StrokeColor.A > 0)
            {
                Drawer2D.DrawAlignedStrokedText(batch, text, TextFont, TextColor, StrokeColor, Alignment, GlobalBounds);
            }
            else
            {
                Drawer2D.DrawAlignedText(batch, text, TextFont, TextColor, Alignment, GlobalBounds);
            }
            base.Render(time, batch);
        }

        public void GetLocalBoundsFromText()
        {
            Vector2 measure = Datastructures.SafeMeasure(TextFont, Text);

            LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, (int) measure.X + 4, (int) measure.Y + 4);
        }
    }

    public class DynamicLabel : Label
    {
        public string Format;
        public float LastValue = default(float);
        public string Postfix;
        public string Prefix;
        public Func<float> ValueFn;

        public DynamicLabel(DwarfGUI gui, GUIComponent parent, string prefixText, string postfix, SpriteFont textFont,
            string format, Func<float> valuefn)
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
                        new Vector3(GlobalBounds.Center.X, GlobalBounds.Center.Y, 0), 1.0f, color,
                        Indicator.IndicatorMode.Indicator2D);
                    LastValue = value;

                    Text = Prefix + value.ToString(Format) + Postfix;
                }
            }
            base.Update(time);
        }
    }
}