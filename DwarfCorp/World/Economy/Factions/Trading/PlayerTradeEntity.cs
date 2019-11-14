using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Trade
{
    public class PlayerTradeEntity : ITradeEntity
    {
        private Faction Faction;

        public PlayerTradeEntity(Faction Faction)
        {
            this.Faction = Faction;
        }

        public Faction TraderFaction { get { return Faction; } }
        public int AvailableSpace { get { return Faction.World.ComputeRemainingStockpileSpace(); } }
        public DwarfBux Money { get { return Faction.Economy.Funds; } }
        public ResourceSet Resources => Faction.World.GetTradeableResources();

        public void AddMoney(DwarfBux Money)
        {
            Faction.AddMoney(Money);
        }

        public void AddResources(List<Resource> Resources)
        {
            foreach (var resource in Resources)
                Faction.World.AddResources(resource);
        }

        public DwarfBux ComputeValue(String Resource)
        {
            return Library.GetResourceType(Resource).HasValue(out var res) ? res.MoneyValue : 0;
        }

        public DwarfBux ComputeValue(List<Resource> Resources)
        {
            return Resources.Sum(r => r.MoneyValue);
        }

        public void RemoveResources(List<Resource> Resources)
        {
            Faction.World.RemoveResources(Resources);
        }
    }
}
