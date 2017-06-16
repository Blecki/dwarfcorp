using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class FinancePanel : Gui.Widget
    {
        public Economy Economy;
        StockTicker Ticker;
        Gui.Widgets.CheckBox ExplorationBox;
        Gui.Widgets.CheckBox MilitaryBox;
        Gui.Widgets.CheckBox ManufacturingBox;
        Gui.Widgets.CheckBox MagicBox;
        Gui.Widgets.CheckBox FinanceBox;

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

            ExplorationBox = selectorPanel.AddChild(new Gui.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Exploration",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gui.Widgets.CheckBox;

            MilitaryBox = selectorPanel.AddChild(new Gui.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Military",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gui.Widgets.CheckBox;

            ManufacturingBox = selectorPanel.AddChild(new Gui.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Manufacturing",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gui.Widgets.CheckBox;

            MagicBox = selectorPanel.AddChild(new Gui.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Magic",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gui.Widgets.CheckBox;

            FinanceBox = selectorPanel.AddChild(new Gui.Widgets.CheckBox
            {
                AutoLayout = AutoLayout.DockTop,
                Text = "Finance",
                OnCheckStateChange = (sender) => SetTickerSector()
            }) as Gui.Widgets.CheckBox;

            Ticker = AddChild(new StockTicker
            {
                AutoLayout = AutoLayout.DockFill,
                Economy = Economy,
                SelectedSectors = Company.Sector.None
            }) as StockTicker;
        }

    }
}
