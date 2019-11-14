using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using DwarfCorp.Trade;

namespace DwarfCorp.Play.Trading
{
    public static class Helper
    {
        public static List<Gui.Widgets.TradeableItem> AggregateResourcesIntoTradeableItems(IEnumerable<Resource> Resources)
        {
            var r = new Dictionary<String, Gui.Widgets.TradeableItem>();
            foreach (var res in Resources)
                if (r.ContainsKey(res.DisplayName))
                    r[res.DisplayName].Resources.Add(res);
                else
                    r.Add(res.DisplayName, new Gui.Widgets.TradeableItem { Resources = new List<Resource> { res }, ResourceType = res.DisplayName });
            return r.Values.ToList();
        }
    }
}
