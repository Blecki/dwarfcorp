using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class StockTicker : Gum.Widget
    {
        public Economy Economy;
        Gum.Widget GraphBox;
        Widget MinLabel;
        Widget MidLabel;
        Widget MaxLabel;

        public Company.Sector SelectedSectors = Company.Sector.All;
        public int StartHistory = 0;
        public int EndHistory = 10;

        public override void Construct()
        {
            Border = "border-thin";

            var topBar = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            var KeyPanel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 0)
            });

            MaxLabel = KeyPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                TextHorizontalAlign = HorizontalAlign.Right
            });

            MinLabel = KeyPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 30),
                TextHorizontalAlign = HorizontalAlign.Right
            });

            MidLabel = KeyPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(0, 30),
                TextHorizontalAlign = HorizontalAlign.Right,
                TextVerticalAlign = VerticalAlign.Center
            });



            GraphBox = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Border = "border-thin",
                Hidden = true
            });
        }

        private IEnumerable<Company> EnumerateCompaniesInSector(Company.Sector Sector)
        {
            foreach (var company in Economy.Market.Where(c => (c.Industry & Sector) == c.Industry))
                yield return company;
            yield return Economy.Company;
        }

        private IEnumerable<Vector3> EnumerateStockValue(Company Company, int Start, int End)
        {
            var graphStart = Start;
            while (Start <= End && Start < Company.StockHistory.Count)
            {
                yield return new Vector3(Start - graphStart, (float)Company.StockHistory[Start].Value,
                    (Start == 0 ? 0.0f : (float)Company.StockHistory[Start - 1].Value));
                Start += 1;
            }
        }

        private IEnumerable<int> EnumerateStockRange(int Start, int End)
        {
            var graphStart = Start;
            while (Start <= End)
            {
                yield return Start - graphStart;
                Start += 1;
            }
        }

        protected override Gum.Mesh Redraw()
        {
            var xScale = (float)GraphBox.Rect.Width / (float)(EndHistory - StartHistory);
            var columns = Gum.Mesh.Merge(EnumerateStockRange(StartHistory, EndHistory).Select(x =>
            {
                return Gum.Mesh.Quad().Scale(1.0f, GraphBox.Rect.Height)
                .Translate(xScale * x + GraphBox.Rect.X, GraphBox.Rect.Y);
            }).ToArray());
            columns.Texture(Root.GetTileSheet("basic").TileMatrix(1));
            columns.Colorize(new Vector4(0, 0, 0, 1));

            float maxValue = EnumerateCompaniesInSector(SelectedSectors).SelectMany(c =>
                EnumerateStockValue(c, StartHistory, EndHistory).Select(v => v.Y)).Max();

            var graphLine = Gum.Mesh.Merge(EnumerateCompaniesInSector(SelectedSectors).SelectMany(c =>
                EnumerateStockValue(c, StartHistory + 1, EndHistory).Select(v =>
                {
                    var r = Gum.Mesh.Quad();
                    r.verticies[0].Position.Y += (maxValue - v.Z);
                    r.verticies[3].Position.Y += (maxValue - v.Z);
                    r.verticies[1].Position.Y += (maxValue - v.Y);
                    r.verticies[2].Position.Y += (maxValue - v.Y);
                    r.Translate(v.X, 0.0f);
                    r.Colorize(c.Information.LogoSymbolColor);
                    return r;
                })).ToArray());

            graphLine.Texture(Root.GetTileSheet("basic").TileMatrix(1));
            graphLine.Scale(xScale, (float)GraphBox.Rect.Height / maxValue);
            graphLine.Translate(GraphBox.Rect.X, GraphBox.Rect.Y);
            
            MinLabel.Text = "$0.00";
            MaxLabel.Text = String.Format("${0:0.00}", maxValue);
            MidLabel.Text = String.Format("${0:0.00}", maxValue / 2.0f);

            return Gum.Mesh.Merge(base.Redraw(), columns, graphLine);
        }
    }
}
