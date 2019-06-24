using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Trade
{
    public class TradeTransaction
    {
        public ITradeEntity EnvoyEntity;
        public List<ResourceAmount> EnvoyItems;
        public DwarfBux EnvoyMoney;
        public ITradeEntity PlayerEntity;
        public List<ResourceAmount> PlayerItems;
        public DwarfBux PlayerMoney;

        public void Apply(WorldManager World)
        {
            if (EnvoyEntity != null)
            {
                EnvoyEntity.AddMoney(-EnvoyMoney);
                EnvoyEntity.AddMoney(PlayerMoney);
                EnvoyEntity.RemoveResources(EnvoyItems);
                EnvoyEntity.AddResources(PlayerItems);
            }

            if (PlayerEntity != null)
            {
                PlayerEntity.AddMoney(-PlayerMoney);
                PlayerEntity.AddMoney(EnvoyMoney);
                PlayerEntity.RemoveResources(PlayerItems);
                PlayerEntity.AddResources(EnvoyItems);
            }
        }
    }

    public class MarketTransaction
    {
        public ITradeEntity PlayerEntity;
        public List<ResourceAmount> PlayerItems;
        public DwarfBux PlayerMoney;

        public void Apply(WorldManager World)
        {
            PlayerEntity.AddMoney(PlayerMoney);
            PlayerEntity.RemoveResources(PlayerItems);
        }
    }
}
