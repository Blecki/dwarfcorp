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
        public int EnvoyMoney;
        public ITradeEntity PlayerEntity;
        public List<ResourceAmount> PlayerItems;
        public int PlayerMoney;

        public float ValueForPlayer
        {
            get
            {
                return (EnvoyEntity.ComputeValue(EnvoyItems) + EnvoyMoney) -
                  (EnvoyEntity.ComputeValue(PlayerItems) + PlayerMoney);
            }
        }

        public void Apply()
        {
            EnvoyEntity.AddMoney(-EnvoyMoney);
            EnvoyEntity.AddMoney(PlayerMoney);
            EnvoyEntity.RemoveResources(EnvoyItems);
            EnvoyEntity.AddResources(PlayerItems);

            PlayerEntity.AddMoney(-PlayerMoney);
            PlayerEntity.AddMoney(EnvoyMoney);
            PlayerEntity.RemoveResources(PlayerItems);
            PlayerEntity.AddResources(EnvoyItems);
        }
    }
}
