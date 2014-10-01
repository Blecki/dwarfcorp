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

        public string WrapLines()
        {
            Vector2 measurement = Datastructures.SafeMeasure(TextFont, Text);

            if(measurement.X < LocalBounds.Width)
            {
                return Text;
            }

            string[] originalWords = Text.Split(' ');

            List<string> wrappedLines = new List<string>();

            StringBuilder actualLine = new StringBuilder();
            double actualWidth = 0;

            foreach (var item in originalWords)
            {
                Vector2 itemMeasure = Datastructures.SafeMeasure(TextFont, item);
                actualLine.Append(item + " ");
                actualWidth += (int)itemMeasure.X;

                if (actualWidth >= LocalBounds.Width)
                {
                    wrappedLines.Add(actualLine.ToString());
                    actualLine.Clear();
                    actualWidth = 0;
                }
            }

            if (actualLine.Length > 0)
                wrappedLines.Add(actualLine.ToString());

            string toReturn = "";

            foreach(var line in wrappedLines)
            {
                toReturn += line + "\n";
            }

            return toReturn;
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            string text = Text;

            if(WordWrap)
            {
                text = WrapLines();
            }

            Drawer2D.DrawAlignedStrokedText(batch, text, TextFont, TextColor, StrokeColor, Alignment, GlobalBounds);
            base.Render(time, batch);
        }
    }

}