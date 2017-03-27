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
        int Money { get; }
        int AvailableSpace { get; }
        float ComputeValue(List<ResourceAmount> Resources);
        float ComputeValue(ResourceLibrary.ResourceType Resource);

        void RemoveResources(List<ResourceAmount> Resources);
        void AddResources(List<ResourceAmount> Resources);
        void AddMoney(float Money);
    }

    public class EnvoyTradeEntity : ITradeEntity
    {
        private Faction.TradeEnvoy SourceEnvoy;

        public EnvoyTradeEntity(Faction.TradeEnvoy SourceEnvoy)
        {
            this.SourceEnvoy = SourceEnvoy;
        }

        public int AvailableSpace { get { return 0; } }
        public int Money { get { return (int)SourceEnvoy.TradeMoney; } }
        public List<ResourceAmount> Resources { get { return SourceEnvoy.TradeGoods; } }
        public void AddMoney(float Money) { }
        public void AddResources(List<ResourceAmount> Resources) { }
        public float ComputeValue(ResourceLibrary.ResourceType Resource)
        {
            // Todo: Account for rare or common items.
            return ResourceLibrary.GetResourceByName(Resource).MoneyValue;
        }

        public float ComputeValue(List<ResourceAmount> Resources)
        {
            return Resources.Sum(r => ComputeValue(r.ResourceType) * r.NumResources);
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

        public int AvailableSpace { get { return Faction.ComputeStockpileSpace(); } }
        public int Money { get { return (int)Faction.Economy.CurrentMoney; } }
        public List<ResourceAmount> Resources { get { return Faction.ListResources().Select(r => r.Value).ToList(); } }
        public void AddMoney(float Money) { Faction.Economy.CurrentMoney += Money; }

        public void AddResources(List<ResourceAmount> Resources)
        {
            foreach (var resource in Resources)
                Faction.AddResources(resource);
        }

        public float ComputeValue(ResourceLibrary.ResourceType Resource)
        {
            return ResourceLibrary.GetResourceByName(Resource).MoneyValue;
        }

        public float ComputeValue(List<ResourceAmount> Resources)
        {
            return Resources.Sum(r => ComputeValue(r.ResourceType) * r.NumResources);
        }

        public void RemoveResources(List<ResourceAmount> Resources)
        {
            Faction.RemoveResources(Resources, Vector3.Zero, false);
        }
    }
}
