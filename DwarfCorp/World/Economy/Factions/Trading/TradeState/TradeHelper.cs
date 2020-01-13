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
        public static List<TradeableItem> AggregateResourcesIntoTradeableItems(ResourceSet Resources)
        {
            var aggregates = new Dictionary<String, TradeableItem>();
            foreach (var res in Resources.Enumerate())
            {
                if (aggregates.ContainsKey(res.DisplayName))
                    aggregates[res.DisplayName].Resources.Add(res);
                else
                    aggregates.Add(res.DisplayName, new Gui.Widgets.TradeableItem { Resources = new List<Resource> { res }, ResourceType = res.DisplayName, Prototype = res });
            }

            return aggregates.Values.ToList();
        }
    }
}
