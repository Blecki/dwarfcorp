using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class StatsTracker
    {
        public class Stat
        {
            private const int MaxStats = 1024;

            public struct Entry
            {
                public DateTime Date;
                public float Value;
            }
            public List<Entry> Values = new List<Entry>();
            public void Add(DateTime now, float value)
            {
                if (Values.Count > 0 && (now - Values.Last().Date) < new TimeSpan(0, 1, 0, 0, 0))
                {
                    return;
                }
                Values.Add(new Entry()
                {
                    Date = now,
                    Value = value
                });

                if (Values.Count > MaxStats)
                {
                    Values.RemoveAt(0);
                }
            }
        }

        public Dictionary<string, Stat> GameStats = new Dictionary<string, Stat>();

        public void AddStat(string name, DateTime time, float value)
        {
            if (!GameStats.ContainsKey(name))
            {
                GameStats.Add(name, new Stat());
            }
            GameStats[name].Add(time, value);
        }

    }
    public class FinancePanel : Gui.Widget
    {
        public Faction Faction;
        private Widget InfoWidget;
        int numrows = 0;
        private Widget Title;

        public FinancePanel()
        {
        }

        private void AddRow(string name, string value)
        {
            var row = InfoWidget.AddChild(new Widget()
            {
                Font = "font10",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(640, 30),
                MaximumSize =  new Point (700, 30),
                Text = name,
                TextVerticalAlign = VerticalAlign.Center,
                Background = new TileReference("basic", 0),
                BackgroundColor = numrows % 2 == 0 ? new Vector4(0, 0, 0, 0.05f) : new Vector4(0, 0, 0, 0.2f)
            });
            numrows++;
            row.AddChild(new Widget()
            {
                Text = value,
                Font = "font10",
                AutoLayout = AutoLayout.DockRight,
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Right,
                MinimumSize = new Point(320, 30)
            });
        }

        public override void Construct()
        {
            Font = "font10";
            Title = AddChild(new Widget()
            {
                Font = "font16",
                Text = "Finance",
                AutoLayout = AutoLayout.DockTop
            });

            InfoWidget = AddChild(new Widget()
            {
                Font = "font10",
                Text = "",
                MinimumSize = new Point(640, 300),
                AutoLayout = AutoLayout.DockTop
            });
            
            OnUpdate = (sender, time) =>
            {
                numrows = 0;
                InfoWidget.Clear();
                AddRow("Liquid assets:", Faction.Economy.CurrentMoney.ToString());
                var resources = Faction.ListResourcesInStockpilesPlusMinions();
                AddRow("Material assets:", String.Format("{0} goods valued at ${1}", resources.Values.Select(r => r.First.NumResources + r.Second.NumResources).Sum(),
                    resources.Values.Select(r =>
                    {
                        var value = ResourceLibrary.GetResourceByName(r.First.ResourceType).MoneyValue.Value;
                        return (r.First.NumResources * value) + (r.Second.NumResources * value);
                    }).Sum()));
                var payPerDay = (DwarfBux)Faction.Minions.Select(m => m.Stats.CurrentLevel.Pay.Value).Sum();
                AddRow("Employees:", String.Format("{0} at {1} per day.", Faction.Minions.Count, payPerDay));
                AddRow("Runway:", String.Format("{0} day(s).\n", (int)(Faction.Economy.CurrentMoney / Math.Max(payPerDay, (decimal)0.01))));
                var freeStockPile = Faction.ComputeRemainingStockpileSpace();
                var totalStockPile = Math.Max(Faction.ComputeTotalStockpileSpace(), 1);
                AddRow("Stockpile space:", String.Format("{0} used of {1} ({2:00.00}%)\n", totalStockPile - freeStockPile, totalStockPile, (float)(totalStockPile - freeStockPile) / (float)totalStockPile * 100.0f));
                var freeTreasury = Faction.ComputeRemainingTreasurySpace();
                var totalTreasury = Math.Max(Faction.ComputeTotalTreasurySpace(), 1.0m);
                AddRow("Treasury space:", String.Format("{0} used of {1} ({2:00.00}%)\n", Faction.Economy.CurrentMoney, (DwarfBux)totalTreasury, 100.0f * (1.0f - ((float)(decimal)(totalTreasury - Faction.Economy.CurrentMoney) / (float)totalTreasury))));
                AddRow("Average dwarf happiness:", String.Format("{0}%", (int)(float)Faction.Minions.Sum(m => m.Status.Happiness.Percentage) / Math.Max(Faction.Minions.Count, 1)));
                InfoWidget.Layout();
            };
            var selector = AddChild(new ComboBox()
            {
                Items = Faction.World.Stats.GameStats.Keys.ToList(),
                AutoLayout = AutoLayout.DockTop
            }) as ComboBox;
            var graph = AddChild(new Graph() { AutoLayout = AutoLayout.DockFill,  GraphStyle = Graph.Style.LineChart }) as Graph;
            graph.Values = Faction.World.Stats.GameStats["Money"].Values.Select(v => v.Value).ToList();

            selector.OnSelectedIndexChanged = (sender) =>
            {
                var values = Faction.World.Stats.GameStats[selector.SelectedItem].Values;
                graph.Values = Faction.World.Stats.GameStats[selector.SelectedItem].Values.Select(v => v.Value).ToList();
                if (values.Count > 0)
                {
                    graph.XLabelMin = "\n" + TextGenerator.AgeToString(Faction.World.Time.CurrentDate - values.First().Date);
                    graph.XLabelMax = "\nNow";
                }
                graph.Invalidate();
            };
            selector.OnSelectedIndexChanged.Invoke(selector);
            Layout();
            Root.RegisterForUpdate(this);
        }
    }
}
