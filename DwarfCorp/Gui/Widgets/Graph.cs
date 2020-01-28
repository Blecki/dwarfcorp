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
        public Widget MinLabel;
        public Widget MaxLabel;
        public Widget XLabelMinWidget;
        public Widget XLabelMaxWidget;
        public string XLabelMin = "";
        public string XLabelMax = "";
        public string MinLabelString = "0";
        public enum Style
        {
            BarChart,
            LineChart
        }
        public Style GraphStyle = Style.BarChart;
        public List<float> Values = new List<float>();
        public float MaxValueSeen = 0.0f;
        public int GraphWidth { get { return Children[2].Rect.Width; } }
        public Color LineColor = Color.Black;

        public void SetFont(string font)
        {
            MinLabel.Font = font;
            MaxLabel.Font = font;
            XLabelMinWidget.Font = font;
            XLabelMaxWidget.Font = font;
        }

        public override void Construct()
        {
            MaxLabel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 30),
                TextSize = GameSettings.Current.ConsoleTextSize,
                Font = "monofont",
                TextHorizontalAlign = HorizontalAlign.Left
            });

            MinLabel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                TextSize = GameSettings.Current.ConsoleTextSize,
                Font = "monofont",
                TextHorizontalAlign = HorizontalAlign.Left
            });

            GraphBox = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Transparent = true
            });

            XLabelMinWidget = GraphBox.AddChild(new Widget()
            {
                Font = "monofont",
                TextColor = Color.Black.ToVector4(),
                AutoLayout = AutoLayout.FloatBottomLeft,
                MinimumSize = new Point(128, 32),
                Text = XLabelMin,
                TextHorizontalAlign = HorizontalAlign.Left,
                TextVerticalAlign = VerticalAlign.Below
            });

            XLabelMaxWidget = GraphBox.AddChild(new Widget()
            {
                Font = "monofont",
                TextColor = Color.Black.ToVector4(),
                AutoLayout = AutoLayout.FloatBottomRight,
                MinimumSize = new Point(128, 32),
                Text = XLabelMin,
                TextHorizontalAlign = HorizontalAlign.Right,
                TextVerticalAlign = VerticalAlign.Below
            });
            GraphBox.Layout();
            Layout();
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
            if (Values.Count < 2)
                return base.Redraw();

            if (GraphStyle == Style.BarChart)
            {
                var tileMatrix = Root.GetTileSheet("basic").TileMatrix(1);
                float alpha = 0.995f;
                float currentMax = Values.Max();
                MaxValueSeen = Math.Max(alpha * MaxValueSeen + (1.0f - alpha) * currentMax, currentMax);
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
            else
            {
                var rect = GraphBox.Rect;
                var tileMatrix = Root.GetTileSheet("basic").TileMatrix(1);
                float max = Math.Max(Values.Max(), 1.0e-3f);
                float min = Math.Min(Values.Min(), 1.0e-4f);
                int num_gridlines = 10;

                List<Gui.Mesh> meshes = new List<Mesh>();
                for (int i = 0; i < num_gridlines + 1; i++)
                {
                    float alpha = (float)i / (num_gridlines);
                    float x_0 = rect.X;
                    float x_1 = rect.X + rect.Width;
                    float y_0 = rect.Y + (rect.Height - (alpha) * rect.Height);
                    float y_1 = rect.Y + (rect.Height - (alpha) * rect.Height);
                    meshes.Add(
                        Mesh.Quad().Scale(rect.Width, 1).Translate(x_0, y_0)
                    .Texture(tileMatrix).Colorize(LineColor.ToVector4() * 0.5f));
                }

                for (int i = 0; i < Values.Count - 1; i++)
                {
                    float alpha_0 = (float)(i) / Values.Count;
                    float x_0 = rect.X + Rect.Width * alpha_0;
                    float y_0 = rect.Y  + (rect.Height - (Values[i] / max) * rect.Height);
                    float alpha_1 = (float)(i + 1) / Values.Count;
                    float x_1 = rect.X + rect.Width * alpha_1;
                    float y_1 = rect.Y + (rect.Height - (Values[i + 1] / max) * rect.Height);

                    Vector3 p1 = new Vector3(x_0, y_0, 0.0f);
                    Vector3 p2 = new Vector3(x_1, y_1, 0.0f);
                    Vector3 dir = (p2 - p1);
                    dir.Normalize();
                    Vector3 cross = Vector3.Cross(Vector3.UnitZ, dir);
                    Vector3 p11 = p1 + cross * 2;
                    Vector3 p12 = p1 - cross * 2;
                    Vector3 p21 = p2 + cross * 2;
                    Vector3 p22 = p2 - cross * 2;
                    meshes.Add(Mesh.Quad(new Vector2(p11.X, p11.Y), new Vector2(p12.X, p12.Y), new Vector2(p21.X, p21.Y), new Vector2(p22.X, p22.Y))
                        .Texture(tileMatrix).Colorize(LineColor.ToVector4()));
                }
                MaxLabel.Text = String.Format("{0}", (int)min);
                MinLabel.Text = String.Format("{0}", (int)max);
                XLabelMaxWidget.Text = XLabelMax;
                XLabelMinWidget.Text = XLabelMin;
                Layout();
                return Gui.Mesh.Merge(base.Redraw(), Gui.Mesh.Merge(meshes.ToArray()));
            }
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
