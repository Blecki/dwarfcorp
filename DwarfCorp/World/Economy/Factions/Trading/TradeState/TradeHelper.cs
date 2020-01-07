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
            var uniques = new List<TradeableItem>();
            foreach (var res in Resources.Enumerate())
            {
                if (res.Aggregate)
                {
                    if (aggregates.ContainsKey(res.TypeName))
                        aggregates[res.TypeName].Resources.Add(res);
                    else
                        aggregates.Add(res.TypeName, new Gui.Widgets.TradeableItem { Resources = new List<Resource> { res }, ResourceType = res.TypeName, Prototype = res });
                }
                else
                    uniques.Add(new TradeableItem { Resources = new List<Resource> { res }, ResourceType = res.TypeName, Prototype = res });
            }

            return aggregates.Values.Concat(uniques).ToList();
        }
    }
}
