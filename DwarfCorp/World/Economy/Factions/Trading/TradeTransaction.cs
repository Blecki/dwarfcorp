using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Trade
{
    public class TradeTransaction
    {
        public ITradeEntity EnvoyEntity;
        public List<Resource> EnvoyItems;
        public DwarfBux EnvoyMoney;
        public ITradeEntity PlayerEntity;
        public List<Resource> PlayerItems;
        public DwarfBux PlayerMoney;

        public void Apply(WorldManager World)
        {
            if (EnvoyEntity != null && PlayerEntity != null)
            {
                EnvoyEntity.AddMoney(-EnvoyMoney);
                EnvoyEntity.AddMoney(PlayerMoney);

                PlayerEntity.AddMoney(-PlayerMoney);
                PlayerEntity.AddMoney(EnvoyMoney);

                EnvoyEntity.RemoveResources(EnvoyItems);
                PlayerEntity.RemoveResources(PlayerItems);

                EnvoyEntity.AddResources(PlayerItems);
                PlayerEntity.AddResources(EnvoyItems);
            }
        }
    }

    public class MarketTransaction
    {
        public ITradeEntity PlayerEntity;
        public List<Resource> PlayerItems;
        public DwarfBux PlayerMoney;

        public void Apply(WorldManager World)
        {
            PlayerEntity.AddMoney(PlayerMoney);
            PlayerEntity.RemoveResources(PlayerItems);
        }
    }
}
