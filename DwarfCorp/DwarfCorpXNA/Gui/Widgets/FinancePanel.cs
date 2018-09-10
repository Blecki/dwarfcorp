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
        public Faction Faction;

        public FinancePanel()
        {
        }

        public override void Construct()
        {
            Font = "font16";
            
            OnUpdate = (sender, time) =>
            {
                var builder = new StringBuilder();
                builder.AppendFormat("Liquid assets: ${0}\n", Faction.Treasurys.Select(t => t.Money.Value).Sum());

                var resources = Faction.ListResourcesInStockpilesPlusMinions();

                builder.AppendFormat("Material assets: {0} resources valued at ${1}\n", resources.Values.Select(r => r.First.NumResources + r.Second.NumResources).Sum(),
                    resources.Values.Select(r =>
                    {
                        var value = ResourceLibrary.GetResourceByName(r.First.ResourceType).MoneyValue.Value;
                        return (r.First.NumResources * value) + (r.Second.NumResources * value);
                    }).Sum());

                builder.AppendFormat("{0} at ${1} per day.\n", Faction.Minions.Count, Faction.Minions.Select(m => m.Stats.CurrentLevel.Pay.Value).Sum());

                var freeStockPile = Faction.ComputeRemainingStockpileSpace();
                var totalStockPile = Faction.ComputeTotalStockpileSpace();
                builder.AppendFormat("Stockpile space: {0} used of {1} ({2:00.00}%)\n", totalStockPile - freeStockPile, totalStockPile, (float)(totalStockPile - freeStockPile) / (float)totalStockPile * 100.0f);

                Text = builder.ToString();
            };

            Root.RegisterForUpdate(this);
        }
    }
}
