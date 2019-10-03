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
            if (EnvoyEntity != null && PlayerEntity != null)
            {
                EnvoyEntity.AddMoney(-EnvoyMoney);
                EnvoyEntity.AddMoney(PlayerMoney);

                PlayerEntity.AddMoney(-PlayerMoney);
                PlayerEntity.AddMoney(EnvoyMoney);

                var envoyRemovedResources = EnvoyEntity.RemoveResourcesByType(EnvoyItems);
                var playerRemovedResources = PlayerEntity.RemoveResourcesByType(PlayerItems);

                EnvoyEntity.AddResources(playerRemovedResources);
                PlayerEntity.AddResources(envoyRemovedResources);
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
