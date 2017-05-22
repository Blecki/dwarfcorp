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

        public DwarfBux ValueForPlayer
        {
            get
            {
                return (EnvoyEntity.ComputeValue(EnvoyItems) + EnvoyMoney) -
                  (EnvoyEntity.ComputeValue(PlayerItems) + PlayerMoney);
            }
        }

        public void Apply(WorldManager World)
        {
            EnvoyEntity.AddMoney(-EnvoyMoney);
            EnvoyEntity.AddMoney(PlayerMoney);
            EnvoyEntity.RemoveResources(EnvoyItems);
            EnvoyEntity.AddResources(PlayerItems);

            PlayerEntity.AddMoney(-PlayerMoney);
            PlayerEntity.AddMoney(EnvoyMoney);
            PlayerEntity.RemoveResources(PlayerItems);
            PlayerEntity.AddResources(EnvoyItems);

            World.GoalManager.OnGameEvent(new Goals.Events.Trade
            {
                PlayerFaction = PlayerEntity.TraderFaction,
                PlayerGold = PlayerMoney,
                PlayerGoods = PlayerItems,
                OtherFaction = EnvoyEntity.TraderFaction,
                OtherGold = EnvoyMoney,
                OtherGoods = EnvoyItems
            });
        }
    }
}
