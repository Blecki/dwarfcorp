using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates
{
    public class FinancePanel : Gui.Widgets.Window
    {
        private Widget InfoWidget;
        public WorldManager World;
        int numrows = 0;

        public FinancePanel()
        {
        }

        private void AddRow(string name, string value)
        {
            var row = InfoWidget.AddChild(new Widget()
            {
                Font = "font10",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                Text = name,
                TextVerticalAlign = VerticalAlign.Center,
                Background = new TileReference("basic", 0),
                BackgroundColor = numrows % 2 == 0 ? new Vector4(0, 0, 0, 0.05f) : new Vector4(0, 0, 0, 0.2f)
            });

            row.AddChild(new Widget()
            {
                Text = value,
                Font = "font10",
                AutoLayout = AutoLayout.DockRight,
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Right,
                MinimumSize = new Point(200, 30),
                MaximumSize = new Point(200, 30)
            });

            numrows++;
        }

        public override void Construct()
        {
            base.Construct();

            Text = "Finance";
            
            InfoWidget = AddChild(new Widget()
            {
                Font = "font10",
                Text = "",
                MinimumSize = new Point(0, 300),
                AutoLayout = AutoLayout.DockTop
            });

            AddChild(new PolicyPanel
            {
                AutoLayout = AutoLayout.DockFill,
                World = World
            });
            
            OnUpdate = (sender, time) =>
            {
                if (this.Hidden) return;

                numrows = 0;
                InfoWidget.Clear();
                AddRow("Corporate Liquid Assets:", World.Overworld.PlayerCorporationFunds.ToString());
                AddRow("Corporate Material Assets:", new DwarfBux(World.Overworld.PlayerCorporationResources.Enumerate().Sum(r => r.MoneyValue)).ToString());
                AddRow("Liquid assets:", World.PlayerFaction.Economy.Funds.ToString());
                var resources = World.EnumerateResourcesIncludingMinions();
                AddRow("Material assets:", String.Format("{0} goods valued at ${1}",
                    resources.Count(),
                    resources.Sum(r => r.MoneyValue)));
                var payPerDay = (DwarfBux)World.PlayerFaction.Minions.Select(m => m.Stats.DailyPay.Value).Sum();
                AddRow("Employees:", String.Format("{0} at {1} per day.", World.PlayerFaction.Minions.Count, payPerDay));
                AddRow("Runway:", String.Format("{0} day(s).\n", (int)(World.PlayerFaction.Economy.Funds / Math.Max(payPerDay, (decimal)0.01))));
                var freeStockPile = World.ComputeRemainingStockpileSpace();
                var totalStockPile = Math.Max(World.ComputeTotalStockpileSpace(), 1);
                AddRow("Stockpile space:", String.Format("{0} used of {1} ({2:00.00}%)\n", totalStockPile - freeStockPile, totalStockPile, (float)(totalStockPile - freeStockPile) / (float)totalStockPile * 100.0f));
                AddRow("Average dwarf happiness:", String.Format("{0}%", (int)(float)World.PlayerFaction.Minions.Sum(m => m.Stats.Happiness.Percentage) / Math.Max(World.PlayerFaction.Minions.Count, 1)));
                InfoWidget.Layout();
            };

            Layout();
            Root.RegisterForUpdate(this);
        }
    }
}
