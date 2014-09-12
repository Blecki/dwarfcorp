using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class CapitalPanel : Panel
    {
        public Label CurrentMoneyLabel { get; set; }
        public Label TotalPayLabel { get; set; }
        public Faction Faction { get; set; }
        public StockTicker Stocks { get; set; }
        public CapitalPanel(DwarfGUI gui, GUIComponent parent, Faction faction) :
            base(gui, parent)
        {
            Faction = faction;
            GridLayout layout = new GridLayout(gui, this, 4, 4);
            CurrentMoneyLabel = new Label(gui, layout, "Treasury: ", GUI.TitleFont);
            layout.SetComponentPosition(CurrentMoneyLabel, 0, 0, 2, 1);

            CurrentMoneyLabel.OnUpdate += CurrentMoneyLabel_OnUpdate;

            TotalPayLabel = new Label(gui, layout, "Employee pay: ", GUI.DefaultFont);
            layout.SetComponentPosition(TotalPayLabel, 2, 0, 2, 1);

            Stocks = new StockTicker(gui, layout, Faction.Economy);
            layout.SetComponentPosition(Stocks, 0, 1, 4, 3);
        }

        void CurrentMoneyLabel_OnUpdate()
        {
            CurrentMoneyLabel.Text = "Treasury: " + Faction.Economy.CurrentMoney.ToString("C");

            float totalPay = Faction.Minions.Sum(minion => minion.Stats.CurrentLevel.Pay);

            TotalPayLabel.Text = "Employee pay: " + totalPay.ToString("C") + " per day";
        }
    }
}
