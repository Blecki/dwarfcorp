using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

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
        public List<ResourceAmount> Resources { get { return SourceEnvoy.TradeGoods; } }
        public void AddMoney(DwarfBux Money) { SourceEnvoy.TradeMoney += Money; }
        public void AddResources(List<ResourceAmount> Resources)
        {
            foreach(var resource in Resources)
            {
                bool found = false;
                foreach (var existingResource in SourceEnvoy.TradeGoods)
                {
                    if (existingResource.Type == resource.Type)
                    {
                        existingResource.Count += resource.Count;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    SourceEnvoy.TradeGoods.Add(resource);
                }
            }
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

        public DwarfBux ComputeValue(List<ResourceAmount> Resources)
        {
            return Resources.Sum(r => ComputeValue(r.Type) * (decimal)r.Count);
        }

        public void RemoveResources(List<ResourceAmount> Resources)
        {
            foreach(var r in Resources)
            {
                foreach(var r2 in SourceEnvoy.TradeGoods)
                {
                    if (r.Type == r2.Type)
                    {
                        r2.Count -= r.Count;
                    }
                }
            }
        }
    }
}
