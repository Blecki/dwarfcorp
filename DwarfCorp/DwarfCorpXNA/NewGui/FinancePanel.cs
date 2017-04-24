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

        public override void Construct()
        {
            Border = "border-thin";

            Ticker = AddChild(new StockTicker
            {
                Economy = Economy
            }) as StockTicker;
        }

    }
}
