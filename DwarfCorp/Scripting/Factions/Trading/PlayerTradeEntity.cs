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

        public Race TraderRace { get { return Faction.Race; } }
        public Faction TraderFaction { get { return Faction; } }
        public int AvailableSpace { get { return Faction.World.ComputeRemainingStockpileSpace(); } }
        public DwarfBux Money { get { return Faction.Economy.Funds; } }
        public List<ResourceAmount> Resources { get { return Faction.World.ListResources().Where(r => 
            ResourceLibrary.GetResourceByName(r.Value.Type).MoneyValue > 0).Select(r => r.Value).ToList(); } }

        public void AddMoney(DwarfBux Money)
        {
            Faction.AddMoney(Money);
        }

        public void AddResources(List<ResourceAmount> Resources)
        {
            foreach (var resource in Resources)
                Faction.World.AddResources(resource);
        }

        public DwarfBux ComputeValue(String Resource)
        {
            return ResourceLibrary.GetResourceByName(Resource).MoneyValue;
        }

        public DwarfBux ComputeValue(List<ResourceAmount> Resources)
        {
            return Resources.Sum(r => ComputeValue(r.Type) * (decimal)r.Count);
        }

        public void RemoveResources(List<ResourceAmount> Resources)
        {
            Faction.World.RemoveResources(Resources);
        }
    }
}
