using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class FinancePanel : Gum.Widget
    {
        public Economy Economy;
        StockTicker Ticker;
        Gum.Widgets.CheckBox ExplorationBox;
        Gum.Widgets.CheckBox MilitaryBox;
        Gum.Widgets.CheckBox ManufacturingBox;
        Gum.Widgets.CheckBox MagicBox;
        Gum.Widgets.CheckBox FinanceBox;

        private void SetTickerSector()
        {
            var sector = (ExplorationBox.CheckState ? Company.Sector.Exploration : Company.Sector.None)
                | (MilitaryBox.CheckState ? Company.Sector.Military : Company.Sector.None)
                | (ManufacturingBox.CheckState ? Company.Sector.Manufacturing : Company.Sector.None)
                | (MagicBox.CheckState ? Company.Sector.Magic : Company.Sector.None)
                | (FinanceBox.CheckState ? Company.Sector.Finance : Company.Sector.None);
            Ticker.SelectedSectors = sector;
            Ticker.Invalidate();
        }
        
        public override void Construct()
        {
            Border = "border-thin";

            var selectorPanel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(128, 0)
            });

            ExplorationBox = selectorPanel.AddChild(new Gum.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Exploration",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gum.Widgets.CheckBox;

            MilitaryBox = selectorPanel.AddChild(new Gum.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Military",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gum.Widgets.CheckBox;

            ManufacturingBox = selectorPanel.AddChild(new Gum.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Manufacturing",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gum.Widgets.CheckBox;

            MagicBox = selectorPanel.AddChild(new Gum.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Magic",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gum.Widgets.CheckBox;

            FinanceBox = selectorPanel.AddChild(new Gum.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Finance",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gum.Widgets.CheckBox;

            Ticker = AddChild(new StockTicker
            {
                AutoLayout = AutoLayout.DockFill,
                Economy = Economy,
                SelectedSectors = Company.Sector.None
            }) as StockTicker;
        }

    }
}
