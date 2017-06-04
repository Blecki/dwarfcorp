using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Trade
{
    public interface ITradeEntity
    {
        List<ResourceAmount> Resources { get; }
        DwarfBux Money { get; }
        int AvailableSpace { get; }
        DwarfBux ComputeValue(List<ResourceAmount> Resources);
        DwarfBux ComputeValue(ResourceLibrary.ResourceType Resource);
        Race TraderRace { get; }
        Faction TraderFaction { get; }
        void RemoveResources(List<ResourceAmount> Resources);
        void AddResources(List<ResourceAmount> Resources);
        void AddMoney(DwarfBux Money);
    }

    public class EnvoyTradeEntity : ITradeEntity
    {
        private Faction.TradeEnvoy SourceEnvoy;

        public EnvoyTradeEntity(Faction.TradeEnvoy SourceEnvoy)
        {
            this.SourceEnvoy = SourceEnvoy;
        }

        public int AvailableSpace { get { return 0; } }
        public DwarfBux Money { get { return SourceEnvoy.TradeMoney; } }
        public List<ResourceAmount> Resources { get { return SourceEnvoy.TradeGoods; } }
        public void AddMoney(DwarfBux Money) { }
        public void AddResources(List<ResourceAmount> Resources) { }
        public Race TraderRace { get { return SourceEnvoy.OwnerFaction.Race; } }
        public Faction TraderFaction { get { return SourceEnvoy.OwnerFaction; } }
        public DwarfBux ComputeValue(ResourceLibrary.ResourceType Resource)
        {
            var resource = ResourceLibrary.GetResourceByName(Resource);
            if (SourceEnvoy.OwnerFaction.Race.CommonResources.Any(r => resource.Tags.Contains(r)))
                return resource.MoneyValue * 0.5m;
            if (SourceEnvoy.OwnerFaction.Race.RareResources.Any(r => resource.Tags.Contains(r)))
                return resource.MoneyValue * 2.0m;
            return resource.MoneyValue;
        }

        public DwarfBux ComputeValue(List<ResourceAmount> Resources)
        {
            return Resources.Sum(r => ComputeValue(r.ResourceType) * (decimal)r.NumResources);
        }

        public void RemoveResources(List<ResourceAmount> Resources) { }
    }

    public class PlayerTradeEntity : ITradeEntity
    {
        private Faction Faction;

        public PlayerTradeEntity(Faction Faction)
        {
            this.Faction = Faction;
        }

        public Race TraderRace { get { return Faction.Race; } }
        public Faction TraderFaction { get { return Faction; } }
        public int AvailableSpace { get { return Faction.ComputeStockpileSpace(); } }
        public DwarfBux Money { get { return Faction.Economy.CurrentMoney; } }
        public List<ResourceAmount> Resources { get { return Faction.ListResources().Where(r => 
            ResourceLibrary.GetResourceByName(r.Value.ResourceType).MoneyValue > 0).Select(r => r.Value).ToList(); } }
        public void AddMoney(DwarfBux Money) { Faction.Economy.CurrentMoney += Money; }

        public void AddResources(List<ResourceAmount> Resources)
        {
            foreach (var resource in Resources)
                Faction.AddResources(resource);
        }

        public DwarfBux ComputeValue(ResourceLibrary.ResourceType Resource)
        {
            return ResourceLibrary.GetResourceByName(Resource).MoneyValue;
        }

        public DwarfBux ComputeValue(List<ResourceAmount> Resources)
        {
            return Resources.Sum(r => ComputeValue(r.ResourceType) * (decimal)r.NumResources);
        }

        public void RemoveResources(List<ResourceAmount> Resources)
        {
            Faction.RemoveResources(Resources, Vector3.Zero, false);
        }
    }
}
