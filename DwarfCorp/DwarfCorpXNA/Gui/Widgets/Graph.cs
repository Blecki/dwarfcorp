using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class Graph : Gui.Widget
    {
        Gui.Widget GraphBox;
        Widget MinLabel;
        Widget MaxLabel;
        public string MinLabelString = "0";

        public List<float> Values = new List<float>();
        public float MaxValueSeen = 0.0f;
        public int GraphWidth { get { return Children[2].Rect.Width; } }

        public override void Construct()
        {
            MaxLabel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 30),
                TextSize = GameSettings.Default.ConsoleTextSize,
                Font = "monofont",
                TextHorizontalAlign = HorizontalAlign.Left
            });

            MinLabel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                TextSize = GameSettings.Default.ConsoleTextSize,
                Font = "monofont",
                TextHorizontalAlign = HorizontalAlign.Left
            });

            GraphBox = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Hidden = true
            });
        }

        private IEnumerable<int> EnumerateRange(int Start, int End)
        {
            var graphStart = Start;
            while (Start < End)
            {
                yield return Start - graphStart;
                Start += 1;
            }
        }

        protected override Gui.Mesh Redraw()
        {
            if (Values.Count == 0)
                return base.Redraw();

            var tileMatrix = Root.GetTileSheet("basic").TileMatrix(1);
            float alpha = 0.995f;
            float currentMax = Values.Max();
            MaxValueSeen = Math.Max(alpha * MaxValueSeen +  (1.0f - alpha) * currentMax, currentMax);
            var maxValue = MaxValueSeen;
            var yScale = (float)GraphBox.Rect.Width / maxValue;
            var columns = Gui.Mesh.Merge(EnumerateRange(0, Values.Count).Select(x =>
            {
                return Gui.Mesh.Quad().Scale(1.0f, Values[x] * yScale)
                .Translate(x + GraphBox.Rect.X, GraphBox.Rect.Y)
                .Texture(tileMatrix)
                .Colorize(new Vector4(Values[x] / maxValue, 1.0f - (Values[x] / maxValue), 0, 0.75f));
            }).ToArray());

            MinLabel.Text = ModString(MinLabelString);
            MaxLabel.Text = ModString(String.Format("{0}", maxValue));

            return Gui.Mesh.Merge(base.Redraw(), columns);
        }

        private String ModString(String S)
        {
            var r = new StringBuilder();
            foreach (var C in S)
                r.Append((char)(C + ' '));
            return r.ToString();
        }
    }
}
