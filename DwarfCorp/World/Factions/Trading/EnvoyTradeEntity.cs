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
            {
                if (SourceEnvoy.OwnerFaction.Race.CommonResources.Any(r => resource.Tags.Contains(r)))
                    return resource.MoneyValue * 0.75m;
                if (SourceEnvoy.OwnerFaction.Race.RareResources.Any(r => resource.Tags.Contains(r)))
                    return resource.MoneyValue * 1.25m;
                return resource.MoneyValue;
            }
            return 0.0m;
        }

        public DwarfBux ComputeValue(List<ResourceTypeAmount> Resources)
        {
            return Resources.Sum(r => ComputeValue(r.Type) * r.Count);
        }

        public List<Resource> RemoveResourcesByType(List<ResourceTypeAmount> Resources)
        {
            return SourceEnvoy.TradeGoods.RemoveByType(Resources);
        }
    }
}
