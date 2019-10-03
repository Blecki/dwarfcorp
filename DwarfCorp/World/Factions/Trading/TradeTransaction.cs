using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Trade
{
    public class TradeTransaction
    {
        public ITradeEntity EnvoyEntity;
        public List<ResourceTypeAmount> EnvoyItems;
        public DwarfBux EnvoyMoney;
        public ITradeEntity PlayerEntity;
        public List<ResourceTypeAmount> PlayerItems;
        public DwarfBux PlayerMoney;

        public void Apply(WorldManager World)
        {
            if (EnvoyEntity != null)
            {
                EnvoyEntity.AddMoney(-EnvoyMoney);
                EnvoyEntity.AddMoney(PlayerMoney);
                var removedResources = EnvoyEntity.RemoveResourcesByType(EnvoyItems);
                EnvoyEntity.AddResources(removedResources);
            }

            if (PlayerEntity != null)
            {
                PlayerEntity.AddMoney(-PlayerMoney);
                PlayerEntity.AddMoney(EnvoyMoney);
                var removedResources = PlayerEntity.RemoveResourcesByType(PlayerItems);
                PlayerEntity.AddResources(removedResources);
            }
        }
    }

    public class MarketTransaction
    {
        public ITradeEntity PlayerEntity;
        public List<ResourceTypeAmount> PlayerItems;
        public DwarfBux PlayerMoney;

        public void Apply(WorldManager World)
        {
            PlayerEntity.AddMoney(PlayerMoney);
            PlayerEntity.RemoveResourcesByType(PlayerItems);
        }
    }
}
