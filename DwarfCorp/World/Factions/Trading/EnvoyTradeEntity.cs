using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp.Trade
{
    public class EnvoyTradeEntity : ITradeEntity
    {
        private TradeEnvoy SourceEnvoy;

        public EnvoyTradeEntity(TradeEnvoy SourceEnvoy)
        {
            this.SourceEnvoy = SourceEnvoy;
        }

        public int AvailableSpace { get { return 0; } }
        public DwarfBux Money { get { return SourceEnvoy.TradeMoney; } }
        public ResourceSet Resources { get { return SourceEnvoy.TradeGoods; } }
        public void AddMoney(DwarfBux Money) { SourceEnvoy.TradeMoney += Money; }
        public void AddResources(List<Resource> Resources)
        {
            foreach (var res in Resources)
                SourceEnvoy.TradeGoods.Add(res);
        }
        public Race TraderRace { get { return SourceEnvoy.OwnerFaction.Race; } }
        public Faction TraderFaction { get { return SourceEnvoy.OwnerFaction; } }

        public DwarfBux ComputeValue(String Resource)
        {
            if (Library.GetResourceType(Resource).HasValue(out var resource))
                return GetValueMultiplier(resource) * resource.MoneyValue;
            return 0.0m;
        }

        private float GetValueMultiplier(ResourceType ResourceType)
        {
            if (SourceEnvoy.OwnerFaction.Race.CommonResources.Any(r => ResourceType.Tags.Contains(r)))
                return 0.75f;
            if (SourceEnvoy.OwnerFaction.Race.RareResources.Any(r => ResourceType.Tags.Contains(r)))
                return 1.25f;
            return 1.0f;
        }

        public DwarfBux ComputeValue(List<Resource> Resources)
        {
            return Resources.Sum(r => (r.ResourceType.HasValue(out var type) ? GetValueMultiplier(type) : 1.0f) * r.MoneyValue);
        }

        public void RemoveResources(List<Resource> Resources)
        {
            foreach (var res in Resources)
                SourceEnvoy.TradeGoods.Remove(res);
        }
    }
}
