// CapitalPanel.cs
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
